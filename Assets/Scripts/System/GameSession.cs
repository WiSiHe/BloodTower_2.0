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

    [Header("Score")]
    [SerializeField] private int baseScore = 1000; // Base score for completing the game
    [SerializeField] private int timeBonusMultiplier = 100; // Bonus points per second survived
    [SerializeField] private int childrenBonus = 500; // Bonus per child saved
    [SerializeField] private int childrenPenalty = -200; // Penalty per child killed
    [SerializeField] private int victoryBonus = 5000; // Bonus for winning the game

    [Header("Keys")]
    [SerializeField] private List<string> keysSerialized = new List<string>();
    private HashSet<StringWrapper> keys = new HashSet<StringWrapper>();

    [Header("Children Stats")]
    [SerializeField] private int childrenSaved = 0;
    [SerializeField] private int childrenKilled = 0;
    
    // Game state
    private bool _hasWon = false;

    // --- Accessors ---
    public int   MaxHearts      => maxHearts;
    public int   Hearts         => Mathf.Clamp(hearts, 0, maxHearts);
    public float Sanity         => Mathf.Clamp(sanity, 0f, 100f);
    public float RunTime        => Mathf.Max(0f, runTime);
    public int   Score          => CalculateScore();
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
        
        // Subscribe to scene loading to ensure GameSession persists
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ensure GameSession is always present and active
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        // Start timer when entering gameplay scenes (not menus)
        if (!scene.name.Contains("Menu") && !scene.name.Contains("GameOver"))
        {
            StartRunTimer();
        }
        else
        {
            StopRunTimer();
        }
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
        _hasWon = false;

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
    
    // -------- Score Calculation --------
    private int CalculateScore()
    {
        int score = baseScore;
        
        // Time bonus: reward surviving longer
        score += Mathf.RoundToInt(runTime * timeBonusMultiplier);
        
        // Children bonuses/penalties
        score += childrenSaved * childrenBonus;
        score += childrenKilled * childrenPenalty;
        
        // Health bonus: reward finishing with more hearts
        score += hearts * 100; // 100 points per remaining heart
        
        // Sanity bonus: reward maintaining sanity
        score += Mathf.RoundToInt(sanity * 2); // 2 points per sanity point
        
        // Victory bonus: massive bonus for winning the game
        if (_hasWon)
        {
            score += victoryBonus;
        }
        
        return Mathf.Max(0, score); // Never negative score
    }

    // -------- Children stats --------
    public void RecordChildSaved()  { childrenSaved++; }
    public void RecordChildKilled() { childrenKilled++; }
    
    // -------- Victory --------
    public void RecordVictory()
    {
        _hasWon = true;
        StopRunTimer(); // Stop timer when game is won
        Debug.Log($"[GameSession] Player won! Final score: {Score}");
    }
    
    public bool HasWon => _hasWon;

    // -------- Keys API --------
    public bool AddKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId)) 
        {
            Debug.LogWarning("[GameSession] Attempted to add empty or null key ID.");
            return false;
        }
        var added = keys.Add(new StringWrapper(keyId));
        SyncSerializedKeys();
        Debug.Log($"[GameSession] Added key '{keyId}'. Success: {added}. Total keys: {keys.Count}");
        return added;
    }
    public bool HasKey(string keyId)       
    {
        if (string.IsNullOrWhiteSpace(keyId)) 
        {
            Debug.LogWarning("[GameSession] Checking for empty or null key ID.");
            return false;
        }
        bool hasKey = keys.Contains(new StringWrapper(keyId));
        Debug.Log($"[GameSession] HasKey('{keyId}'): {hasKey}. Total keys: {keys.Count}");
        return hasKey;
    }
    public bool RemoveKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId)) 
        {
            Debug.LogWarning("[GameSession] Attempted to remove empty or null key ID.");
            return false;
        }
        var removed = keys.Remove(new StringWrapper(keyId));
        SyncSerializedKeys();
        Debug.Log($"[GameSession] Removed key '{keyId}'. Success: {removed}. Total keys: {keys.Count}");
        return removed;
    }
    public void ClearKeys()
    {
        keys.Clear();
        SyncSerializedKeys();
    }
    
    // Debug helper to list all keys
    public string[] GetAllKeys()
    {
        var keyArray = new string[keys.Count];
        int i = 0;
        foreach (var key in keys)
        {
            keyArray[i++] = key.Value;
        }
        return keyArray;
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
        Debug.Log($"[GameSession] RestartFromTop called. Loading scene: {tutorialSceneName}");
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
