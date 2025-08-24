using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;      // optional
    [SerializeField] private TMP_Text timeText;       // optional
    [SerializeField] private Button   restartButton;  // first selected
    [SerializeField] private Button   quitButton;

    [Header("Scene Names (fallback if GameSession missing)")]
    [SerializeField] private string tutorialSceneName = "Tutorial";
    [SerializeField] private string mainMenuSceneName = "StartMenu";

    private void Awake()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        UIUtilities.EnsureSingleEventSystem();
        // NEW: single call that unblocks any invisible overlays (incl. DDOL objects)
        UIUtilities.ForceUnblockAllRaycastsEverywhere();
    }

    private void OnEnable()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es && restartButton && restartButton.IsInteractable() && restartButton.gameObject.activeInHierarchy)
        {
            es.SetSelectedGameObject(restartButton.gameObject);
            restartButton.OnSelect(null);
        }
    }

    private void Start()
    {
        if (GameSession.Instance != null)
        {
            if (timeText)
            {
                float t = GameSession.Instance.RunTime;
                int mm = Mathf.FloorToInt(t / 60f);
                int ss = Mathf.FloorToInt(t % 60f);
                timeText.text = $"Time: {mm:00}:{ss:00}";
            }
            if (scoreText) scoreText.text = $"Score: {GameSession.Instance.Score}";
        }
        else
        {
            if (timeText)  timeText.text  = "Time: 00:00";
            if (scoreText) scoreText.text = "Score: 0";
        }
    }

    public void OnRestartPressed()
    {
        if (GameSession.Instance != null) GameSession.Instance.RestartFromTop();
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(tutorialSceneName);
        }
    }

    public void OnQuitPressed()
    {
        if (GameSession.Instance != null) GameSession.Instance.QuitToStartMenu();
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
    }

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