using UnityEngine;
using UnityEngine.SceneManagement;

public class BossFightManager : MonoBehaviour
{
    [Header("Fallback scenes (only used if no EndingRouter is found)")]
    [SerializeField] private string victorySceneName  = "Victory_Neutral";
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Refs")]
    [SerializeField] private GameObject boss;            // optional: your King root
    [SerializeField] private EndingRouter endingRouter;  // optional; auto-found if left empty

    private bool ended;

    private void Awake()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        if (!endingRouter) endingRouter = FindObjectOfType<EndingRouter>(true);
        if (!boss)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) boss = b;
        }
    }

    public void OnBossFell(GameObject bossGO)
    {
        if (ended) return; ended = true;
        if (bossGO) Destroy(bossGO);

        if (endingRouter != null) endingRouter.LoadEndingScene();
        else LoadScene(victorySceneName);
    }

    public void OnPlayerDied()
    {
        if (ended) return; ended = true;
        LoadScene(gameOverSceneName);
    }

    private void LoadScene(string scene)
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var fader = Object.FindFirstObjectByType<SceneFader>(FindObjectsInactive.Include);
#else
        var fader = Object.FindObjectOfType<SceneFader>(true);
#endif
        Time.timeScale = 1f;
        if (fader != null) fader.FadeToScene(scene);
        else SceneManager.LoadScene(scene);
    }
}