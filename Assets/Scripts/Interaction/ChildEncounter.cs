using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class ChildEncounter : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool oneShot = true;

    [Header("Effects")]
    [SerializeField] private int   healHearts = 1;     // when drinking
    [SerializeField] private float sanityPenalty = -20f; // negative to reduce sanity when drinking

    [Header("UI (World-space panel as child of this prefab)")]
    [SerializeField] private Canvas   promptCanvas;   // World Space canvas (child)
    [SerializeField] private CanvasGroup promptGroup; // on the canvas (to toggle visibility)
    [SerializeField] private Button   saveButton;
    [SerializeField] private Button   drinkButton;
    [SerializeField] private Button   cancelButton;   // optional
    [SerializeField] private TMP_Text promptText;     // optional flavor text

    [Header("Optional: player control")]
    [Tooltip("Disable player controls while the prompt is open (looks up a MonoBehaviour on the Player).")]
    [SerializeField] private string playerControllerComponentName = "PlayerController";

    private bool used;
    private GameObject playerGO;
    private MonoBehaviour cachedController;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        HidePromptImmediate();

        // Wire buttons
        if (saveButton)  saveButton.onClick.AddListener(OnChooseSave);
        if (drinkButton) drinkButton.onClick.AddListener(OnChooseDrink);
        if (cancelButton) cancelButton.onClick.AddListener(HidePrompt);
    }

    private void OnDestroy()
    {
        if (saveButton)  saveButton.onClick.RemoveListener(OnChooseSave);
        if (drinkButton) drinkButton.onClick.RemoveListener(OnChooseDrink);
        if (cancelButton) cancelButton.onClick.RemoveListener(HidePrompt);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (oneShot && used) return;
        if (!other.CompareTag(targetTag)) return;

        playerGO = other.gameObject;
        ShowPrompt();
    }

    // ---------- UI control ----------
    private void ShowPrompt()
    {
        if (promptGroup != null)
        {
            promptGroup.alpha = 1f;
            promptGroup.blocksRaycasts = true;
            promptGroup.interactable = true;
        }
        if (promptCanvas != null) promptCanvas.gameObject.SetActive(true);

        LockPlayer(true);

        // Focus a button for keyboard/gamepad
        if (EventSystem.current != null && saveButton != null)
        {
            EventSystem.current.SetSelectedGameObject(saveButton.gameObject);
            saveButton.OnSelect(null);
        }
    }

    private void HidePrompt()
    {
        if (promptGroup != null)
        {
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;
            promptGroup.alpha = 0f;
        }
        if (promptCanvas != null) promptCanvas.gameObject.SetActive(false);

        LockPlayer(false);

        if (oneShot) used = true;
    }

    private void HidePromptImmediate()
    {
        if (promptGroup != null)
        {
            promptGroup.alpha = 0f;
            promptGroup.blocksRaycasts = false;
            promptGroup.interactable = false;
        }
        if (promptCanvas != null) promptCanvas.gameObject.SetActive(false);
    }

    private void LockPlayer(bool lockIt)
    {
        if (playerGO == null || string.IsNullOrEmpty(playerControllerComponentName)) return;

        if (cachedController == null)
        {
            // Try to find a controller MonoBehaviour by name on player
            var comps = playerGO.GetComponents<MonoBehaviour>();
            foreach (var c in comps)
            {
                if (c != null && c.GetType().Name == playerControllerComponentName)
                {
                    cachedController = c;
                    break;
                }
            }
        }

        if (cachedController != null) cachedController.enabled = !lockIt;

        var rb = playerGO.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
#if UNITY_6000_OR_NEWER
            rb.linearVelocity = lockIt ? Vector2.zero : rb.linearVelocity;
#else
            if (lockIt) rb.linearVelocity = Vector2.zero;
#endif
            rb.constraints = lockIt ? RigidbodyConstraints2D.FreezeAll : RigidbodyConstraints2D.None;
        }
    }

    // ---------- Button actions ----------
    public void OnChooseSave()
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.RecordChildSaved();
            // optional: tiny sanity bonus for mercy? uncomment if desired
            // GameSession.Instance.AddSanityDelta(+5f);
        }

        HidePrompt();
        Destroy(gameObject); // remove child from world
    }

    public void OnChooseDrink()
    {
        if (GameSession.Instance != null)
        {
            if (healHearts > 0) GameSession.Instance.Heal(healHearts);
            if (Mathf.Abs(sanityPenalty) > 0.01f) GameSession.Instance.AddSanityDelta(sanityPenalty); // negative reduces sanity
            GameSession.Instance.RecordChildKilled();
        }

        HidePrompt();
        Destroy(gameObject); // remove child from world
    }
}