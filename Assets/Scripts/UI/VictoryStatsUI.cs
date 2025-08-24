using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class VictoryStatsUI : MonoBehaviour
{
    [Header("Texts (optional)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;   // multiline text block
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Title Overrides (optional)")]
    [SerializeField] private string goodTitle    = "Salvation";
    [SerializeField] private string neutralTitle = "A Lonely Ascent";
    [SerializeField] private string evilTitle    = "Blood Crown";

    private void Start()
    {
        var gs = GameSession.Instance;

        // Detect which ending we’re in by scene name (or put an EndingKind on the scene if you prefer)
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (titleText)
        {
            if (scene.Contains("Good"))      titleText.text = goodTitle;
            else if (scene.Contains("Evil")) titleText.text = evilTitle;
            else                              titleText.text = neutralTitle;
        }

        if (gs)
        {
            int saved  = gs.ChildrenSaved;
            int killed = gs.ChildrenKilled;
            float t    = gs.RunTime;
            int   mm   = Mathf.FloorToInt(t / 60f);
            int   ss   = Mathf.FloorToInt(t % 60f);

            if (summaryText)
                summaryText.text = $"Children saved: {saved}\nChildren… consumed: {killed}";

            if (timeText)  timeText.text  = $"Time: {mm:00}:{ss:00}";
            if (scoreText) scoreText.text = $"Score: {gs.Score}";
        }

        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es && restartButton && restartButton.IsInteractable())
        {
            es.SetSelectedGameObject(restartButton.gameObject);
            restartButton.OnSelect(null);
        }
    }

    public void OnRestartPressed() => GameSession.Instance?.RestartFromTop();
    public void OnQuitPressed()    => GameSession.Instance?.QuitToStartMenu();

#if ENABLE_INPUT_SYSTEM
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) OnRestartPressed();
        if (Gamepad.current  != null && Gamepad.current.aButton.wasPressedThisFrame)   OnRestartPressed();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) OnQuitPressed();
        if (Gamepad.current  != null && Gamepad.current.bButton.wasPressedThisFrame)     OnQuitPressed();
    }
#endif
}