using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// Displays victory stats & wires Restart/Quit buttons using GameSession data.
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

    // Note: Score calculation is now handled by GameSession.CalculateScore()

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
        
        if (gs == null)
        {
            Debug.LogWarning("[VictoryStatsUI] GameSession.Instance is null!");
            return;
        }

        // Use the actual GameSession properties
        float runSeconds = gs.RunTime;
        int saved = gs.ChildrenSaved;
        int killed = gs.ChildrenKilled;
        float sanity = gs.Sanity;
        int score = gs.Score;

        // Display the stats
        if (timeText)   timeText.text   = $"Time: {FormatTime(runSeconds)}";
        if (savedText)  savedText.text  = $"Children Saved: {saved}";
        if (killedText) killedText.text = $"Children Consumed: {killed}";
        if (sanityText) sanityText.text = $"Sanity: {sanity:F0}";
        if (scoreText)  scoreText.text  = $"Score: {score:N0}";

        // Set title if empty
        if (titleText && string.IsNullOrWhiteSpace(titleText.text))
        {
            string sceneName = SceneManager.GetActiveScene().name; // e.g., Victory_Good
            titleText.text = sceneName.Replace("Victory_", "").ToUpperInvariant() + " VICTORY";
        }
        
        Debug.Log($"[VictoryStatsUI] Displayed stats - Time: {runSeconds:F1}s, Saved: {saved}, Killed: {killed}, Sanity: {sanity:F0}, Score: {score}");
    }

    private void OnRestart()
    {
        Debug.Log("[VictoryStatsUI] Restart button pressed");
        
        // Use GameSession restart if available, otherwise fallback to direct scene load
        if (GameSession.Instance != null)
        {
            GameSession.Instance.RestartFromTop();
        }
        else
        {
            LoadScene(tutorialScene);
        }
    }
    
    private void OnQuit()
    {
        Debug.Log("[VictoryStatsUI] Quit button pressed");
        
        // Use GameSession quit if available, otherwise fallback to direct scene load
        if (GameSession.Instance != null)
        {
            GameSession.Instance.QuitToStartMenu();
        }
        else
        {
            LoadScene(startMenuScene);
        }
    }

    private void LoadScene(string scene)
    {
        Debug.Log($"[VictoryStatsUI] Loading scene: {scene}");
        
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


}
