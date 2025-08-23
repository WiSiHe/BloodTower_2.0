using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;              // Keyboard, Mouse, Gamepad, Touchscreen
using UnityEngine.InputSystem.Controls;     // ButtonControl
#endif

public class IntroManager : MonoBehaviour
{
    [Header("Intro Settings")]
    [SerializeField] private float delayBeforeContinue = 5f;     // Seconds before auto-continue
    [SerializeField] private string nextSceneName = "Tutorial";  // Next scene to load

    [Header("UI References")]
    [SerializeField] private TMP_Text introText;                 // Drag your TMP text here (optional)

    private bool _skipped;
    private Coroutine _flow;

    private void Start()
    {
        if (introText) introText.gameObject.SetActive(true);
        _flow = StartCoroutine(ShowIntroAndContinue());
    }

    private void Update()
    {
        if (_skipped) return;

        if (WasAnyInputPressedThisFrame())
        {
            SkipNow();
        }
    }

    private IEnumerator ShowIntroAndContinue()
    {
        yield return new WaitForSeconds(delayBeforeContinue);
        if (!_skipped) LoadNextScene();
    }

    /// <summary>Public method you can wire to a UI Button’s OnClick if you also want a visible “Continue”.</summary>
    public void SkipNow()
    {
        if (_skipped) return;
        _skipped = true;
        if (_flow != null) StopCoroutine(_flow);
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        Debug.Log("[IntroManager] Loading next scene: " + nextSceneName);
        Time.timeScale = 1f; // safety, in case paused
        SceneManager.LoadScene(nextSceneName);
    }

    // ------- Input: supports the new Input System, falls back to legacy if enabled -------
    private bool WasAnyInputPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        // Keyboard (any key)
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        // Mouse (any button)
        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.rightButton.wasPressedThisFrame ||
             Mouse.current.middleButton.wasPressedThisFrame ||
             (Mouse.current.forwardButton != null && Mouse.current.forwardButton.wasPressedThisFrame) ||
             (Mouse.current.backButton != null && Mouse.current.backButton.wasPressedThisFrame)))
            return true;

        // Touch (primary touch)
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        // Gamepad (any button)
        if (Gamepad.current != null)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is ButtonControl b && b.wasPressedThisFrame)
                    return true;
            }
        }

        return false;
#else
        // Legacy Input (if Active Input Handling = Both or Old)
        if (Input.anyKeyDown) return true;
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            return true;
        return false;
#endif
    }
}