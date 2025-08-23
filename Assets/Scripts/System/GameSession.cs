using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string tutorialSceneName = "Tutorial";
    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Run State")]
    [SerializeField] private int   maxHearts    = 3;
    [SerializeField] private int   hearts       = 3;
    [SerializeField] private float sanity       = 100f; // 0..100
    [SerializeField] private float runTime      = 0f;   // seconds
    [SerializeField] private bool  timerEnabled = false;
    [SerializeField] private bool  runHasStarted = false;

    [Header("Score")]
    [SerializeField] private int score = 0;

    public int   MaxHearts   => maxHearts;
    public int   Hearts      => Mathf.Clamp(hearts, 0, maxHearts);
    public float Sanity      => Mathf.Clamp(sanity, 0f, 100f);
    public float RunTime     => Mathf.Max(0f, runTime);
    public int   Score       => Mathf.Max(0, score);
    public bool  TimerEnabled => timerEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (timerEnabled) runTime += Time.deltaTime;
    }

    // ---------- Run lifecycle ----------
    public void ResetForNewRun()
    {
        hearts = maxHearts;
        sanity = 100f;
        score  = 0;
        runTime = 0f;
        timerEnabled = false;
        runHasStarted = false;
    }

    public void StartRunTimer(bool resetElapsed = false)
    {
        if (resetElapsed) runTime = 0f;
        runHasStarted = true;
        timerEnabled = true;
        Debug.Log($"[GameSession] Timer STARTED (reset={resetElapsed}) | runTime={runTime:0.00}");
    }

    public void StopRunTimer()
    {
        timerEnabled = false;
        Debug.Log("[GameSession] Timer STOPPED");
    }

    // ---------- Health / Sanity / Score ----------
    public void Damage(int amount = 1)
    {
        hearts = Mathf.Clamp(hearts - Mathf.Abs(amount), 0, maxHearts);
        if (hearts <= 0) TriggerGameOver();
    }
    public void Heal(int amount = 1) => hearts = Mathf.Clamp(hearts + Mathf.Abs(amount), 0, maxHearts);
    public void AddSanityDelta(float delta) => sanity = Mathf.Clamp(sanity + delta, 0f, 100f);
    public void AddScore(int amount) => score = Mathf.Max(0, score + Mathf.Abs(amount));
    public void SetScore(int value)  => score = Mathf.Max(0, value);

    // ---------- Scene helpers ----------
    public void TriggerGameOver()
    {
        Time.timeScale = 1f;
        StopRunTimer();
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
        StopRunTimer();
        SceneManager.LoadScene(startMenuSceneName);
    }
}