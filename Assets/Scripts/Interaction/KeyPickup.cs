using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KeyPickup : MonoBehaviour
{
    [Tooltip("Unique key ID, e.g. 'red_key', 'tower_key_1'.")]
    [SerializeField] private string keyId = "tower_key";

    [Tooltip("Only this tag can pick up the key.")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("Play a sound or VFX here if you want.")]
    [SerializeField] private UnityEngine.Events.UnityEvent onPickedUp;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag)) return;

        if (GameSession.Instance != null && GameSession.Instance.AddKey(keyId))
        {
            onPickedUp?.Invoke();
            Destroy(gameObject);
        }
    }
}