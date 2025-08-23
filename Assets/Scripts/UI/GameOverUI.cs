using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Attach in the GameOver scene; wires buttons and shows the score.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button restartButton;   // optional: first selected
    [SerializeField] private Button quitButton;

    private void Start()
    {
        int score = GameSession.Instance ? GameSession.Instance.Score : 0;
        if (scoreText) scoreText.text = $"Score: {score}";

        // focus Restart for pad/keyboard
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es && restartButton && restartButton.IsInteractable())
        {
            es.SetSelectedGameObject(restartButton.gameObject);
            restartButton.OnSelect(null);
        }
    }

    // ---- Button hooks ----
    public void OnRestartPressed()
    {
        if (GameSession.Instance) GameSession.Instance.RestartFromTop();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial");
    }

    public void OnQuitPressed()
    {
        if (GameSession.Instance) GameSession.Instance.QuitToStartMenu();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("StartMenu");
    }

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