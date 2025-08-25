using UnityEngine;
using System.Collections;

/// <summary>
/// Visual/audio feedback when the player takes damage:
/// - brief hit-stop (slow motion)
/// - sprite flash to a hurt color
/// - optional full-screen flash via CanvasGroup
/// - camera shake via CameraShaker
/// Attach to the Player. Call OnDamaged(amount) or DamageFeedback.PlayerHit().
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DamageFeedback : MonoBehaviour
{
    public static DamageFeedback Instance { get; private set; }

    [Header("Sprite Flash")]
    [SerializeField] private Color hurtColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private int   flashCycles   = 2;

    [Header("Screen Flash (optional)")]
    [SerializeField] private CanvasGroup screenFlash; // full-screen UI image with white color
    [SerializeField] private float screenFlashAlpha = 0.35f;
    [SerializeField] private float screenFlashFade  = 0.2f;

    [Header("Hit Stop (slow motion)")]
    [SerializeField] private float hitStopTimeScale = 0.05f;
    [SerializeField] private float hitStopDuration  = 0.06f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeAmplitude = 2.2f;
    [SerializeField] private float shakeFrequency = 14f;
    [SerializeField] private float shakeDuration  = 0.20f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip   hurtSfx;

    private SpriteRenderer sr;
    private Color originalColor;
    private bool playing;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
    }

    /// <summary>Static helper: call this from any damage code.</summary>
    public static void PlayerHit() => Instance?.OnDamaged(1);

    public void OnDamaged(int amount)
    {
        if (!playing) StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        playing = true;

        // Camera shake
        if (CameraShaker.Instance) CameraShaker.Instance.Shake(shakeAmplitude, shakeFrequency, shakeDuration);

        // Audio
        if (hurtSfx && sfxSource) sfxSource.PlayOneShot(hurtSfx);

        // Screen flash
        if (screenFlash) StartCoroutine(ScreenFlashCo());

        // Sprite flash + hit stop in parallel
        var flashCo = StartCoroutine(FlashSpriteCo());
        var stopCo  = StartCoroutine(HitStopCo());

        yield return flashCo;
        yield return stopCo;

        playing = false;
    }

    private IEnumerator FlashSpriteCo()
    {
        float cycle = flashDuration / Mathf.Max(1, flashCycles);
        for (int i = 0; i < flashCycles; i++)
        {
            sr.color = hurtColor;
            yield return new WaitForSecondsRealtime(cycle * 0.5f);
            sr.color = originalColor;
            yield return new WaitForSecondsRealtime(cycle * 0.5f);
        }
        sr.color = originalColor;
    }

    private IEnumerator ScreenFlashCo()
    {
        screenFlash.alpha = screenFlashAlpha;
        // make sure it blocks raycasts if it's your overlay
        screenFlash.blocksRaycasts = false;
        float t = 0f;
        while (t < screenFlashFade)
        {
            t += Time.unscaledDeltaTime;
            screenFlash.alpha = Mathf.Lerp(screenFlashAlpha, 0f, t / screenFlashFade);
            yield return null;
        }
        screenFlash.alpha = 0f;
    }

    private IEnumerator HitStopCo()
    {
        float prevScale = Time.timeScale;
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = prevScale;
    }
}