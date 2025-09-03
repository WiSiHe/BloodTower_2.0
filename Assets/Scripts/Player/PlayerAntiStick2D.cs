using UnityEngine;

/// Prevents the player from "sticking" to the sides of platforms/walls:
/// - Temporarily ensures zero friction on side contacts
/// - Applies a tiny depenetration nudge away from the wall
/// - Adds a small downward slide so you don't hover while jammed
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerAntiStick2D : MonoBehaviour
{
    [Header("Side Contact Detection")]
    [Tooltip("Treat contacts with |normal.x| >= this as 'side' collisions (0.8 ≈ steep wall).")]
    [SerializeField] private float sideNormalDot = 0.8f;

    [Tooltip("Ignore contacts that are basically ground/ceiling. If |normal.y| > this, it's not 'side'.")]
    [SerializeField] private float verticalReject = 0.6f;

    [Header("Fix Behaviour")]
    [Tooltip("World units to nudge away from the wall per physics tick when pressed into it.")]
    [SerializeField] private float depenetrationPerTick = 0.0035f;

    [Tooltip("Minimal downward slide while jammed so you don't hover.")]
    [SerializeField] private float minDownSlideSpeed = -0.35f;

    [Tooltip("Optional material with zero friction. If not set, we'll just keep the player's current one.")]
    [SerializeField] private PhysicsMaterial2D noFrictionMaterial;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Rigidbody2D rb;
    private Collider2D col;
    private PhysicsMaterial2D originalMat;
    private bool sideContactActive;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        originalMat = col.sharedMaterial;

        // Good physics defaults for platformers
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void OnCollisionStay2D(Collision2D c)
    {
        // Scan all contacts; react to the strongest "side" one
        bool hadSide = false;
        Vector2 strongestNormal = Vector2.zero;
        float bestAbsX = 0f;

        for (int i = 0; i < c.contactCount; i++)
        {
            Vector2 n = c.GetContact(i).normal; // points from *other* into *this* player
            float ax = Mathf.Abs(n.x);
            float ay = Mathf.Abs(n.y);

            if (ay > verticalReject) continue;      // it's ground/ceiling-like, not side
            if (ax >= sideNormalDot)                // strong side normal
            {
                if (ax > bestAbsX) { bestAbsX = ax; strongestNormal = n; }
                hadSide = true;
            }
        }

        if (!hadSide) return;

        sideContactActive = true;

        // 1) Ensure zero friction while jammed on a side
        if (noFrictionMaterial != null && col.sharedMaterial != noFrictionMaterial)
            col.sharedMaterial = noFrictionMaterial;

        // 2) Tiny depenetration away from the wall (normal points from other→player, so move with +normal)
        Vector2 push = strongestNormal.normalized * depenetrationPerTick;
#if UNITY_6000_0_OR_NEWER
        rb.MovePosition(rb.position + push);
#else
        rb.MovePosition(rb.position + push);
#endif

        // 3) Make sure we slide down a bit if we're pinned, so we don't "hang" on micro-ledges
        // Skip this if player is bouncing (let bounce protection handle it)
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Use reflection to check if player is bouncing (since isBouncing is private)
            var isBouncingField = typeof(PlayerController).GetField("isBouncing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isBouncingField != null && (bool)isBouncingField.GetValue(playerController))
            {
                if (debugLogs)
                    Debug.Log("[PlayerAntiStick2D] Skipping downslide - player is bouncing");
                return;
            }
        }
        
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity;
        if (v.y > minDownSlideSpeed) v.y = minDownSlideSpeed;
        rb.linearVelocity = v;
#else
        var v = rb.velocity;
        if (v.y > minDownSlideSpeed) v.y = minDownSlideSpeed;
        rb.velocity = v;
#endif

        if (debugLogs)
            Debug.Log($"[PlayerAntiStick2D] Side contact → depenetrate {push}, set downslide {minDownSlideSpeed}");
    }

    void OnCollisionExit2D(Collision2D c)
    {
        // If no more contacts remain this frame, restore material
        sideContactActive = false;
        RestoreMaterialSoon();
    }

    void LateUpdate()
    {
        // Safety: if we didn't process a side this frame, restore mat
        if (!sideContactActive) RestoreMaterialSoon();
        sideContactActive = false;
    }

    private void RestoreMaterialSoon()
    {
        if (noFrictionMaterial != null && originalMat != null && col.sharedMaterial == noFrictionMaterial)
            col.sharedMaterial = originalMat; // if you always want no friction, just remove this line
    }
}
