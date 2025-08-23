using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    [Header("Next Level")]
    [Tooltip("Exact scene name (must be in Build Settings).")]
    [SerializeField] private string nextSceneName = "Tutorial";

    [SerializeField] AudioSource exitAudio;

    [Tooltip("Only the object with this tag can trigger the exit.")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("If true, disables this trigger after first use to prevent double-loads.")]
    [SerializeField] private bool oneShot = true;

    [Header("Optional: Callbacks")]
    [Tooltip("Play a sound, animation, etc. right when player enters.")]
    [SerializeField] private UnityEngine.Events.UnityEvent onTriggered;

#if UNITY_EDITOR
    // Editor-only helper to reduce typos: drag a SceneAsset and it will copy its name
    [SerializeField] private Object sceneAsset; // keep as Object to avoid editor-only type in builds
    private void OnValidate()
    {
        if (sceneAsset != null)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(sceneAsset);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".unity"))
            {
                // Extract scene name from path (last segment without .unity)
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(file)) nextSceneName = file;
            }
        }
         if (exitAudio == null)
            exitAudio = GetComponentInChildren<AudioSource>();
    }
#endif

    private bool _used;

    private void Reset()
    {
        // Make it a 2D trigger by default
        var box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_used && oneShot) return;
        if (!other.CompareTag(targetTag)) return;

        _used = true;
        onTriggered?.Invoke();
        LoadNext();
    }

    private void LoadNext()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("[LevelExit] nextSceneName is empty. Set it in the Inspector.");
            _used = false; // allow retry if misconfigured
            return;
        }

        // If you use a GameSession and want to reset anything, do it here before load:
        // GameSession.Instance?.SetLastLevel(nextSceneName);

        Time.timeScale = 1f; // safety

        var player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc) pc.enabled = false;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
        #if UNITY_6000_OR_NEWER
            rb.linearVelocity = Vector2.zero;
        #else
                rb.linearVelocity = Vector2.zero;
        #endif
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }

        // Try to use the SceneFader prefab if it's present; otherwise load directly.
        var fader = FindObjectOfType<SceneFader>();
        if (fader != null)
        {
            // Use your existing loadDelay as the fade duration as well.
            fader.FadeAndLoad(nextSceneName);
        }
        else
        {
            // Fallback: no fader in scene
            SceneManager.LoadScene(nextSceneName);
        }
    }

    // Editor gizmo so you can see the trigger volume easily
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.25f);
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
            Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(box.offset, box.size);
        }
    }
}