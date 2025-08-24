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
    [SerializeField] private float maxLockSeconds = 20.0f;
    [Tooltip("Allow keyboard/controller cancel while prompt is up (Esc/B).")]
    [SerializeField] private bool allowQuickCancel = true;

    // Runtime state
    private bool promptVisible;
    private bool used;
    private float lockStartTime;
    private bool hasPrevConstraints;

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
        // Watchdog: if somehow we remain locked too long, unlock automatically.
        if (promptVisible && (Time.unscaledTime - lockStartTime) > maxLockSeconds)
        {
            Debug.LogWarning("[ChildEncounter] Watchdog unlock triggered.");
            ClosePromptOnly();
        }

#if ENABLE_INPUT_SYSTEM
        if (allowQuickCancel && promptVisible)
        {
            // Escape or B button = cancel/close
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var gp = UnityEngine.InputSystem.Gamepad.current;
            if ((kb != null && kb.escapeKey.wasPressedThisFrame) ||
                (gp != null && (gp.bButton.wasPressedThisFrame || gp.startButton.wasPressedThisFrame)))
            {
                ClosePromptOnly();
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
        }

        ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Walking/jumping off the child without choosing must unlock
        if (promptVisible && other.CompareTag(targetTag))
            ClosePromptOnly();
    }

    // ---------- UI control ----------
    private void ShowPrompt()
    {
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
        if (playerGO == null) return;

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
                playerRB.linearVelocity = Vector2.zero;
#else
                playerRB.linearVelocity = Vector2.zero;
#endif
                playerRB.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else
            {
                // Restore exactly what we had; if we never stored, fallback to None
                playerRB.constraints = hasPrevConstraints ? prevConstraints : RigidbodyConstraints2D.None;
            }
        }
    }

    private void ForceUnlockPlayer()
    {
        // Absolute safety: never leave the player immobile
        if (cachedController != null) cachedController.enabled = true;

        if (playerRB != null)
        {
            playerRB.constraints = hasPrevConstraints ? prevConstraints : RigidbodyConstraints2D.None;
        }
    }

    // ---------- Choices ----------
    public void OnChooseSave()
    {
        GameSession.Instance?.RecordChildSaved();

        ClosePromptOnly();
        StartCoroutine(DestroyNextFrame());
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
        StartCoroutine(DestroyNextFrame());
    }
}