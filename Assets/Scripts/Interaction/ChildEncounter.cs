using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class ChildEncounter : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool oneShot = true;

    [Header("Effects")]
    [SerializeField] private int   healHearts    = 1;     // when drinking
    [SerializeField] private float sanityPenalty = -20f;  // negative = reduce sanity

    [Header("UI (World-space)")]
    [SerializeField] private Canvas      promptCanvas;   // world-space child canvas
    [SerializeField] private CanvasGroup promptGroup;    // to toggle visibility / raycasts
    [SerializeField] private TMP_Text    promptText;     // optional
    [SerializeField] private Button      saveButton;
    [SerializeField] private Button      drinkButton;
    [SerializeField] private Button      cancelButton;   // optional

    [Header("Player control")]
    [Tooltip("Exact class name of the movement script to disable temporarily (e.g., 'PlayerController').")]
    [SerializeField] private string playerControllerComponentName = "PlayerController";
    [Tooltip("Freeze the player's Rigidbody2D while the prompt is open.")]
    [SerializeField] private bool freezeRigidbodyWhilePrompt = true;

    [Header("Safety / Watchdog")]
    [Tooltip("If the player stays locked longer than this (seconds), auto-unlock.")]
    [SerializeField] private float maxLockSeconds = 5.0f; // Reduced from 20 to 5 seconds
    [Tooltip("Allow keyboard/controller cancel while prompt is up (Esc/B).")]
    [SerializeField] private bool allowQuickCancel = true;
    [Tooltip("Emergency unlock key combination (hold both keys for 1 second).")]
    [SerializeField] private bool enableEmergencyUnlock = true;

    // Runtime state
    private bool promptVisible;
    private bool used;
    private float lockStartTime;
    private bool hasPrevConstraints;
    private bool isUnlocking = false; // Prevent repeated unlock calls
    
    // Emergency unlock system
    private float emergencyKeyHoldTime = 0f;
    private bool emergencyKeysHeld = false;

    private GameObject    playerGO;
    private MonoBehaviour cachedController;
    private Rigidbody2D   playerRB;

    private bool prevControllerEnabled;
    private RigidbodyConstraints2D prevConstraints;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        HidePromptImmediate();

        if (saveButton)  saveButton.onClick.AddListener(OnChooseSave);
        if (drinkButton) drinkButton.onClick.AddListener(OnChooseDrink);
        if (cancelButton) cancelButton.onClick.AddListener(ClosePromptOnly);
    }

    private void OnDisable()
    {
        ForceUnlockPlayer(); // if prefab is disabled mid-interaction
    }

    private void OnDestroy()
    {
        ForceUnlockPlayer(); // if prefab is destroyed mid-interaction

        if (saveButton)  saveButton.onClick.RemoveListener(OnChooseSave);
        if (drinkButton) drinkButton.onClick.RemoveListener(OnChooseDrink);
        if (cancelButton) cancelButton.onClick.RemoveListener(ClosePromptOnly);
    }

    private void Update()
    {
        if (!promptVisible) return;

        // Watchdog: if somehow we remain locked too long, unlock automatically.
        if ((Time.unscaledTime - lockStartTime) > maxLockSeconds && !isUnlocking)
        {
            ForceUnlockPlayer();
            return;
        }

#if ENABLE_INPUT_SYSTEM
        if (allowQuickCancel)
        {
            // Escape or B button = cancel/close
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if ((kb != null && kb.escapeKey.wasPressedThisFrame) ||
                (gp != null && (gp.bButton.wasPressedThisFrame || gp.startButton.wasPressedThisFrame)))
            {
                ClosePromptOnly();
                return;
            }
        }

        // Emergency unlock: Hold Shift+Ctrl for 1 second
        if (enableEmergencyUnlock)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                bool shiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                bool ctrlHeld = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
                
                if (shiftHeld && ctrlHeld)
                {
                    emergencyKeyHoldTime += Time.unscaledDeltaTime;
                    if (emergencyKeyHoldTime >= 1.0f && !emergencyKeysHeld && !isUnlocking)
                    {
                        ForceUnlockPlayer();
                        emergencyKeysHeld = true;
                    }
                }
                else
                {
                    emergencyKeyHoldTime = 0f;
                    emergencyKeysHeld = false;
                }
            }
        }
        
        // Global emergency unlock: Hold Shift+Alt+Ctrl for 2 seconds (works even when not in child encounter)
        if (enableEmergencyUnlock)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                bool shiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                bool altHeld = kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;
                bool ctrlHeld = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
                
                if (shiftHeld && altHeld && ctrlHeld)
                {
                    emergencyKeyHoldTime += Time.unscaledDeltaTime;
                    if (emergencyKeyHoldTime >= 2.0f && !emergencyKeysHeld && !isUnlocking)
                    {
                        GlobalEmergencyUnlock();
                        emergencyKeysHeld = true;
                    }
                }
                else
                {
                    emergencyKeyHoldTime = 0f;
                    emergencyKeysHeld = false;
                }
            }
        }
#endif
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (oneShot && used) return;
        if (!other.CompareTag(targetTag)) return;

        // Always bind to the root with the attached RB (not a child hitbox)
        playerGO = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        playerRB = playerGO.GetComponent<Rigidbody2D>();

        // Find controller by type name on player OR its parents
        cachedController = null;
        if (!string.IsNullOrEmpty(playerControllerComponentName))
        {
            var comps = playerGO.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var c in comps)
            {
                if (c != null && c.GetType().Name == playerControllerComponentName)
                {
                    cachedController = c;
                    break;
                }
            }
            
            // Debug warning if controller not found
            if (cachedController == null)
            {
                Debug.LogWarning($"[ChildEncounter] Could not find controller component '{playerControllerComponentName}' on {playerGO.name} or its parents.");
            }
        }

        ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Walking/jumping off the child without choosing must unlock
        if (promptVisible && other.CompareTag(targetTag))
        {

            ClosePromptOnly();
        }
    }

    // ---------- UI control ----------
    private void ShowPrompt()
    {
        // Safety check: don't show prompt if already visible or if player references are invalid
        if (promptVisible)
        {
            Debug.LogWarning("[ChildEncounter] ShowPrompt called but prompt is already visible!");
            return;
        }
        
        if (playerGO == null)
        {
            Debug.LogError("[ChildEncounter] ShowPrompt called but playerGO is null!");
            return;
        }

        promptVisible = true;
        lockStartTime = Time.unscaledTime;

        if (promptCanvas) promptCanvas.gameObject.SetActive(true);
        if (promptGroup)
        {
            promptGroup.alpha = 1f;
            promptGroup.interactable = true;
            promptGroup.blocksRaycasts = true;
        }

        LockPlayer(true);

        // Focus first button for KB/gamepad
        if (EventSystem.current && saveButton && saveButton.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(saveButton.gameObject);
            saveButton.OnSelect(null);
        }
    }

    private void ClosePromptOnly()
    {
        if (!promptVisible) 
        {
            Debug.LogWarning("[ChildEncounter] ClosePromptOnly called but prompt not visible!");
            return;
        }


        promptVisible = false;

        // Unlock BEFORE touching UI to avoid race conditions with destruction
        LockPlayer(false);

        if (promptGroup)
        {
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;
            promptGroup.alpha = 0f;
        }
        if (promptCanvas) promptCanvas.gameObject.SetActive(false);

        if (oneShot) used = true;
        
        // Reset emergency unlock state
        emergencyKeyHoldTime = 0f;
        emergencyKeysHeld = false;
    }

    private void HidePromptImmediate()
    {
        promptVisible = false;
        if (promptGroup)
        {
            promptGroup.alpha = 0f;
            promptGroup.blocksRaycasts = false;
            promptGroup.interactable = false;
        }
        if (promptCanvas) promptCanvas.gameObject.SetActive(false);
    }

    private IEnumerator DestroyNextFrame()
    {
        yield return null;
        if (this) Destroy(gameObject);
    }

    // ---------- Lock / Unlock ----------
    private void LockPlayer(bool lockIt)
    {
        if (playerGO == null) 
        {
            Debug.LogWarning("[ChildEncounter] LockPlayer called but playerGO is null!");
            return;
        }

        if (cachedController != null)
        {
            if (lockIt)
            {
                prevControllerEnabled = cachedController.enabled;
                cachedController.enabled = false;

            }
            else
            {
                cachedController.enabled = prevControllerEnabled;

            }
        }
        else if (lockIt)
        {
            Debug.LogWarning("[ChildEncounter] No cached controller found - player may not be properly locked!");
        }

        if (playerRB != null && freezeRigidbodyWhilePrompt)
        {
            if (lockIt)
            {
                if (!hasPrevConstraints)
                {
                    prevConstraints = playerRB.constraints;
                    hasPrevConstraints = true;

                }
#if UNITY_6000_OR_NEWER
                playerRB.velocity = Vector2.zero;
#else
                playerRB.linearVelocity = Vector2.zero;
#endif
                playerRB.constraints = RigidbodyConstraints2D.FreezeAll;

            }
            else
            {
                // Restore exactly what we had; if we never stored, fallback to None
                var restoreConstraints = hasPrevConstraints ? prevConstraints : RigidbodyConstraints2D.None;
                playerRB.constraints = restoreConstraints;

                hasPrevConstraints = false; // Reset flag after restoration
            }
        }
    }

    private void ForceUnlockPlayer()
    {
        // Prevent repeated calls
        if (isUnlocking) return;
        isUnlocking = true;
        
        // Absolute safety: never leave the player immobile
        if (cachedController != null) 
        {
            cachedController.enabled = true;
        }

        if (playerRB != null)
        {
            // Always restore to FreezeRotation if we don't have previous constraints stored
            // This prevents the character from falling over
            var restoreConstraints = hasPrevConstraints ? prevConstraints : RigidbodyConstraints2D.FreezeRotation;
            playerRB.constraints = restoreConstraints;
            hasPrevConstraints = false; // Reset flag
        }
        
        // Force cleanup UI state to prevent interaction issues
        ForceCleanupUI();
        
        // Reset state flags
        promptVisible = false;
        
        // Reset unlocking flag after a short delay to allow for cleanup
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ResetUnlockingFlag());
        }
        else
        {
            // If GameObject is inactive, reset immediately
            isUnlocking = false;
        }
    }
    
    private IEnumerator ResetUnlockingFlag()
    {
        yield return new WaitForSeconds(0.1f); // Short delay to prevent immediate re-triggering
        isUnlocking = false;
    }
    
    private void ForceCleanupUI()
    {
        // Clear EventSystem selection to prevent stuck UI focus
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // Force hide prompt UI
        if (promptGroup)
        {
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;
            promptGroup.alpha = 0f;
        }
        
        if (promptCanvas)
        {
            promptCanvas.gameObject.SetActive(false);
        }
        
        // Reset emergency unlock state
        emergencyKeyHoldTime = 0f;
        emergencyKeysHeld = false;
    }
    
    private void GlobalEmergencyUnlock()
    {
        if (isUnlocking) return; // Prevent repeated calls
        isUnlocking = true;
        
        // Find and unlock all players
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            
            var controllers = player.GetComponentsInChildren<MonoBehaviour>();
            foreach (var controller in controllers)
            {
                if (controller != null && controller.GetType().Name == "PlayerController")
                {
                    controller.enabled = true;
                }
            }
        }
        
        // Clear EventSystem selection globally
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // Force cleanup this instance
        ForceCleanupUI();
        promptVisible = false;
    }

    // ---------- Choices ----------
    public void OnChooseSave()
    {
        GameSession.Instance?.RecordChildSaved();

        ClosePromptOnly();
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DestroyNextFrame());
        }
        else
        {
            // If GameObject is inactive, destroy immediately
            if (this) Destroy(gameObject);
        }
    }

    public void OnChooseDrink()
    {
        if (GameSession.Instance)
        {
            if (healHearts > 0) GameSession.Instance.Heal(healHearts);
            if (Mathf.Abs(sanityPenalty) > 0.01f) GameSession.Instance.AddSanityDelta(sanityPenalty);
            GameSession.Instance.RecordChildKilled();
        }

        ClosePromptOnly();
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DestroyNextFrame());
        }
        else
        {
            // If GameObject is inactive, destroy immediately
            if (this) Destroy(gameObject);
        }
    }
}
