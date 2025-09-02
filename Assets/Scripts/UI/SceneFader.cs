using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Simple, backward-compatible screen fader + scene loader.
/// Works with both FadeToScene(...) and FadeAndLoad(...).
public class SceneFader : MonoBehaviour
{
    [Header("Fade UI")]
    [Tooltip("Full-screen CanvasGroup used to fade. Alpha will be animated 0→1.")]
    [SerializeField] private CanvasGroup fadeCanvas;

    [Tooltip("Seconds for the fade to full black before loading the scene.")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Tooltip("Use unscaled time (ignores Time.timeScale) for the fade.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("Block input while fading.")]
    [SerializeField] private bool blockRaycastsWhileFading = true;

    private Coroutine _running;

    // ---------- Backward-compat helpers ----------
    // Old code calls: sceneFader.FadeToScene("MyScene");
    public void FadeToScene(string sceneName) => FadeAndLoad(sceneName, 0f);

    // Old code calls: sceneFader.FadeToScene("MyScene", 0.35f);
    public void FadeToScene(string sceneName, float delaySeconds) => FadeAndLoad(sceneName, delaySeconds);

    // Newer API (also used by some scripts now)
    public void FadeAndLoad(string sceneName) => FadeAndLoad(sceneName, 0f);

    public void FadeAndLoad(string sceneName, float delaySeconds)
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(FadeAndSwitchScene(sceneName, delaySeconds));
    }

    // Optional static convenience (if some code wants to call without a reference)
    public static void FadeGlobal(string sceneName, float delaySeconds = 0f)
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var fader = FindFirstObjectByType<SceneFader>(FindObjectsInactive.Include);
#else
        var fader = FindObjectOfType<SceneFader>(true);
#endif
        if (fader != null) fader.FadeAndLoad(sceneName, delaySeconds);
        else SceneManager.LoadScene(sceneName); // fail-safe
    }

    private IEnumerator FadeAndSwitchScene(string sceneName, float delaySeconds)
    {
        if (fadeCanvas == null)
        {
            Debug.LogWarning("[SceneFader] No CanvasGroup assigned; loading scene without fade.");
            yield return Delay(delaySeconds);
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        // Prep fade layer
        fadeCanvas.gameObject.SetActive(true);
        if (blockRaycastsWhileFading) fadeCanvas.blocksRaycasts = true;

        // Optional delay before fade begins
        yield return Delay(delaySeconds);

        // Animate alpha -> 1
        float t = 0f;
        float start = fadeCanvas.alpha;
        while (t < fadeDuration)
        {
            t += Dt();
            fadeCanvas.alpha = Mathf.Lerp(start, 1f, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        fadeCanvas.alpha = 1f;

        // Load the new scene
        SceneManager.LoadScene(sceneName);
    }

    private float Dt() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private IEnumerator Delay(float s)
    {
        if (s <= 0f) yield break;
        float t = 0f;
        while (t < s) { t += Dt(); yield return null; }
    }
}