using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultDuration = 1f;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0); // start transparent
    }

    public void FadeAndLoad(string sceneName, float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        StartCoroutine(FadeRoutine(sceneName, duration));
    }

    private IEnumerator FadeRoutine(string sceneName, float duration)
    {
        // Fade out
        yield return Fade(0f, 1f, duration);

        // Load scene
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        // Small delay so scene settles
        yield return null;

        // Fade in
        yield return Fade(1f, 0f, duration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        var c = fadeImage.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
    }
}
