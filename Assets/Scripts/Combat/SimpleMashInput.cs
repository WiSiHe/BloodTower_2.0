using UnityEngine;
using UnityEngine.InputSystem;

/// Attach to any GameObject in the duel scene.
/// Assign "Mash Action" (e.g., Player/Jump). Exposes TapRate for others to read.
public class SimpleMashInput : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference mashAction;

    [Header("Behaviour")]
    [Tooltip("Time window used to compute taps per second.")]
    [SerializeField] private float rateWindowSeconds = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    // public accessors expected by BossMashDuel
    public float TapRate { get; private set; } = 0f;
    public bool DebugLogs => debugLogs;

    float windowTimer = 0f;
    int   tapCount    = 0;

    void OnEnable()
    {
        if (mashAction && mashAction.action != null)
        {
            mashAction.action.performed += OnMash;
            mashAction.action.Enable();
        }
        windowTimer = 0f;
        tapCount    = 0;
        TapRate     = 0f;
    }

    void OnDisable()
    {
        if (mashAction && mashAction.action != null)
        {
            mashAction.action.performed -= OnMash;
            mashAction.action.Disable();
        }
    }

    void Update()
    {
        windowTimer += Time.deltaTime;
        if (windowTimer >= Mathf.Max(0.05f, rateWindowSeconds))
        {
            TapRate     = tapCount / windowTimer;
            windowTimer = 0f;
            tapCount    = 0;

            if (debugLogs)
                Debug.Log($"[SIMPLE MASH] rate={TapRate:0.00} tps");
        }
    }

    void OnMash(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        tapCount++;
    }
}