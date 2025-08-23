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
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        int score = GameSession.Instance ? GameSession.Instance.Score : 0;
        if (scoreText != null) scoreText.text = $"Score: {score}";

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
        if (Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame)     OnRestartPressed();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) OnQuitPressed();
        if (Gamepad.current != null && Gamepad.current.bButton.wasPressedThisFrame)     OnQuitPressed();
    }
#endif
}