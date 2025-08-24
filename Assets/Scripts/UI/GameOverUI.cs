using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;      // optional (drag a TMP text or leave null)
    [SerializeField] private Button   restartButton;
    [SerializeField] private Button   quitButton;

    private void Start()
    {
        float t = GameSession.Instance ? GameSession.Instance.RunTime : 0f;
        int   s = GameSession.Instance ? GameSession.Instance.Score   : 0;

        if (timeText != null)
        {
            int mm = Mathf.FloorToInt(t / 60f);
            int ss = Mathf.FloorToInt(t % 60f);
            timeText.text = $"Time: {mm:00}:{ss:00}";
        }

        if (scoreText != null)
            scoreText.text = $"Score: {s}";

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