using UnityEngine;
using UnityEngine.SceneManagement;

public class BossFightManager : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string victorySceneName = "Victory"; // set in Inspector (or credits)
    [SerializeField] private string gameOverSceneName = "GameOver"; // fallback if needed

    [Header("Refs (optional)")]
    [SerializeField] private GameObject boss; // assign your King root; used for sanity checks

    private bool _ended;

    private void Awake()
    {
        // Ensure timescale sane when entering the arena
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        if (boss == null)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) boss = b;
        }
    }

    public void OnBossFell(GameObject bossGO)
    {
        if (_ended) return;
        _ended = true;

        // Remove boss and celebrate
        if (bossGO) Destroy(bossGO);

        // You can add VFX/SFX here, then load the scene
        LoadVictory();
    }

    public void OnPlayerDied()
    {
        if (_ended) return;
        _ended = true;

        LoadGameOver();
    }

    private void LoadVictory()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var fader = Object.FindFirstObjectByType<SceneFader>(FindObjectsInactive.Include);
#else
        var fader = Object.FindObjectOfType<SceneFader>(true);
#endif
        if (fader != null) fader.FadeToScene(victorySceneName);
        else SceneManager.LoadScene(victorySceneName);
    }

    private void LoadGameOver()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var fader = Object.FindFirstObjectByType<SceneFader>(FindObjectsInactive.Include);
#else
        var fader = Object.FindObjectOfType<SceneFader>(true);
#endif
        if (fader != null) fader.FadeToScene(gameOverSceneName);
        else SceneManager.LoadScene(gameOverSceneName);
    }
}