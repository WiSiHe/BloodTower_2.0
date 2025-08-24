using UnityEngine;
using UnityEngine.SceneManagement;

public enum EndingKind { Good, Neutral, Evil }

public class EndingRouter : MonoBehaviour
{
    [Header("Ending Scenes (must be in Build Settings)")]
    [SerializeField] private string goodSceneName    = "Victory_Good";
    [SerializeField] private string neutralSceneName = "Victory_Neutral";
    [SerializeField] private string evilSceneName    = "Victory_Evil";

    [Header("Rules")]
    [Tooltip("Good ending if saved >= this many.")]
    [SerializeField] private int goodMinSaved = 3;

    [Tooltip("If true, any kill disqualifies the Good ending.")]
    [SerializeField] private bool requireZeroKillsForGood = true;

    [Tooltip("Evil ending if kills >= this many, regardless of saves.")]
    [SerializeField] private int evilMinKills = 5;

    public EndingKind GetEnding()
    {
        int saved  = GameSession.Instance ? GameSession.Instance.ChildrenSaved  : 0;
        int killed = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;

        // Evil dominates if threshold reached
        if (killed >= evilMinKills) return EndingKind.Evil;

        // Good requires enough saved (and optionally zero kills)
        if (saved >= goodMinSaved && (!requireZeroKillsForGood || killed == 0))
            return EndingKind.Good;

        return EndingKind.Neutral;
    }

    public void LoadEndingScene()
    {
        var kind = GetEnding();
        string scene =
            kind == EndingKind.Good    ? goodSceneName :
            kind == EndingKind.Evil    ? evilSceneName :
                                         neutralSceneName;

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