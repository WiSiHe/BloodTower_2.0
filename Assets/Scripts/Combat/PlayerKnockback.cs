using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerKnockback : MonoBehaviour
{
    [SerializeField] private string controllerTypeName = "PlayerController";
    [SerializeField] private float defaultDisableSeconds = 0.25f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeAmplitude = 1.4f;
    [SerializeField] private float shakeFrequency = 7f;
    [SerializeField] private float shakeDuration  = 0.15f;

    private Rigidbody2D rb;
    private MonoBehaviour cachedController;
    private Coroutine co;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        foreach (var mb in GetComponentsInParent<MonoBehaviour>())
            if (mb && mb.GetType().Name == controllerTypeName) { cachedController = mb; break; }
        if (cachedController == null)
            Debug.LogWarning($"[PlayerKnockback] Could not find controller '{controllerTypeName}' on {name} or parents.");
    }

    public void ApplyKnockback(Vector2 impulse, float disableSeconds = -1f)
    {
        if (disableSeconds < 0f) disableSeconds = defaultDisableSeconds;

        rb.AddForce(impulse, ForceMode2D.Impulse);

        // Camera shake (safe if shaker not present)
        CameraShaker.Instance?.Shake(shakeAmplitude, shakeFrequency, shakeDuration);

        if (cachedController != null && disableSeconds > 0f)
        {
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(DisableControllerFor(disableSeconds));
        }
    }

    // Fallback so SendMessage can trigger knockback too
    private void OnExternalKnockback(Vector2 impulse)
    {
        ApplyKnockback(impulse, defaultDisableSeconds);
    }

    private IEnumerator DisableControllerFor(float seconds)
    {
        bool prev = cachedController.enabled;
        cachedController.enabled = false;
        yield return new WaitForSeconds(seconds);
        cachedController.enabled = prev;
    }
}