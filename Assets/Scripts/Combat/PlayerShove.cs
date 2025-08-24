using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerShove : MonoBehaviour
{
    [Header("Delta‑V Impulse (mass‑aware)")]
    [SerializeField] private float baseTargetDeltaV = 5.5f;
    [SerializeField] private float deltaVPerChild   = 0.35f;
    [Range(0f, 2f)] [SerializeField] private float relativeSpeedBoost = 1.0f;
    [SerializeField] private float shoveCooldown = 0.14f;

    [Header("Continuous Push Assist")]
    [SerializeField] private float pushAssistForce = 80f;
    [SerializeField] private bool  requireApproachForAssist = true;

    [Header("Safety")]
    [SerializeField] private float minImpulse = 20f; // N·s

    [Header("Target & Debug")]
    [SerializeField] private string bossTag = "Boss";
    [SerializeField] private bool   debugLogs = true;

    private float lastShoveTime;
    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private float CurrentTargetDeltaV()
    {
        int kills = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;
        return baseTargetDeltaV + Mathf.Max(0, kills) * deltaVPerChild;
    }

    private void OnCollisionStay2D(Collision2D c)
    {
        if (c.collider.isTrigger || (c.otherCollider && c.otherCollider.isTrigger)) return;

        bool hitBoss = c.collider.CompareTag(bossTag) || (c.otherCollider && c.otherCollider.CompareTag(bossTag));
        if (!hitBoss) return;

        var bossRB = c.otherRigidbody;
        if (bossRB == null) return;

        Vector2 dir = (bossRB.position - rb.position).normalized;
        if (c.contactCount > 0)
        {
            Vector2 avgNormal = Vector2.zero;
            int n = Mathf.Min(c.contactCount, 4);
            for (int i = 0; i < n; i++) avgNormal += c.GetContact(i).normal;
            avgNormal /= Mathf.Max(1, n);
            Vector2 push = -avgNormal; push.y = 0f;
            if (push.sqrMagnitude > 0.0001f) dir = push.normalized;
        }

        // Continuous assist
        if (pushAssistForce > 0f)
        {
#if UNITY_6000_0_OR_NEWER
            float relAlong = Vector2.Dot(rb.linearVelocity - bossRB.linearVelocity, dir);
#else
            float relAlong = Vector2.Dot(rb.velocity - bossRB.velocity, dir);
#endif
            bool ok = !requireApproachForAssist || relAlong >= -0.05f;
            if (ok) bossRB.AddForce(dir * pushAssistForce, ForceMode2D.Force);
        }

        if (Time.time - lastShoveTime >= shoveCooldown)
        {
            float targetDeltaV = CurrentTargetDeltaV();

#if UNITY_6000_0_OR_NEWER
            float relSpeedAlong = Vector2.Dot(rb.linearVelocity - bossRB.linearVelocity, dir);
#else
            float relSpeedAlong = Vector2.Dot(rb.velocity - bossRB.velocity, dir);
#endif
            if (relSpeedAlong > 0f)
                targetDeltaV *= (1f + Mathf.Clamp01(relSpeedAlong / 4f) * relativeSpeedBoost);

            float impulse = Mathf.Max(minImpulse, bossRB.mass * targetDeltaV);

            var knock = bossRB.GetComponent<BossKnockback>() ??
                        bossRB.GetComponentInParent<BossKnockback>() ??
                        bossRB.GetComponentInChildren<BossKnockback>();

            if (knock != null) knock.ApplyKnockback(dir * impulse, 0.35f);
            else               bossRB.AddForce(dir * impulse, ForceMode2D.Impulse);

            rb.AddForce(-dir * (impulse * 0.35f / Mathf.Max(1f, rb.mass)), ForceMode2D.Impulse);

            if (debugLogs)
                Debug.Log($"[PlayerShove] impulse={impulse:0.0} (min {minImpulse}), bossMass={bossRB.mass:0.00}, ΔV={(impulse/bossRB.mass):0.00}, rel={relSpeedAlong:0.00}, dir=({dir.x:0.00},{dir.y:0.00})");

            lastShoveTime = Time.time;
        }
    }

    public void SetDesign(float baseTargetDeltaV, float deltaVPerChild, float relativeSpeedBoost, float shoveCooldown)
    {
        this.baseTargetDeltaV   = baseTargetDeltaV;
        this.deltaVPerChild     = deltaVPerChild;
        this.relativeSpeedBoost = relativeSpeedBoost;
        this.shoveCooldown      = shoveCooldown;
    }
}