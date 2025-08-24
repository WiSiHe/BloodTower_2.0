using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerKnockback : MonoBehaviour
{
    [SerializeField] private string controllerTypeName = "PlayerController";
    [SerializeField] private float defaultDisableSeconds = 0.25f;

    private Rigidbody2D rb;
    private MonoBehaviour cachedController;
    private Coroutine co;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        foreach (var mb in GetComponentsInParent<MonoBehaviour>()) // parent-safe search
            if (mb && mb.GetType().Name == controllerTypeName) { cachedController = mb; break; }

        if (cachedController == null)
            Debug.LogWarning($"[PlayerKnockback] Could not find controller '{controllerTypeName}' on {name} or parents.");
    }

    public void ApplyKnockback(Vector2 impulse, float disableSeconds = -1f)
    {
        if (disableSeconds < 0f) disableSeconds = defaultDisableSeconds;
        rb.AddForce(impulse, ForceMode2D.Impulse);

        if (cachedController != null && disableSeconds > 0f)
        {
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(DisableControllerFor(disableSeconds));
        }
    }

    // Fallback so SendMessage works if BossKing can’t find the component directly
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