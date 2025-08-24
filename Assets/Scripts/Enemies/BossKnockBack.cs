using UnityEngine;
using System.Collections;

/// Applies knockback to the Boss and guarantees slide:
/// - Disables the boss controller for a short "stun"
/// - Swaps to low friction
/// - Clears FreezePosition (keeps only FreezeRotation)
/// - Zeroes drag during stun
/// - Enforces a minimum horizontal speed while stunned
[RequireComponent(typeof(Rigidbody2D))]
public class BossKnockback : MonoBehaviour
{
    [Header("Boss controller type name (on this GameObject)")]
    [SerializeField] private string controllerTypeName = "BossKingController";

    [Header("Stun Settings")]
    [Tooltip("Seconds the boss AI is disabled after a shove.")]
    [SerializeField] private float defaultDisableSeconds = 0.35f;

    [Tooltip("While stunned, enforce at least this horizontal speed (units/s).")]
    [SerializeField] private float minHorizontalSpeed = 5.0f;

    [Tooltip("Physics material to apply during stun (Friction ~0.05–0.1).")]
    [SerializeField] private PhysicsMaterial2D lowFrictionDuringStun;

    [Tooltip("Print debug when stunning / restoring.")]
    [SerializeField] private bool debugLogs = false;

    public bool IsStunned { get; private set; }

    private Rigidbody2D rb;
    private MonoBehaviour controller;
    private Collider2D col;

    // Saved state (drag/damping & constraints & material)
#if UNITY_6000_0_OR_NEWER
    private float prevLinearDamping;
    private float prevAngularDamping;
#else
    private float prevDrag;
    private float prevAngularDrag;
#endif
    private RigidbodyConstraints2D prevConstraints;
    private PhysicsMaterial2D prevMaterial;

    private Coroutine co;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Find the movement controller by type name on this GO
        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Name == controllerTypeName)
            {
                controller = mb;
                break;
            }
        }
    }

    /// Apply an impulse and stun; impulse.x sign is used to enforce min horizontal speed.
    public void ApplyKnockback(Vector2 impulse, float disableSeconds = -1f)
    {
        if (disableSeconds < 0f) disableSeconds = defaultDisableSeconds;

        rb.AddForce(impulse, ForceMode2D.Impulse);

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(DoStun(disableSeconds, Mathf.Sign(impulse.x)));
    }

    private IEnumerator DoStun(float seconds, float dirX)
    {
        IsStunned = true;

        // Save state
        prevConstraints = rb.constraints;
#if UNITY_6000_0_OR_NEWER
        prevLinearDamping  = rb.linearDamping;
        prevAngularDamping = rb.angularDamping;
#else
        prevDrag        = rb.drag;
        prevAngularDrag = rb.angularDrag;
#endif
        if (col) prevMaterial = col.sharedMaterial;

        // Apply stun state
        if (controller) controller.enabled = false;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // ensure X/Y are free
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping  = 0f;
        rb.angularDamping = 0.05f;
#else
        rb.drag        = 0f;
        rb.angularDrag = 0.05f;
#endif
        if (col && lowFrictionDuringStun) col.sharedMaterial = lowFrictionDuringStun;

        if (debugLogs)
        {
#if UNITY_6000_0_OR_NEWER
            Debug.Log($"[BossKnockback] STUN begin: linDamp=0, angDamp=0.05, constraints={rb.constraints}, mat={(col?col.sharedMaterial:null)}, dirX={dirX}");
#else
            Debug.Log($"[BossKnockback] STUN begin: drag=0, angDrag=0.05, constraints={rb.constraints}, mat={(col?col.sharedMaterial:null)}, dirX={dirX}");
#endif
        }

        // Enforce a minimum horizontal speed for the duration
        float t = 0f;
        dirX = Mathf.Approximately(dirX, 0f) ? 1f : Mathf.Sign(dirX);
        while (t < seconds)
        {
#if UNITY_6000_0_OR_NEWER
            Vector2 v = rb.linearVelocity;
            float sx = Mathf.Abs(v.x);
            if (sx < minHorizontalSpeed) v.x = dirX * minHorizontalSpeed;
            rb.linearVelocity = v;
#else
            Vector2 v = rb.velocity;
            float sx = Mathf.Abs(v.x);
            if (sx < minHorizontalSpeed) v.x = dirX * minHorizontalSpeed;
            rb.velocity = v;
#endif
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Restore state
        if (controller) controller.enabled = true;
        rb.constraints = prevConstraints;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping  = prevLinearDamping;
        rb.angularDamping = prevAngularDamping;
#else
        rb.drag        = prevDrag;
        rb.angularDrag = prevAngularDrag;
#endif
        if (col) col.sharedMaterial = prevMaterial;

        if (debugLogs)
        {
#if UNITY_6000_0_OR_NEWER
            Debug.Log($"[BossKnockback] STUN end: linDamp={rb.linearDamping}, angDamp={rb.angularDamping}, constraints={rb.constraints}, mat={(col?col.sharedMaterial:null)}");
#else
            Debug.Log($"[BossKnockback] STUN end: drag={rb.drag}, angDrag={rb.angularDrag}, constraints={rb.constraints}, mat={(col?col.sharedMaterial:null)}");
#endif
        }

        IsStunned = false;
    }
}