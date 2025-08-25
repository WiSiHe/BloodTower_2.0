using UnityEngine;
using System.Collections;

/// When the player rams the King, the King either PARRIES instantly (very high approach)
/// or BRACES (heavy + high friction) then shoulder‑bashes to break the scrum.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossCounterPush : MonoBehaviour
{
    [Header("Detect player (tag and/or layer)")]
    [SerializeField] private string    playerTag    = "Player";
    [SerializeField] private LayerMask playerLayers = ~0;

    [Header("Scrum detection")]
    [Tooltip("Min approach speed along contact axis to count as pushing (m/s).")]
    [SerializeField] private float approachThreshold = 0.9f;
    [Tooltip("Time pushing before brace+counter (s).")]
    [SerializeField] private float braceDelay = 0.30f;

    [Header("Instant Parry")]
    [Tooltip("If approach ≥ this, fire an instant counter (m/s).")]
    [SerializeField] private float parryThreshold = 3.0f;

    [Header("Brace state")]
    [SerializeField] private float braceDuration        = 0.45f;
    [SerializeField] private float braceMassMultiplier  = 2.2f;
    [SerializeField] private PhysicsMaterial2D highFrictionWhileBraced;

    [Header("Counter shove")]
    [SerializeField] private float counterImpulse  = 14f;
    [SerializeField] private float counterCooldown = 1.00f;
    [SerializeField] private float windupSeconds   = 0.08f;

    [Header("Feedback (optional)")]
    [SerializeField] private float camShakeAmp = 2.0f, camShakeFreq = 12f, camShakeDur = 0.20f;
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip   windupSfx, bashSfx;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    Rigidbody2D rb;
    Collider2D  col;

    float contactTimer = 0f;
    float lastCounterTime = -999f;
    bool  bracing = false;

    // saved state
    float             savedMass;
    PhysicsMaterial2D savedMat;

    // previous-frame motion (so the solver can't zero our approach reading)
    Vector2     prevKingPos,  prevPlayerPos;
    float       prevDt = 1 / 60f;
    Rigidbody2D lastPlayerRb;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (col.isTrigger) col.isTrigger = false;
        prevKingPos = rb.position;
    }

    void LateUpdate()
    {
        if (lastPlayerRb != null) prevPlayerPos = lastPlayerRb.position;
        prevKingPos = rb.position;
        prevDt = Mathf.Max(0.0001f, Time.deltaTime);
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (bracing) return;

        var otherRb = c.collider.attachedRigidbody; // works with child colliders
        if (otherRb == null) return;

        if (((1 << otherRb.gameObject.layer) & playerLayers) == 0) return;
        if (!string.IsNullOrEmpty(playerTag) && !otherRb.CompareTag(playerTag)) return;

        lastPlayerRb ??= otherRb;
        if (c.contactCount == 0) return;

        // Pick the most "into the king" contact
        float   bestApproach = 0f;
        Vector2 bestPushDir  = Vector2.zero;

        for (int i = 0; i < c.contactCount; i++)
        {
            // Contact normal points from KING into OTHER
            Vector2 nKingToPlayer = c.GetContact(i).normal;
            Vector2 pushDir       = -nKingToPlayer.normalized; // king shoves player along this

            // Previous-frame motion (robust)
            Vector2 kingDelta   = (rb.position        - prevKingPos)   / prevDt;
            Vector2 playerDelta = (otherRb.position   - prevPlayerPos) / prevDt;
            Vector2 relPrev     = playerDelta - kingDelta; // player vs king last frame
            float approachPrev  = Vector2.Dot(relPrev, -pushDir); // >0 when player moved into king

            // Fallbacks
#if UNITY_6000_0_OR_NEWER
            Vector2 relNow = otherRb.linearVelocity - rb.linearVelocity;
#else
            Vector2 relNow = otherRb.velocity - rb.velocity;
#endif
            float approachNow  = Vector2.Dot(relNow, -pushDir);
            float approachCont = Vector2.Dot(c.relativeVelocity, -pushDir);

            float approach = Mathf.Max(approachPrev, approachNow, approachCont);

            if (approach > bestApproach)
            {
                bestApproach = approach;
                bestPushDir  = pushDir;
            }
        }

        if (debugLogs)
            Debug.Log($"[BossCounterPush] contacts={c.contactCount}, bestApproach={bestApproach:0.00}, needed={approachThreshold}");

        // Cooldown gate
        if (Time.time - lastCounterTime < counterCooldown) return;

        // 1) Instant parry for huge charges
        if (bestApproach >= parryThreshold)
        {
            if (debugLogs) Debug.Log($"[BossCounterPush] PARRY! approach={bestApproach:0.00} ≥ {parryThreshold}");
            StartCoroutine(DoBraceAndCounter(bestPushDir, otherRb, instant:true));
            return;
        }

        // 2) Otherwise, accumulate time for brace+counter
        if (bestApproach >= approachThreshold)
        {
            contactTimer += Time.fixedDeltaTime;
            if (contactTimer >= braceDelay)
            {
                StartCoroutine(DoBraceAndCounter(bestPushDir, otherRb, instant:false));
            }
        }
        else
        {
            contactTimer = 0f;
        }
    }

    void OnCollisionExit2D(Collision2D c)
    {
        var otherRb = c.collider.attachedRigidbody;
        if (otherRb && (string.IsNullOrEmpty(playerTag) || otherRb.CompareTag(playerTag)))
        {
            contactTimer = 0f;
            if (otherRb == lastPlayerRb) lastPlayerRb = null;
        }
    }

    IEnumerator DoBraceAndCounter(Vector2 pushDir, Rigidbody2D playerRb, bool instant)
    {
        bracing = true;
        lastCounterTime = Time.time;

        // BRACE
        savedMass = rb.mass;
        rb.mass   = savedMass * braceMassMultiplier;

        if (col)
        {
            savedMat = col.sharedMaterial;
            if (highFrictionWhileBraced) col.sharedMaterial = highFrictionWhileBraced;
        }

        if (debugLogs) Debug.Log($"[BossCounterPush] BRACE begin (mass x{braceMassMultiplier}). instant={instant}");

        // Wind‑up (skip or keep short for parry)
        float wind = instant ? Mathf.Min(0.04f, windupSeconds * 0.5f) : windupSeconds;
        if (windupSfx && sfx) sfx.PlayOneShot(windupSfx);
        float t = 0f;
        while (t < wind) { t += Time.unscaledDeltaTime; yield return null; }

        // COUNTER
        if (playerRb)
        {
            var knock = playerRb.GetComponent<PlayerKnockback>();
            if (knock != null)
            {
                float mult = counterImpulse / Mathf.Max(0.01f, playerRb.mass); // impulse → knockback multiplier
                knock.ApplyKnockback(pushDir, mult);
            }
            playerRb.AddForce(pushDir * counterImpulse, ForceMode2D.Impulse);      // ensure physical shove
            rb.AddForce(-pushDir * counterImpulse * 0.35f, ForceMode2D.Impulse);   // small recoil
        }

        if (bashSfx && sfx) sfx.PlayOneShot(bashSfx);
        if (CameraShaker.Instance) CameraShaker.Instance.Shake(camShakeAmp, camShakeFreq, camShakeDur);

        if (debugLogs) Debug.Log($"[BossCounterPush] COUNTER fired. dir={pushDir} imp={counterImpulse:0.0}");

        // Stay heavy briefly (shorter if parry)
        yield return new WaitForSeconds(instant ? Mathf.Min(0.25f, braceDuration * 0.6f) : braceDuration);

        // RESTORE
        rb.mass = savedMass;
        if (col) col.sharedMaterial = savedMat;
        bracing = false;
        contactTimer = 0f;

        if (debugLogs) Debug.Log("[BossCounterPush] BRACE end.");
    }
}