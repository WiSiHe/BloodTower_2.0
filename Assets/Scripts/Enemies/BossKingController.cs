using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossKingController : MonoBehaviour
{
    [Header("Chase (anti‑stuck controller)")]
    [SerializeField] private float targetSpeed = 6f;
    [SerializeField] private float accelGain  = 40f;
    [SerializeField] private float maxSpeed   = 10f;
    [SerializeField] private float dirBias    = 0.15f;
    [SerializeField] private float velocityDamping = 0.98f;

    [Header("Shove")]
    [SerializeField] private float shoveImpulse  = 16f;
    [SerializeField] private float shoveCooldown = 0.35f;
    [SerializeField] private float playerDisableSecs = 0.25f;
    [SerializeField] private string playerTag = "Player";

    [Header("Anti‑Stuck Nudge")]
    [SerializeField] private float stuckSpeedThreshold = 0.05f;
    [SerializeField] private float stuckTime           = 0.35f;
    [SerializeField] private float nudgeImpulse        = 2.0f;

    private Rigidbody2D rb;
    private Transform player;
    private BossKnockback knock;
    private float lastShove;
    private float lowSpeedTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        knock = GetComponent<BossKnockback>();

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var pc = GameObject.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        player = pc ? pc.transform : GameObject.FindGameObjectWithTag(playerTag)?.transform;
#else
        var pc = GameObject.FindObjectOfType<PlayerController>(true);
        player = pc ? pc.transform : GameObject.FindGameObjectWithTag(playerTag)?.transform;
#endif
    }

    private void FixedUpdate()
    {
        if (!player) return;

        // Do NOT steer while stunned; let external impulses move the boss
        if (knock && knock.IsStunned) return;

        float dx  = player.position.x - transform.position.x;
        float dir = Mathf.Sign(dx);
        if (Mathf.Abs(dx) < 0.05f)
            dir = Mathf.Sign(dx + Mathf.Sign(dx) * dirBias + (dx == 0 ? dirBias : 0));

        float desiredVx = dir * targetSpeed;
        float errorVx   = desiredVx - rb.linearVelocity.x;
        float forceX    = errorVx * accelGain;
        rb.AddForce(new Vector2(forceX, 0f), ForceMode2D.Force);

        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        rb.linearVelocity *= velocityDamping;

        if (Mathf.Abs(rb.linearVelocity.x) < stuckSpeedThreshold)
        {
            lowSpeedTimer += Time.fixedDeltaTime;
            if (lowSpeedTimer >= stuckTime)
            {
                rb.AddForce(new Vector2(Mathf.Sign(dx) * nudgeImpulse, 0f), ForceMode2D.Impulse);
                rb.WakeUp();
                lowSpeedTimer = 0f;
            }
        }
        else lowSpeedTimer = 0f;
    }

    private void OnCollisionStay2D(Collision2D c)
    {
        if (c.collider.isTrigger || (c.otherCollider && c.otherCollider.isTrigger)) return;
        if (Time.time - lastShove < shoveCooldown) return;

        Rigidbody2D rbA = c.rigidbody;
        Rigidbody2D rbB = c.otherRigidbody;
        if (!rbA || !rbB) return;

        Rigidbody2D playerRB = null;
        Rigidbody2D bossRB   = null;

        if (rbA.gameObject.CompareTag(playerTag)) { playerRB = rbA; bossRB = rbB; }
        else if (rbB.gameObject.CompareTag(playerTag)) { playerRB = rbB; bossRB = rbA; }
        else return;

        Vector2 away = (playerRB.position - bossRB.position).normalized;

        var knockback =
            playerRB.GetComponent<PlayerKnockback>() ??
            playerRB.GetComponentInParent<PlayerKnockback>() ??
            playerRB.GetComponentInChildren<PlayerKnockback>();

        if (knockback != null) knockback.ApplyKnockback(away * shoveImpulse, playerDisableSecs);
        else                   playerRB.AddForce(away * shoveImpulse, ForceMode2D.Impulse);

        rb.AddForce(-away * (shoveImpulse * 0.25f), ForceMode2D.Impulse);
        lastShove = Time.time;
    }

    // Helper for BossFightManager tuning
    public void SetDesign(float shoveImpulse, float shoveCooldown, float targetSpeed, float accelGain, float maxSpeed)
    {
        this.shoveImpulse  = shoveImpulse;
        this.shoveCooldown = shoveCooldown;
        this.targetSpeed   = targetSpeed;
        this.accelGain     = accelGain;
        this.maxSpeed      = maxSpeed;
    }
}