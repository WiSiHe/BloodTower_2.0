using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persists between scenes; tracks the run score and provides scene helpers.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string tutorialSceneName = "Tutorial";
    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Run State")]
    [SerializeField] private int score = 0;
    public int Score => score;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ----- Score API -----
    public void ResetForNewRun() => score = 0;
    public void AddScore(int amount) => score = Mathf.Max(0, score + amount);
    public void SetScore(int value) => score = Mathf.Max(0, value);

    // ----- Scene helpers -----
    public void TriggerGameOver()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameOverSceneName);
    }

    public void RestartFromTop()
    {
        Time.timeScale = 1f;
        ResetForNewRun();
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void QuitToStartMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startMenuSceneName);
    }
}