using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpikeTrap : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float cooldown = 2f; // seconds between damage
    [SerializeField] private string targetTag = "Player";
    [Tooltip("If true, the trap only hurts once then deactivates forever.")]
    [SerializeField] private bool oneShot = false;

    [Header("Optional: Callbacks")]
    [SerializeField] private UnityEngine.Events.UnityEvent onTriggered; // play SFX, VFX, etc.

    private bool used;
    private float lastDamageTime = -999f;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // spikes usually triggers
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Also check while inside trap, so standing on spikes keeps hurting with cooldown
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (oneShot && used) return;
        if (!other.CompareTag(targetTag)) return;

        // Check cooldown
        if (Time.time - lastDamageTime < cooldown) return;

        // Apply damage
        if (GameSession.Instance != null)
        {
            GameSession.Instance.Damage(damageAmount);
            Debug.Log($"[SpikeTrap] Damaged player for {damageAmount}. Remaining hearts: {GameSession.Instance.Hearts}");
        }

        onTriggered?.Invoke();
        used = true;
        lastDamageTime = Time.time;
    }
}