using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossKingController : MonoBehaviour
{
    [Header("Chase")]
    [SerializeField] private float targetSpeed = 6f;       // desired horizontal speed toward player
    [SerializeField] private float accelGain  = 40f;       // how strongly we correct toward targetSpeed
    [SerializeField] private float maxSpeed   = 10f;       // absolute clamp so it doesn’t go wild
    [SerializeField] private float dirBias    = 0.15f;     // prevents 0 when perfectly aligned on X

    [Header("Shove")]
    [SerializeField] private float shoveImpulse       = 18f;
    [SerializeField] private float shoveCooldown      = 0.35f;
    [SerializeField] private float playerDisableSecs  = 0.25f;
    [SerializeField] private string playerTag         = "Player";

    [Header("Damping")]
    [Tooltip("Light velocity damping per FixedUpdate (set ~0.98..1). Too low = stalls.")]
    [SerializeField] private float velocityDamping = 0.98f;

    [Header("Anti-Stuck")]
    [SerializeField] private float stuckSpeedThreshold = 0.05f;  // below this we consider “stopped”
    [SerializeField] private float stuckTime           = 0.35f;  // time under threshold before nudging
    [SerializeField] private float nudgeImpulse        = 2.0f;   // small kick toward player

    private Rigidbody2D rb;
    private Transform player;
    private float lastShove;
    private float lowSpeedTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;  // avoid sleeping stalls
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var go = GameObject.FindGameObjectWithTag(playerTag);
        player = go ? go.transform : null;
    }

    private void FixedUpdate()
    {
        if (!player) return;

        // Direction along X, with a tiny bias so it’s never exactly 0
        float dx = player.position.x - transform.position.x;
        float dir = Mathf.Sign(dx);
        if (Mathf.Abs(dx) < 0.05f) dir = Mathf.Sign(dx + Mathf.Sign(dx) * dirBias + (dx == 0 ? dirBias : 0));

        // PD-style speed matching along X
        float desiredVx = dir * targetSpeed;
        float errorVx   = desiredVx - rb.linearVelocity.x;
        float forceX    = errorVx * accelGain; // ForceMode2D.Force (per-step)
        rb.AddForce(new Vector2(forceX, 0f), ForceMode2D.Force);

        // Clamp absolute speed and apply light damping (close to 1 to avoid stalls)
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        rb.linearVelocity *= velocityDamping;

        // Anti-stuck: if we’re barely moving horizontally for a bit, give a nudge toward player
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
        else
        {
            lowSpeedTimer = 0f;
        }
    }

    private void OnCollisionStay2D(Collision2D c)
    {
        // Ignore triggers
        if (c.collider.isTrigger || (c.otherCollider && c.otherCollider.isTrigger)) return;
        if (Time.time - lastShove < shoveCooldown) return;

        // Identify which rigidbody is the player by tag
        Rigidbody2D rbA = c.rigidbody;      // one side of the contact
        Rigidbody2D rbB = c.otherRigidbody; // other side
        if (!rbA || !rbB) return;

        Rigidbody2D playerRB = null;
        Rigidbody2D bossRB   = null;

        if (rbA.gameObject.CompareTag(playerTag)) { playerRB = rbA; bossRB = rbB; }
        else if (rbB.gameObject.CompareTag(playerTag)) { playerRB = rbB; bossRB = rbA; }
        else return; // not a player contact

        Vector2 away = (playerRB.position - bossRB.position).normalized;

        // Prefer PlayerKnockback so movement controller doesn’t cancel the shove
        PlayerKnockback knock =
            playerRB.GetComponent<PlayerKnockback>() ??
            playerRB.GetComponentInParent<PlayerKnockback>() ??
            playerRB.GetComponentInChildren<PlayerKnockback>();

        if (knock != null)
        {
            knock.ApplyKnockback(away * shoveImpulse, playerDisableSecs);
        }
        else
        {
            playerRB.AddForce(away * shoveImpulse, ForceMode2D.Impulse);
        }

        // Small recoil for king
        rb.AddForce(-away * (shoveImpulse * 0.25f), ForceMode2D.Impulse);

        lastShove = Time.time;
    }
}