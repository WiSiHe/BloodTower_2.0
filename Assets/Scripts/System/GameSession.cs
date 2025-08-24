using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string tutorialSceneName = "Tutorial";
    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Health / Sanity")]
    [SerializeField] private int   maxHearts = 3;
    [SerializeField] private int   hearts    = 3;     // 0..maxHearts
    [SerializeField] private float sanity    = 100f;  // 0..100

    [Header("Timer")]
    [SerializeField] private float runTime   = 0f;    // seconds
    [SerializeField] private bool  timerEnabled = false;

    [Header("Score (lower time = better)")]
    [Tooltip("Higher = more points for finishing quickly. Score = round(TimeScoreBase / max(1, runTimeSeconds))")]
    [SerializeField] private int timeScoreBase = 100000;

    [Header("Keys")]
    [Tooltip("Keys collected this run (IDs). Use AddKey/HasKey/RemoveKey/ClearKeys APIs.")]
    [SerializeField] private List<string> keysSerialized = new List<string>();
    private HashSet<string> keys = new HashSet<string>();

    // ---- Public read-only accessors ----
    public int   MaxHearts => maxHearts;
    public int   Hearts    => Mathf.Clamp(hearts, 0, maxHearts);
    public float Sanity    => Mathf.Clamp(sanity, 0f, 100f);
    public float RunTime   => Mathf.Max(0f, runTime);
    public int   Score     => Mathf.RoundToInt(timeScoreBase / Mathf.Max(1f, runTime)); // lower time => higher score
    public int   KeyCount  => keys.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Rehydrate HashSet from serialized list (useful during playmode enter)
        keys = new HashSet<string>(keysSerialized);
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
        runTime = 0f;
        timerEnabled = false;
        ClearKeys();
    }

    public void StartRunTimer(bool resetElapsed = false)
    {
        if (resetElapsed) runTime = 0f;
        timerEnabled = true;
    }

    public void StopRunTimer() => timerEnabled = false;

    // ---------- Health ----------
    public void Damage(int amount = 1)
    {
        hearts = Mathf.Clamp(hearts - Mathf.Abs(amount), 0, maxHearts);
        if (hearts <= 0) TriggerGameOver();
    }
    public void Heal(int amount = 1) => hearts = Mathf.Clamp(hearts + Mathf.Abs(amount), 0, maxHearts);

    // ---------- Sanity ----------
    public void AddSanityDelta(float delta) => sanity = Mathf.Clamp(sanity + delta, 0f, 100f);

    // ---------- Keys API ----------
    public bool AddKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId)) return false;
        bool added = keys.Add(keyId);
        SyncSerializedKeys();
        return added;
    }

    public bool HasKey(string keyId) => !string.IsNullOrWhiteSpace(keyId) && keys.Contains(keyId);

    public bool RemoveKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId)) return false;
        bool removed = keys.Remove(keyId);
        SyncSerializedKeys();
        return removed;
    }

    public void ClearKeys()
    {
        keys.Clear();
        SyncSerializedKeys();
    }

    private void SyncSerializedKeys()
    {
        keysSerialized.Clear();
        keysSerialized.AddRange(keys);
    }

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