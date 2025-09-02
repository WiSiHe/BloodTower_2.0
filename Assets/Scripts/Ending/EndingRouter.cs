using UnityEngine;
using UnityEngine.SceneManagement;

/// Drop this on a single GameObject in your scene.
/// Call EndingRouter.GoToVictory(); or GoToGameOver();
public class EndingRouter : MonoBehaviour
{
    public static EndingRouter Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string goodScene     = "Victory_Good";
    [SerializeField] private string neutralScene  = "Victory_Neutral";
    [SerializeField] private string evilScene     = "Victory_Evil";
    [SerializeField] private string gameOverScene = "GameOver";

    [Header("Decision thresholds (tweak to taste)")]
    [Tooltip("If (Saved - Consumed) >= this and sanity >= minGoodSanity => GOOD.")]
    [SerializeField] private int   minGoodDelta  = 2;
    [SerializeField] private float minGoodSanity = 60f;

    [Tooltip("If Consumed >= this OR sanity <= maxEvilSanity => EVIL.")]
    [SerializeField] private int   minEvilConsumed = 2;
    [SerializeField] private float maxEvilSanity   = 30f;

    // Optional fade – leave null to use direct SceneManager
    [SerializeField] private SimpleSceneFader fader;
    [SerializeField] private float fadeOutSeconds = 0.45f;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!fader) fader = FindFirstObjectByType<SimpleSceneFader>(FindObjectsInactive.Include);
    }

    public static void GoToVictory()
    {
        if (!Instance) { Debug.LogWarning("EndingRouter missing. Loading neutral directly."); SceneManager.LoadScene("Victory_Neutral"); return; }
        
        // Record victory in GameSession for scoring
        if (GameSession.Instance != null)
        {
            GameSession.Instance.RecordVictory();
        }
        
        Instance.Load(Instance.ChooseVictoryScene());
    }

    public static void GoToGameOver()
    {
        if (!Instance) { SceneManager.LoadScene("GameOver"); return; }
        Instance.Load(Instance.gameOverScene);
    }

    string ChooseVictoryScene()
    {
        int   saved   = GameSession.Instance ? GameSession.Instance.ChildrenSaved  : 0;
        int   eaten   = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;
        float sanity  = GameSession.Instance ? GameSession.Instance.Sanity         : 50f;
        int   delta   = saved - eaten;

        if (delta >= minGoodDelta && sanity >= minGoodSanity) return goodScene;
        if (eaten >= minEvilConsumed || sanity <= maxEvilSanity) return evilScene;
        return neutralScene;
    }

    void Load(string sceneName)
    {
        if (fader != null) fader.FadeAndLoad(sceneName, fadeOutSeconds);
        else SceneManager.LoadScene(sceneName);
    }
}
