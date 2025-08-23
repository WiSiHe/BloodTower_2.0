using UnityEngine;

/// <summary>
/// Place this in a scene (e.g., on an empty GameObject).
/// Assign the music clip for this scene; it will auto-play on load via MusicManager.
/// </summary>
public class SceneMusicCue : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float fadeSeconds = 1.5f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    [Header("Behavior")]
    [Tooltip("Stop music (fade out) when this scene unloads? Usually leave ON for one-shot scenes like StartMenu/Intro.")]
    [SerializeField] private bool stopOnDisable = false;

    private void Start()
    {
        EnsureManager();
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic(musicClip, fadeSeconds, volume);
        }
    }

    private void OnDisable()
    {
        if (stopOnDisable && MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic(null, fadeSeconds, 0f); // fade out
        }
    }

    private void EnsureManager()
    {
        if (MusicManager.Instance != null) return;

        // Auto-spawn a manager if one doesn't exist yet (optional convenience)
        var go = new GameObject("MusicManager_Auto");
        go.AddComponent<MusicManager>();
    }
}