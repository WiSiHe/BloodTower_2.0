using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelExit : MonoBehaviour
{
    [Header("Next Level")]
    [Tooltip("Exact scene name (must be in Build Settings).")]
    [SerializeField] private string nextSceneName = "Tutorial";

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource exitAudio;
    [SerializeField] private bool waitForAudio = true; // wait for clip length before fade/load

    [Header("Trigger")]
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
        StartCoroutine(LoadNextRoutine());
    }

    private IEnumerator LoadNextRoutine()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("[LevelExit] nextSceneName is empty. Set it in the Inspector.");
            _used = false; // allow retry if misconfigured
            yield break;
        }

        // Safety: unpause
        Time.timeScale = 1f;

        // Try to freeze player control/motion briefly for a clean handoff
        var player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null)
        {
            // If you have a controller script, disable it (optional)
            var behaviour = player.GetComponent<MonoBehaviour>(); // replace with your specific controller if desired
            if (behaviour) behaviour.enabled = false;

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

        // Play exit SFX if assigned
        float wait = 0f;
        if (exitAudio != null && exitAudio.clip != null)
        {
            exitAudio.Play();
            if (waitForAudio) wait = exitAudio.clip.length;
        }

        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        // Use SceneFader if present; else load directly
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var fader = Object.FindFirstObjectByType<SceneFader>(FindObjectsInactive.Include);
#else
        var fader = Object.FindObjectOfType<SceneFader>(true);
#endif
        if (fader != null)
        {
            fader.FadeToScene(nextSceneName);   // <-- public API
        }
        else
        {
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