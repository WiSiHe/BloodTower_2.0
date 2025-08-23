using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;              // Keyboard, Gamepad
#endif

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;   // The whole pause menu panel (starts disabled)
    [SerializeField] private Button firstSelected;    // e.g., Continue button

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "StartMenu";

    [Header("Options")]
    [SerializeField] private bool lockCursorWhenPaused = false; // set true if you want a visible mouse

    private bool isPaused;

    private void Awake()
    {
        if (EventSystem.current == null)
            Debug.LogError("[PauseMenu] No EventSystem in scene. Add one via GameObject → UI → Event System.");

        if (pausePanel == null)
            Debug.LogError("[PauseMenu] Assign Pause Panel in inspector.", this);

        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void OnDisable()
    {
        // Safety: never leave the game frozen if this object gets disabled/destroyed.
        if (isPaused) InternalResume();
    }

    private void Update()
    {
        if (WasPausePressedThisFrame())
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    // ---------- Button Hooks ----------
    public void Resume()
    {
        InternalResume();
        Debug.Log("[PauseMenu] Resume");
    }

    public void RestartLevel()
    {
        Debug.Log("[PauseMenu] Restarting level");
        Time.timeScale = 1f;
        var active = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(active);
    }

    public void QuitToMainMenu()
    {
        Debug.Log("[PauseMenu] Quit to Main Menu");
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[PauseMenu] Quit Game requested");
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // Not supported; you could show a “Thanks for playing” overlay instead.
        Debug.Log("[PauseMenu] Application.Quit not supported on WebGL.");
#else
        Application.Quit();
#endif
    }

    // ---------- Core ----------
    private void Pause()
    {
        if (pausePanel == null) return;

        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);

        if (lockCursorWhenPaused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Focus first button so gamepad/keyboard can navigate immediately
        if (firstSelected != null && firstSelected.gameObject.activeInHierarchy && firstSelected.IsInteractable())
        {
            EventSystem.current?.SetSelectedGameObject(firstSelected.gameObject);
            firstSelected.OnSelect(null);
        }
    }

    private void InternalResume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (lockCursorWhenPaused)
        {
            // Optional: restore to whatever your game uses; here we just hide/unlock defaults
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;
        }

        // Clear UI selection to avoid accidental submits
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // ---------- Input helpers ----------
    private bool WasPausePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        bool kb = Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame);
        bool gp = Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.selectButton.wasPressedThisFrame);
        return kb || gp;
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
    }
}