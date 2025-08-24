using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Reflection;

/// Displays victory stats & wires Restart/Quit. Robust to varying GameSession field names.
public class VictoryStatsUI : MonoBehaviour
{
    [Header("Optional Title (auto-filled from scene if empty)")]
    [SerializeField] private TMP_Text titleText;

    [Header("Stats Texts")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text savedText;
    [SerializeField] private TMP_Text killedText;
    [SerializeField] private TMP_Text sanityText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Destinations")]
    [SerializeField] private string tutorialScene  = "Tutorial";
    [SerializeField] private string startMenuScene = "StartMenu";

    [Header("Score Weights")]
    [SerializeField] private int timePenaltyPerSecond = 1000;   // lower time is better
    [SerializeField] private int bonusPerSavedChild   = 1500;
    [SerializeField] private int penaltyPerKilledChild= 2000;
    [SerializeField] private int sanityBonusPerPoint  = 40;
    [SerializeField] private int baseScore            = 120000;

    private void Awake()
    {
        if (restartButton) restartButton.onClick.AddListener(OnRestart);
        if (quitButton)    quitButton.onClick.AddListener(OnQuit);
    }

    private void OnDestroy()
    {
        if (restartButton) restartButton.onClick.RemoveListener(OnRestart);
        if (quitButton)    quitButton.onClick.RemoveListener(OnQuit);
    }

    private void Start()
    {
        var gs = GameSession.Instance;

        // Try multiple common names so we don't depend on exact property names.
        float runSeconds = SafeGetFloat(gs, new[] { "RunTimeSeconds", "ElapsedSeconds", "RunSeconds", "TimerSeconds", "RunTime", "ElapsedTime" }, 0f);
        int saved   = SafeGetInt(gs, new[] { "ChildrenSaved", "SavedChildren", "KidsSaved" }, 0);
        int killed  = SafeGetInt(gs, new[] { "ChildrenKilled", "KidsKilled", "KilledChildren" }, 0);
        int sanity  = SafeGetInt(gs, new[] { "CurrentSanity", "Sanity", "SanityValue", "SanityPercent" }, 50);

        if (timeText)   timeText.text   = $"Time: {FormatTime(runSeconds)}";
        if (savedText)  savedText.text  = $"Children Saved: {saved}";
        if (killedText) killedText.text = $"Children Consumed: {killed}";
        if (sanityText) sanityText.text = $"Sanity: {sanity}";

        long score = baseScore
                   - Mathf.RoundToInt(runSeconds) * timePenaltyPerSecond
                   + saved  * bonusPerSavedChild
                   - killed * penaltyPerKilledChild
                   + sanity * sanityBonusPerPoint;
        if (score < 0) score = 0;
        if (scoreText) scoreText.text = $"Score: {score:N0}";

        if (titleText && string.IsNullOrWhiteSpace(titleText.text))
        {
            string s = SceneManager.GetActiveScene().name; // e.g., Victory_Good
            titleText.text = s.Replace("Victory_", "").ToUpperInvariant() + " VICTORY";
        }
    }

    private void OnRestart() => LoadScene(tutorialScene);
    private void OnQuit()    => LoadScene(startMenuScene);

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

    private static string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return $"{s / 60:00}:{s % 60:00}";
    }

    // ---------- Reflection helpers ----------
    private static int SafeGetInt(object obj, string[] names, int fallback)
    {
        if (obj == null) return fallback;
        var t = obj.GetType();
        foreach (var n in names)
        {
            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanRead)
            {
                try { return ConvertToInt(p.GetValue(obj)); } catch { }
            }
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
            {
                try { return ConvertToInt(f.GetValue(obj)); } catch { }
            }
        }
        return fallback;
    }

    private static float SafeGetFloat(object obj, string[] names, float fallback)
    {
        if (obj == null) return fallback;
        var t = obj.GetType();
        foreach (var n in names)
        {
            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanRead)
            {
                try { return ConvertToFloat(p.GetValue(obj)); } catch { }
            }
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
            {
                try { return ConvertToFloat(f.GetValue(obj)); } catch { }
            }
        }
        return fallback;
    }

    private static int ConvertToInt(object v)
    {
        if (v == null) return 0;
        if (v is int i) return i;
        if (v is float f) return Mathf.RoundToInt(f);
        if (v is double d) return Mathf.RoundToInt((float)d);
        if (int.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0;
    }

    private static float ConvertToFloat(object v)
    {
        if (v == null) return 0f;
        if (v is float f) return f;
        if (v is int i) return i;
        if (v is double d) return (float)d;
        if (float.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0f;
    }
}