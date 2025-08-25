using UnityEngine;

/// Adds a physics "shove" when the player collides with the boss, but ONLY
/// if (a) cooldown elapsed and (b) the player is moving into the boss (impact).
/// BossFightManager can call SetDesign(...) at runtime to tune values.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerShove : MonoBehaviour
{
    [Header("Who can be shoved")]
    [SerializeField] private string bossTag = "Boss";

    [Header("Design (overridden by BossFightManager)")]
    [Tooltip("Target delta‑V to impart to the boss per shove (m/s).")]
    [SerializeField] private float baseTargetDeltaV = 5.0f;

    [Tooltip("Scales additional delta‑V from relative approach speed.")]
    [SerializeField] private float relativeSpeedBoost = 1.0f;

    [Tooltip("Seconds between shoves. STRICTLY enforced.")]
    [SerializeField] private float shoveCooldown = 0.18f;

    [Header("Guards")]
    [Tooltip("Require this much approach speed along the contact normal to count as an impact (m/s).")]
    [SerializeField] private float approachThreshold = 0.6f;

    [Tooltip("Minimum impulse (N·s) we will apply no matter what.")]
    [SerializeField] public float minImpulse = 20f;

    [Tooltip("Optional forward assist force to help the player lean into pushes.")]
    [SerializeField] public float pushAssistForce = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Rigidbody2D rb;
    private float lastShoveTime = -999f;
    private int lastHandledFixedFrame = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// Called by BossFightManager to retune during the fight.
    public void SetDesign(float baseTargetDeltaV, float deltaVPerChild, float relativeSpeedBoost, float shoveCooldown)
    {
        this.baseTargetDeltaV   = baseTargetDeltaV;
        // deltaVPerChild is unused because we’re driving kills via the manager curve now.
        this.relativeSpeedBoost = relativeSpeedBoost;
        this.shoveCooldown      = shoveCooldown;
    }

    private void FixedUpdate()
    {
        // optional “lean forward” helper while moving toward the king
        if (pushAssistForce > 0f)
        {
            // Apply a very small forward force; harmless if you’re not pushing.
            rb.AddForce(new Vector2(rb.linearVelocity.x * 0.0f, 0f)); // placeholder for custom assist if desired
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag(bossTag)) return;

        // Enforce: only one shove attempt per physics step
        if (lastHandledFixedFrame == Time.frameCount) return;
        lastHandledFixedFrame = Time.frameCount;

        // Cooldown gate
        if (Time.time - lastShoveTime < shoveCooldown) return;

        // Determine if we're IMPACTING the boss (approaching along the contact normal)
        // Use the first contact for direction
        var contacts = collision.contacts;
        if (contacts == null || contacts.Length == 0) return;

        // normal points from *boss* into *player*, so we push the boss along -normal
        Vector2 normalFromBossToPlayer = contacts[0].normal;
        Vector2 pushDir = -normalFromBossToPlayer.normalized;

        // Relative velocity along the push axis (boss - player)
        var bossRb = collision.rigidbody; // boss rigidbody
        if (bossRb == null) return;

#if UNITY_6000_0_OR_NEWER
        Vector2 vPlayer = rb.linearVelocity;
        Vector2 vBoss   = bossRb.linearVelocity;
#else
        Vector2 vPlayer = rb.velocity;
        Vector2 vBoss   = bossRb.velocity;
#endif
        Vector2 rel = vBoss - vPlayer; // if player moves into boss, projection onto pushDir will be negative
        float relAlong = Vector2.Dot(rel, pushDir);

        // Require sufficient approach speed (impact) before we shove
        if (relAlong > -approachThreshold) return;

        // Compute desired impulse: mass * (ΔV + relative bonus)
        float targetDeltaV = baseTargetDeltaV + (relativeSpeedBoost * (-relAlong)); // -relAlong is positive when approaching
        float impulse = Mathf.Max(minImpulse, bossRb.mass * targetDeltaV);

        // Apply impulse to boss (and equal opposite to player if you want recoil; here we just push boss)
        bossRb.AddForce(pushDir * impulse, ForceMode2D.Impulse);

        lastShoveTime = Time.time;

        if (debugLogs)
        {
            Debug.Log($"[PlayerShove] impulse={impulse:0.0} (min {minImpulse}), bossMass={bossRb.mass:0.00}, ΔV={targetDeltaV:0.00}, rel={relAlong:0.00}, dir=({pushDir.x:0.00},{pushDir.y:0.00})");
        }
    }
}