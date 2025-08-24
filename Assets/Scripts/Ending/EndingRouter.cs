using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

/// Chooses which Victory scene to load based on GameSession stats.
/// Works even if your GameSession uses different member names (via reflection).
[DefaultExecutionOrder(-500)]
public class EndingRouter : MonoBehaviour
{
    public static EndingRouter Instance { get; private set; }

    [Header("Victory Scenes (Build Settings)")]
    [SerializeField] private string sceneGood    = "Victory_Good";
    [SerializeField] private string sceneNeutral = "Victory_Neutral";
    [SerializeField] private string sceneEvil    = "Victory_Evil";

    [Header("Good ending rules")]
    [SerializeField] private int mercyMinSaved  = 2;
    [SerializeField] private int mercyMaxKilled = 0;
    [SerializeField] private int mercyMinSanity = 50;

    [Header("Evil ending rules")]
    [SerializeField] private int corruptMinKilled = 2;
    [SerializeField] private int corruptMaxSanity = 30;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadEndingScene()
    {
        var gs = GameSession.Instance;

        int saved  = SafeGetInt(gs, new[] { "ChildrenSaved", "SavedChildren", "KidsSaved" }, 0);
        int killed = SafeGetInt(gs, new[] { "ChildrenKilled", "KidsKilled", "KilledChildren" }, 0);
        int sanity = SafeGetInt(gs, new[] { "CurrentSanity", "Sanity", "SanityValue", "SanityPercent" }, 50);

        bool isGood = (saved >= mercyMinSaved) && (killed <= mercyMaxKilled) && (sanity >= mercyMinSanity);
        bool isEvil = (killed >= corruptMinKilled) && (sanity <= corruptMaxSanity) && (killed > saved);

        string target = isGood ? sceneGood : (isEvil ? sceneEvil : sceneNeutral);

#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var fader = Object.FindFirstObjectByType<SceneFader>(FindObjectsInactive.Include);
#else
        var fader = Object.FindObjectOfType<SceneFader>(true);
#endif
        Time.timeScale = 1f;
        if (fader != null) fader.FadeToScene(target);
        else SceneManager.LoadScene(target);
    }

    // ---------- Helpers ----------
    private static int SafeGetInt(object obj, string[] names, int fallback)
    {
        if (obj == null) return fallback;
        var t = obj.GetType();
        foreach (var n in names)
        {
            // property
            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanRead)
            {
                try { return ConvertToInt(p.GetValue(obj)); } catch { }
            }
            // field
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
            {
                try { return ConvertToInt(f.GetValue(obj)); } catch { }
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

    // Editor shortcuts for testing
    [ContextMenu("Test → Good")]    private void TestGood()    => SceneManager.LoadScene(sceneGood);
    [ContextMenu("Test → Neutral")] private void TestNeutral() => SceneManager.LoadScene(sceneNeutral);
    [ContextMenu("Test → Evil")]    private void TestEvil()    => SceneManager.LoadScene(sceneEvil);
}