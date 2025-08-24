using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class SceneFader : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f;
    private CanvasGroup cg;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        HideAndUnblock();
        DontDestroyOnLoad(gameObject);
    }

    // PUBLIC: call this from other scripts (e.g., LevelExit)
    public void FadeToScene(string sceneName)
    {
        StopAllCoroutines();
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        cg.blocksRaycasts = true;
        cg.interactable = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        if (Time.timeScale == 0f) Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(sceneName);

        t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / duration);
            yield return null;
        }

        HideAndUnblock();
    }

    private void HideAndUnblock()
    {
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        gameObject.SetActive(true);
    }
}