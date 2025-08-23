using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class StartRunOnTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool resetElapsedTime = true;
    [SerializeField] private bool oneShot = true;

    private bool used;
    private BoxCollider2D col2d;

    private void Reset()
    {
        col2d = GetComponent<BoxCollider2D>();
        col2d.isTrigger = true;
    }

    private void Awake()
    {
        col2d = GetComponent<BoxCollider2D>();
        if (!col2d.isTrigger)
        {
            col2d.isTrigger = true;
            Debug.LogWarning("[StartRunOnTrigger] BoxCollider2D wasn't a trigger. Fixed to IsTrigger=TRUE.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTrigger(other, "Enter");
    }

    // If the player spawns overlapping the trigger (or moves slowly), this also catches it:
    private void OnTriggerStay2D(Collider2D other)
    {
        // Only attempt again if not already used
        if (!used) HandleTrigger(other, "Stay");
    }

    private void HandleTrigger(Collider2D other, string phase)
    {
        if (used && oneShot) return;

        // Log what touched us (helps diagnose)
        // Note: For 2D triggers to fire, at least ONE of the colliders must be on a Rigidbody2D (usually the player)
        Debug.Log($"[StartRunOnTrigger] {phase}: {other.name} (tag={other.tag})");

        if (!other.CompareTag(targetTag)) return;

        if (GameSession.Instance == null)
        {
            Debug.LogError("[StartRunOnTrigger] GameSession.Instance is NULL. Make sure a GameSession exists in a prior scene and uses DontDestroyOnLoad.");
            return;
        }

        GameSession.Instance.StartRunTimer(resetElapsedTime);
        used = true;
        Debug.Log("[StartRunOnTrigger] Run timer started ✔");
    }

    // Nice gizmo for editor visibility
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawWireCube(box.offset, box.size);
        }
    }
}