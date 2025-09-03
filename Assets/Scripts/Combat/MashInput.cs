using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// Counts button taps from an Input System action (e.g., Jump) and lets other
/// scripts consume the per-frame delta via ConsumeTapCount().
///
/// How to use:
/// - Add this to a GameObject (e.g., "BossMash").
/// - Drag an InputActionReference (your Jump/Attack action) into Mash Action.
/// - (Optional) Leave "Enable And Disable Action" on if this object should
///   manage the action's lifetime.
/// - Read taps with ConsumeTapCount() each Update() in your duel/VFX code.
public class MashInput : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Reference to the Input System action that the player will mash (e.g., Jump).")]
    [SerializeField] private InputActionReference mashAction;

    [Header("Behaviour")]
    [Tooltip("If true, this component will Enable/Disable the action on OnEnable/OnDisable.")]
    [SerializeField] private bool enableAndDisableAction = true;

    [Tooltip("Reset the internal counters whenever this component is enabled.")]
    [SerializeField] private bool resetCountOnEnable = true;

    [Header("Optional: tap-rate (for UI/VFX)")]
    [Tooltip("Seconds of history to compute RecentRate (taps per second). 0 disables rate calc.")]
    [SerializeField] private float rateWindowSeconds = 0.4f;

    // --- Public read-only info (optional) ---
    /// Total taps since last reset/enable.
    public int TotalTaps { get; private set; }
    /// Recent taps per second over the sliding window. 0 if disabled.
    public float RecentRate { get; private set; }

    // --- Internals ---
    InputAction _action;
    int _frameTapCount;                     // taps since last ConsumeTapCount()
    readonly Queue<float> _tapTimes = new Queue<float>(); // for rate calc (Time.unscaledTime)

    void OnValidate()
    {
        if (rateWindowSeconds < 0f) rateWindowSeconds = 0f;
    }

    void OnEnable()
    {
        _action = mashAction ? mashAction.action : null;
        if (_action == null)
        {
            Debug.LogWarning("[MashInput] No action assigned. Drag an InputActionReference (Jump/Attack).", this);
            return;
        }

        _action.performed += OnMashPerformed;
        if (enableAndDisableAction && !_action.enabled) _action.Enable();

        if (resetCountOnEnable) ResetCounters();
    }

    void OnDisable()
    {
        if (_action != null)
        {
            _action.performed -= OnMashPerformed;
            if (enableAndDisableAction && _action.enabled) _action.Disable();
        }
    }

    void Update()
    {
        // Maintain sliding window for RecentRate (optional).
        if (rateWindowSeconds > 0f && _tapTimes.Count > 0)
        {
            float cutoff = Time.unscaledTime - rateWindowSeconds;
            while (_tapTimes.Count > 0 && _tapTimes.Peek() < cutoff)
                _tapTimes.Dequeue();

            RecentRate = _tapTimes.Count / Mathf.Max(0.0001f, rateWindowSeconds);
        }
        else
        {
            RecentRate = 0f;
        }
    }

    void OnMashPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        _frameTapCount++;
        TotalTaps++;

        if (rateWindowSeconds > 0f)
            _tapTimes.Enqueue(Time.unscaledTime);
    }

    /// Returns the number of taps since the last call and resets that counter.
    public int ConsumeTapCount()
    {
        int d = _frameTapCount;
        _frameTapCount = 0;
        return d;
    }

    /// Manually zero out all state (useful when starting a new duel).
    public void ResetCounters()
    {
        _frameTapCount = 0;
        TotalTaps = 0;
        RecentRate = 0f;
        _tapTimes.Clear();
    }

    // Optional helper for tests / debugging (call from context menu).
#if UNITY_EDITOR
    [ContextMenu("Simulate Tap")]
    void SimulateTapForDebug()
    {
        _frameTapCount++;
        TotalTaps++;
        if (rateWindowSeconds > 0f) _tapTimes.Enqueue(Time.unscaledTime);
    }
#endif
}