using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string tutorialSceneName = "Tutorial";
    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private string gameOverSceneName  = "GameOver";

    [Header("Health / Sanity")]
    [SerializeField] private int   maxHearts = 3;
    [SerializeField] private int   hearts    = 3;     // 0..maxHearts
    [SerializeField] private float sanity    = 100f;  // 0..100

    [Header("Timer")]
    [SerializeField] private float runTime   = 0f;    // seconds
    [SerializeField] private bool  timerEnabled = false;

    [Header("Score (lower time = better)")]
    [SerializeField] private int timeScoreBase = 100000; // Score = round(base / max(1, seconds))

    [Header("Keys")]
    [SerializeField] private List<string> keysSerialized = new List<string>();
    private HashSet<StringWrapper> keys = new HashSet<StringWrapper>();

    [Header("Children Stats")]
    [SerializeField] private int childrenSaved = 0;
    [SerializeField] private int childrenKilled = 0;

    // --- Accessors ---
    public int   MaxHearts      => maxHearts;
    public int   Hearts         => Mathf.Clamp(hearts, 0, maxHearts);
    public float Sanity         => Mathf.Clamp(sanity, 0f, 100f);
    public float RunTime        => Mathf.Max(0f, runTime);
    public int   Score          => Mathf.RoundToInt(timeScoreBase / Mathf.Max(1f, runTime));
    public int   ChildrenSaved  => Mathf.Max(0, childrenSaved);
    public int   ChildrenKilled => Mathf.Max(0, childrenKilled);
    public int   KeyCount       => keys.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Rehydrate keys
        keys.Clear();
        foreach (var s in keysSerialized) keys.Add(new StringWrapper(s));
    }

    private void Update()
    {
        if (timerEnabled) runTime += Time.deltaTime;
    }

    // -------- Run lifecycle --------
    public void ResetForNewRun()
    {
        hearts = maxHearts;
        sanity = 100f;
        runTime = 0f;
        timerEnabled = false;

        childrenSaved = 0;
        childrenKilled = 0;

        ClearKeys();
    }

    public void StartRunTimer(bool resetElapsed = false)
    {
        if (resetElapsed) runTime = 0f;
        timerEnabled = true;
    }

    public void StopRunTimer() => timerEnabled = false;

    // -------- Health / Sanity --------
    public void Damage(int amount = 1)
    {
        hearts = Mathf.Clamp(hearts - Mathf.Abs(amount), 0, maxHearts);
        if (hearts <= 0) TriggerGameOver();
    }
    public void Heal(int amount = 1) => hearts = Mathf.Clamp(hearts + Mathf.Abs(amount), 0, maxHearts);
    public void AddSanityDelta(float delta) => sanity = Mathf.Clamp(sanity + delta, 0f, 100f);

    // -------- Children stats --------
    public void RecordChildSaved()  { childrenSaved++; }
    public void RecordChildKilled() { childrenKilled++; }

    // -------- Keys API --------
    public bool AddKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId)) return false;
        var added = keys.Add(new StringWrapper(keyId));
        SyncSerializedKeys();
        return added;
    }
    public bool HasKey(string keyId)       => !string.IsNullOrWhiteSpace(keyId) && keys.Contains(new StringWrapper(keyId));
    public bool RemoveKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId)) return false;
        var removed = keys.Remove(new StringWrapper(keyId));
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
        foreach (var w in keys) keysSerialized.Add(w.Value);
    }

    // -------- Scene helpers --------
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

    // Small wrapper so HashSet comparisons are case-sensitive and allocation-safe
    private struct StringWrapper
    {
        public string Value;
        public StringWrapper(string v) { Value = v; }
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override bool Equals(object obj) => obj is StringWrapper w && w.Value == Value;
    }
}