using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class SimpleSceneFader : MonoBehaviour
{
    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        DontDestroyOnLoad(gameObject);
    }

    public void FadeAndLoad(string sceneName, float duration)
    {
        StartCoroutine(FadeAndLoadRoutine(sceneName, Mathf.Max(0.01f, duration)));
    }

    IEnumerator FadeAndLoadRoutine(string scene, float t)
    {
        // Fade out
        float a = 0f;
        while (a < 1f) { a += Time.unscaledDeltaTime / t; group.alpha = Mathf.Clamp01(a); yield return null; }

        // Load
        SceneManager.LoadScene(scene);

        // Fade in (optional – comment out if you want to keep black)
        a = 1f;
        while (a > 0f) { a -= Time.unscaledDeltaTime / t; group.alpha = Mathf.Clamp01(a); yield return null; }
    }
}