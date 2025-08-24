using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerShove : MonoBehaviour
{
    [Header("Base Shove")]
    [SerializeField] private float baseShoveImpulse = 8f;
    [SerializeField] private float perChildBonus    = 2f;
    [SerializeField] private float shoveCooldown    = 0.25f;

    [Header("Target")]
    [SerializeField] private string bossTag = "Boss";

    private float lastShoveTime;
    private Rigidbody2D rb;

    private void Awake() { rb = GetComponent<Rigidbody2D>(); }

    private float CurrentShoveImpulse()
    {
        int kills = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;
        return baseShoveImpulse + perChildBonus * Mathf.Max(0, kills);
    }

    private void OnCollisionStay2D(Collision2D c)
    {
        if (c.collider.isTrigger || (c.otherCollider && c.otherCollider.isTrigger)) return;
        bool hitBoss = c.collider.CompareTag(bossTag) || (c.otherCollider && c.otherCollider.CompareTag(bossTag));
        if (!hitBoss) return;
        if (Time.time - lastShoveTime < shoveCooldown) return;

        var otherRB = c.otherRigidbody;
        if (otherRB == null) return;

        Vector2 dir = (otherRB.position - rb.position).normalized;
        float impulse = CurrentShoveImpulse();

        otherRB.AddForce(dir * impulse, ForceMode2D.Impulse);
        rb.AddForce(-dir * (impulse * 0.35f), ForceMode2D.Impulse);

        lastShoveTime = Time.time;
    }
}