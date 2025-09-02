using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// A simple controller for switching between UI panels with game pause functionality.
/// </summary>
public class MainUIController : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Array of UI panels to control. Index 0 will be active by default.")]
    public GameObject[] panels;
    
    [Header("Additional UI Elements")]
    [Tooltip("Additional UI GameObjects to hide/show with the pause menu (e.g., panel navigation buttons)")]
    public GameObject[] additionalUIElements;
    
    [Header("Settings")]
    [Tooltip("Index of the panel to show when this controller is enabled")]
    [SerializeField] private int _defaultPanelIndex = 0;
    
    [Tooltip("Whether to automatically show the default panel on enable")]
    [SerializeField] private bool _autoShowDefaultOnEnable = false;
    
    [Tooltip("Whether to pause the game when any panel is active")]
    [SerializeField] private bool _pauseGameOnPanelActive = true;
    
    [Header("Game Control Buttons")]
    [Tooltip("Button to quit the game")]
    [SerializeField] private Button _quitButton;
    
    [Tooltip("Button to restart the game")]
    [SerializeField] private Button _restartButton;
    
    [Tooltip("Button to resume the game")]
    [SerializeField] private Button _resumeButton;
    
    [Tooltip("Parent GameObject containing all buttons (optional - if not set, buttons are managed individually)")]
    [SerializeField] private GameObject _buttonsParent;
    
    [Header("Input Settings")]
    [Tooltip("Whether to allow pause toggle with keyboard input")]
    [SerializeField] private bool _allowKeyboardPause = true;
    
    private int _currentPanelIndex = -1;
    private bool _wasGamePaused = false;
    private InputSystem_Actions _inputActions;
    
    /// <summary>
    /// Gets the currently active panel index
    /// </summary>
    public int CurrentPanelIndex => _currentPanelIndex;
    
    /// <summary>
    /// Gets the number of panels
    /// </summary>
    public int PanelCount => panels != null ? panels.Length : 0;
    
    void Awake()
    {
        // Force hide all UI elements by default
        ForceHideAllUI();
        
        // Setup input actions
        _inputActions = new InputSystem_Actions();
    }
    
    void OnEnable()
    {
        // Setup button listeners
        SetupButtonListeners();
        
        // Setup input actions
        SetupInputActions();
        
        // Force hide everything on enable to ensure clean state
        ForceHideAllUI();
        
        if (_autoShowDefaultOnEnable)
        {
            SetActivePanel(_defaultPanelIndex);
        }
    }
    
    void OnDisable()
    {
        // Remove button listeners
        RemoveButtonListeners();
        
        // Cleanup input actions
        CleanupInputActions();
    }
    
    void Start()
    {
        // Ensure everything starts hidden and game is running
        ForceHideAllUI();
        
        // Ensure game is running (not paused)
        Time.timeScale = 1f;
        Debug.Log("[MainUIController] Game started - Time.timeScale: 1");
    }
    
    /// <summary>
    /// Sets up button click listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(OnQuitButtonPressed);
        }
        
        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(OnRestartButtonPressed);
        }
        
        if (_resumeButton != null)
        {
            _resumeButton.onClick.AddListener(OnResumeButtonPressed);
        }
    }
    
    /// <summary>
    /// Removes button click listeners
    /// </summary>
    private void RemoveButtonListeners()
    {
        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(OnQuitButtonPressed);
        }
        
        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(OnRestartButtonPressed);
        }
        
        if (_resumeButton != null)
        {
            _resumeButton.onClick.RemoveListener(OnResumeButtonPressed);
        }
    }
    
    /// <summary>
    /// Sets the active panel by index. Deactivates all other panels.
    /// </summary>
    /// <param name="index">Index of the panel to activate</param>
    public void SetActivePanel(int index)
    {
        if (panels == null || panels.Length == 0)
        {
            Debug.LogWarning("[MainUIController] No panels assigned!");
            return;
        }
        
        if (index < 0 || index >= panels.Length)
        {
            Debug.LogWarning($"[MainUIController] Panel index {index} is out of range (0-{panels.Length - 1})");
            return;
        }
        
        // Handle game pause state
        bool willShowPanel = index >= 0;
        HandleGamePause(willShowPanel);
        
        // Show/hide buttons based on panel state
        if (willShowPanel)
        {
            ShowAllButtons();
        }
        else
        {
            HideAllButtons();
        }
        
        // Deactivate all panels first
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
            {
                bool shouldBeActive = i == index;
                if (panels[i].activeSelf != shouldBeActive)
                {
                    panels[i].SetActive(shouldBeActive);
                }
            }
        }
        
        _currentPanelIndex = index;
        Debug.Log($"[MainUIController] Switched to panel {index}");
    }
    
    /// <summary>
    /// Sets the active panel by GameObject reference
    /// </summary>
    /// <param name="panel">The panel GameObject to activate</param>
    public void SetActivePanel(GameObject panel)
    {
        if (panels == null || panels.Length == 0)
        {
            Debug.LogWarning("[MainUIController] No panels assigned!");
            return;
        }
        
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == panel)
            {
                SetActivePanel(i);
                return;
            }
        }
        
        Debug.LogWarning("[MainUIController] Panel not found in panels array!");
    }
    
    /// <summary>
    /// Shows the next panel in the array (wraps to 0 if at end)
    /// </summary>
    public void ShowNextPanel()
    {
        if (panels == null || panels.Length == 0) return;
        
        int nextIndex = (_currentPanelIndex + 1) % panels.Length;
        SetActivePanel(nextIndex);
    }
    
    /// <summary>
    /// Shows the previous panel in the array (wraps to last if at beginning)
    /// </summary>
    public void ShowPreviousPanel()
    {
        if (panels == null || panels.Length == 0) return;
        
        int prevIndex = _currentPanelIndex <= 0 ? panels.Length - 1 : _currentPanelIndex - 1;
        SetActivePanel(prevIndex);
    }
    
    /// <summary>
    /// Hides all panels
    /// </summary>
    public void HideAllPanels()
    {
        if (panels == null) return;
        
        // Handle game pause state
        HandleGamePause(false);
        
        // Hide buttons when hiding panels
        HideAllButtons();
        
        // Force hide ALL panels
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
            {
                panels[i].SetActive(false);
            }
        }
        
        _currentPanelIndex = -1;
        Debug.Log("[MainUIController] All panels hidden");
    }
    
    /// <summary>
    /// Force hides all UI elements (panels and buttons) - useful for ensuring clean state
    /// </summary>
    public void ForceHideAllUI()
    {
        // Hide all panels
        if (panels != null)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                {
                    panels[i].SetActive(false);
                }
            }
        }
        
        // Hide all buttons and additional UI elements
        HideAllButtons();
        
        // Reset state
        _currentPanelIndex = -1;
        HandleGamePause(false);
        
        Debug.Log("[MainUIController] All UI elements force hidden");
    }
    
    /// <summary>
    /// Shows all panels (useful for debugging)
    /// </summary>
    public void ShowAllPanels()
    {
        if (panels == null) return;
        
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
            {
                panels[i].SetActive(true);
            }
        }
        
        Debug.Log("[MainUIController] All panels shown");
    }
    
    /// <summary>
    /// Shows all buttons
    /// </summary>
    public void ShowAllButtons()
    {
        // Show buttons parent if it exists
        if (_buttonsParent != null)
        {
            _buttonsParent.SetActive(true);
        }
        else
        {
            // Show individual buttons
            if (_quitButton != null) _quitButton.gameObject.SetActive(true);
            if (_restartButton != null) _restartButton.gameObject.SetActive(true);
            if (_resumeButton != null) _resumeButton.gameObject.SetActive(true);
        }
        
        // Show additional UI elements
        ShowAdditionalUIElements();
        
        Debug.Log("[MainUIController] All buttons shown");
    }
    
    /// <summary>
    /// Hides all buttons
    /// </summary>
    public void HideAllButtons()
    {
        // Hide buttons parent if it exists
        if (_buttonsParent != null)
        {
            _buttonsParent.SetActive(false);
        }
        else
        {
            // Hide individual buttons
            if (_quitButton != null) _quitButton.gameObject.SetActive(false);
            if (_restartButton != null) _restartButton.gameObject.SetActive(false);
            if (_resumeButton != null) _resumeButton.gameObject.SetActive(false);
        }
        
        // Hide additional UI elements
        HideAdditionalUIElements();
        
        Debug.Log("[MainUIController] All buttons hidden");
    }
    
    /// <summary>
    /// Shows additional UI elements
    /// </summary>
    private void ShowAdditionalUIElements()
    {
        if (additionalUIElements == null) return;
        
        for (int i = 0; i < additionalUIElements.Length; i++)
        {
            if (additionalUIElements[i] != null)
            {
                additionalUIElements[i].SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// Hides additional UI elements
    /// </summary>
    private void HideAdditionalUIElements()
    {
        if (additionalUIElements == null) return;
        
        for (int i = 0; i < additionalUIElements.Length; i++)
        {
            if (additionalUIElements[i] != null)
            {
                additionalUIElements[i].SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Handles game pause state based on panel visibility
    /// </summary>
    /// <param name="shouldPause">Whether the game should be paused</param>
    private void HandleGamePause(bool shouldPause)
    {
        if (!_pauseGameOnPanelActive) return;
        
        if (shouldPause)
        {
            // Pausing the game
            _wasGamePaused = Time.timeScale > 0f;
            Time.timeScale = 0f;
            Debug.Log($"[MainUIController] Game paused - Time.timeScale: {Time.timeScale}");
        }
        else
        {
            // Resuming the game
            Time.timeScale = 1f;
            Debug.Log($"[MainUIController] Game resumed - Time.timeScale: {Time.timeScale}");
        }
    }
    
    /// <summary>
    /// Called when quit button is pressed
    /// </summary>
    private void OnQuitButtonPressed()
    {
        Debug.Log("[MainUIController] Quit button pressed");
        
        // Resume game time before quitting
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
            // WebGL doesn't support Application.Quit()
            Debug.Log("Cannot quit in WebGL build");
        #else
            Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Called when restart button is pressed
    /// </summary>
    private void OnRestartButtonPressed()
    {
        Debug.Log("[MainUIController] Restart button pressed");
        
        // Resume game time before restarting
        Time.timeScale = 1f;
        
        // Use GameSession restart if available, otherwise restart current scene
        if (GameSession.Instance != null)
        {
            GameSession.Instance.RestartFromTop();
        }
        else
        {
            var activeScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(activeScene);
        }
    }
    
    /// <summary>
    /// Called when resume button is pressed
    /// </summary>
    private void OnResumeButtonPressed()
    {
        Debug.Log("[MainUIController] Resume button pressed");
        Debug.Log($"[MainUIController] Current Time.timeScale before resume: {Time.timeScale}");
        HideAllPanels();
        Debug.Log($"[MainUIController] Current Time.timeScale after resume: {Time.timeScale}");
    }
    
    /// <summary>
    /// Sets up input action event listeners
    /// </summary>
    private void SetupInputActions()
    {
        if (_inputActions == null) return;
        
        if (_allowKeyboardPause)
        {
            _inputActions.Player.Pause.performed += OnPausePerformed;
        }
        
        _inputActions.Enable();
    }
    
    /// <summary>
    /// Cleans up input action event listeners
    /// </summary>
    private void CleanupInputActions()
    {
        if (_inputActions == null) return;
        
        if (_allowKeyboardPause)
        {
            _inputActions.Player.Pause.performed -= OnPausePerformed;
        }
        
        _inputActions.Disable();
        _inputActions.Dispose();
        _inputActions = null;
    }
    
    /// <summary>
    /// Called when the pause action is performed
    /// </summary>
    /// <param name="context">Input action context</param>
    private void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        TogglePauseMenu();
    }
    
    /// <summary>
    /// Toggles the pause menu on/off
    /// </summary>
    public void TogglePauseMenu()
    {
        if (_currentPanelIndex >= 0)
        {
            // Currently showing a panel, force hide everything
            ForceHideAllUI();
            Debug.Log("[MainUIController] Pause menu hidden via toggle");
        }
        else
        {
            // No panel showing, show the default panel
            SetActivePanel(_defaultPanelIndex);
            Debug.Log("[MainUIController] Pause menu shown via toggle");
        }
    }
}
