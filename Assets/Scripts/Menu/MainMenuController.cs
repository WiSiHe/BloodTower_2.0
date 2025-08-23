using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject creditsPanel;          // Panel shown for credits (can start disabled)

    [Header("Buttons (Main Menu)")]
    [SerializeField] private Button startButton;               // First selected when menu opens
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("Buttons (Credits)")]
    [SerializeField] private Button backButton;                // Inside credits panel (returns to main menu)

    [Header("Scenes")]
    [SerializeField] private string introSceneName = "Intro";  // MainMenu -> Intro

    [Header("Navigation Settings")]
    [SerializeField, Tooltip("Min absolute stick/dpad value to count as a move")]
    private float moveDeadzone = 0.5f;
    [SerializeField, Tooltip("Seconds between repeated moves when holding a direction")]
    private float repeatDelay = 0.20f;

    private List<Selectable> _mainItems = new();
    private List<Selectable> _creditsItems = new();
    private List<Selectable> _activeItems;
    private int _index = 0;
    private float _lastMoveTime = -999f;

    private void Awake()
    {
        // Safety checks
        if (EventSystem.current == null)
            Debug.LogError("[MainMenu] No EventSystem in scene. Add one via GameObject → UI → Event System.");

        if (!startButton || !creditsButton || !quitButton)
            Debug.LogError("[MainMenu] Assign Start/Credits/Quit buttons in the Inspector.", this);

        if (!backButton)
            Debug.LogWarning("[MainMenu] Back button (credits) is not assigned. Credits will still open, but B/Escape won’t close unless wired elsewhere.", this);

        // Build lists
        if (startButton)  _mainItems.Add(startButton);
        if (creditsButton)_mainItems.Add(creditsButton);
        if (quitButton)   _mainItems.Add(quitButton);

        if (backButton)   _creditsItems.Add(backButton);
    }

    private void OnEnable()
    {
        // Start on main menu
        ShowMainMenu();
    }

    // ====================== Button Handlers ======================

    public void StartGame()
    {
        Debug.Log("[MainMenu] StartGame -> " + introSceneName);
        Time.timeScale = 1f;
        SceneManager.LoadScene(introSceneName);
    }

    public void ShowCredits()
    {
        if (!creditsPanel)
        {
            Debug.LogWarning("[MainMenu] No creditsPanel assigned.");
            return;
        }

        creditsPanel.SetActive(true);
        // Prefer a CanvasGroup if present to ensure interactability
        var cg = creditsPanel.GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }

        SwitchActiveItems(_creditsItems);
        SetSelection(0); // focus Back button
        Debug.Log("[MainMenu] Credits shown");
    }

    public void HideCredits()
    {
        if (creditsPanel)
        {
            var cg = creditsPanel.GetComponent<CanvasGroup>();
            if (cg) { cg.interactable = false; cg.blocksRaycasts = false; cg.alpha = 0f; }
            creditsPanel.SetActive(false);
        }

        ShowMainMenu();
        Debug.Log("[MainMenu] Credits hidden");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[MainMenu] Quit (Editor) — stopping Play Mode.");
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Debug.Log("[MainMenu] Quit (WebGL) not supported.");
#else
        Debug.Log("[MainMenu] Quit — exiting application.");
        Application.Quit();
#endif
    }

    // ====================== Navigation Core ======================

    private void ShowMainMenu()
    {
        SwitchActiveItems(_mainItems);
        SetSelection(0); // focus Start
    }

    private void SwitchActiveItems(List<Selectable> items)
    {
        _activeItems = items;
        _index = Mathf.Clamp(_index, 0, Mathf.Max(0, _activeItems.Count - 1));
    }

    private void SetSelection(int newIndex)
    {
        if (_activeItems == null || _activeItems.Count == 0) return;
        _index = Mathf.Clamp(newIndex, 0, _activeItems.Count - 1);

        var target = _activeItems[_index];
        if (target && target.gameObject.activeInHierarchy && target.IsInteractable())
        {
            EventSystem.current?.SetSelectedGameObject(target.gameObject);
            // For keyboard/gamepad hint, also move UI highlight
            target.OnSelect(null);
        }
    }

    private void MoveSelection(int direction) // +1 down, -1 up
    {
        if (_activeItems == null || _activeItems.Count <= 1) return;

        int start = _index;
        int count = _activeItems.Count;

        for (int step = 1; step <= count; step++)
        {
            int tryIndex = (start + direction * step + count) % count;
            var candidate = _activeItems[tryIndex];
            if (candidate && candidate.gameObject.activeInHierarchy && candidate.IsInteractable())
            {
                SetSelection(tryIndex);
                break;
            }
        }
    }

    private void SubmitCurrent()
    {
        if (_activeItems == null || _activeItems.Count == 0) return;

        var current = _activeItems[_index];
        if (current is Button btn && btn.IsInteractable())
        {
            btn.onClick.Invoke();
        }
    }

    // ====================== Input Handling ======================

    private void Update()
    {
        float vertical = GetMenuVertical();
        bool moved = Mathf.Abs(vertical) >= moveDeadzone;

        if (moved && Time.unscaledTime - _lastMoveTime >= repeatDelay)
        {
            _lastMoveTime = Time.unscaledTime;
            MoveSelection(vertical < 0f ? +1 : -1); // Down = +1, Up = -1
        }

        if (WasSubmitPressedThisFrame())
        {
            SubmitCurrent();
        }

        if (WasCancelPressedThisFrame())
        {
            // If credits is open, go back; otherwise do nothing (or open credits if you prefer)
            if (creditsPanel && creditsPanel.activeSelf)
                HideCredits();
        }
    }

    private float GetMenuVertical()
    {
#if ENABLE_INPUT_SYSTEM
        float v = 0f;
        if (Gamepad.current != null)
        {
            v = Mathf.Abs(Gamepad.current.leftStick.ReadValue().y) > Mathf.Abs(v) ? Gamepad.current.leftStick.ReadValue().y : v;
            v = Mathf.Abs(Gamepad.current.dpad.y.ReadValue()) > Mathf.Abs(v) ? Gamepad.current.dpad.y.ReadValue() : v;
        }
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || (Keyboard.current.wKey?.wasPressedThisFrame ?? false)) return +1f;
            if (Keyboard.current.downArrowKey.wasPressedThisFrame || (Keyboard.current.sKey?.wasPressedThisFrame ?? false)) return -1f;
        }
        return v;
#else
        // Legacy input fallback
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return +1f;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return -1f;
        return Input.GetAxisRaw("Vertical");
#endif
    }

    private bool WasSubmitPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        bool pad = Gamepad.current != null && (Gamepad.current.aButton.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame);
        bool kb  = Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        return pad || kb;
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Submit");
#endif
    }

    private bool WasCancelPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        bool pad = Gamepad.current != null && (Gamepad.current.bButton.wasPressedThisFrame || Gamepad.current.selectButton.wasPressedThisFrame);
        bool kb  = Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame);
        return pad || kb;
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel");
#endif
    }
}