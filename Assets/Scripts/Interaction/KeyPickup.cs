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
        if (!other.CompareTag(targetTag)) 
        {
            Debug.Log($"[KeyPickup] Collision with {other.tag}, expected {targetTag} - ignoring.");
            return;
        }

        Debug.Log($"[KeyPickup] Player triggered key pickup for key ID: '{keyId}'");
        
        if (GameSession.Instance == null)
        {
            Debug.LogError("[KeyPickup] GameSession.Instance is null! Cannot add key.");
            return;
        }

        if (GameSession.Instance.AddKey(keyId))
        {
            Debug.Log($"[KeyPickup] Successfully added key '{keyId}' to player inventory.");
            onPickedUp?.Invoke();
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"[KeyPickup] Failed to add key '{keyId}' - may already exist or invalid ID.");
        }
    }
}
