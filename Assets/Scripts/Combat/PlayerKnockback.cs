using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerKnockback : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float stunDuration = 0.25f;

    [Header("Camera Feedback")]
    [SerializeField] private float shakeAmplitude = 3.0f;
    [SerializeField] private float shakeFrequency = 14f;
    [SerializeField] private float shakeDuration  = 0.25f;

    private Rigidbody2D rb;
    private bool stunned;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(Vector2 direction, float forceMultiplier = 1f)
    {
        if (stunned) return;

        // stop vertical vel for crisp push
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
#else
        rb.velocity = new Vector2(0, rb.velocity.y);
#endif

        rb.AddForce(direction.normalized * knockbackForce * forceMultiplier, ForceMode2D.Impulse);
        StartCoroutine(StunRoutine());

        // 🔹 Camera shake on hit
        if (CameraShaker.Instance)
            CameraShaker.Instance.Shake(shakeAmplitude, shakeFrequency, shakeDuration);

        // 🔹 Optional: also trigger player damage FX
        DamageFeedback.PlayerHit();
    }

    private IEnumerator StunRoutine()
    {
        stunned = true;
        yield return new WaitForSeconds(stunDuration);
        stunned = false;
    }
}