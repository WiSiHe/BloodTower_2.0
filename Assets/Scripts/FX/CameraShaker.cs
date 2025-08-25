// Assets/Scripts/FX/CameraShaker.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// Camera shake that does NOT depend on Cinemachine.
/// It creates a hidden pivot Transform above the main camera and jitters that pivot,
/// so your camera's own local position/rotation stay intact.
/// Usage: CameraShaker.Instance.Shake(amplitude, frequency, duration);
/// </summary>
[DefaultExecutionOrder(-200)]
public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }

    [Header("Defaults")]
    [SerializeField] private float defaultAmplitude = 2.0f;  // overall strength
    [SerializeField] private float defaultFrequency = 12f;   // how fast it jiggles
    [SerializeField] private float defaultDuration  = 0.20f;

    [Header("Max Limits (safety)")]
    [SerializeField] private float maxOffset = 0.5f;   // max positional offset in units
    [SerializeField] private float maxAngle  = 4f;     // max roll in degrees

    private Transform pivot;            // the thing we jitter
    private Transform cam;              // the main camera transform
    private Vector3 pivotOrigLocalPos;  // cached
    private Quaternion pivotOrigLocalRot;
    private Coroutine running;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePivot();
    }

    private void OnEnable()  => EnsurePivot();
    private void OnDestroy() => Instance = Instance == this ? null : Instance;

    private void EnsurePivot()
    {
        var mainCam = Camera.main ? Camera.main.transform : null;
        if (mainCam == null) return;

        // If we already wrapped this camera, reuse it
        if (pivot != null && cam == mainCam) return;

        cam = mainCam;

        // If the camera is already inside a "CameraShakePivot", reuse it.
        var existing = cam.parent != null && cam.parent.name == "CameraShakePivot"
            ? cam.parent
            : null;

        if (existing != null)
        {
            pivot = existing;
        }
        else
        {
            // Create a pivot and reparent the camera under it
            pivot = new GameObject("CameraShakePivot").transform;
            pivot.SetParent(cam.parent, worldPositionStays: true);
            pivot.position = cam.position;
            pivot.rotation = cam.rotation;
            cam.SetParent(pivot, worldPositionStays: true);
        }

        pivotOrigLocalPos = pivot.localPosition;
        pivotOrigLocalRot = pivot.localRotation;
    }

    public void Shake() => Shake(defaultAmplitude, defaultFrequency, defaultDuration);

    public void Shake(float amplitude, float frequency, float duration)
    {
        EnsurePivot();
        if (pivot == null) return;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(DoShake(amplitude, frequency, duration));
    }

    private IEnumerator DoShake(float amp, float freq, float dur)
    {
        // Bounds
        amp = Mathf.Max(0f, amp);
        freq = Mathf.Max(0.01f, freq);
        dur = Mathf.Max(0f, dur);

        float t = 0f;
        // random phase offsets so two shakes don’t look identical
        float seedX = Random.value * 10f;
        float seedY = Random.value * 10f;
        float seedR = Random.value * 10f;

        while (t < dur && pivot != null)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / dur); // simple fade out

            // Perlin-based jitter
            float ox = (Mathf.PerlinNoise(seedX, Time.unscaledTime * freq) - 0.5f) * amp * 0.05f * maxOffset;
            float oy = (Mathf.PerlinNoise(seedY, Time.unscaledTime * freq) - 0.5f) * amp * 0.05f * maxOffset;
            float ang = (Mathf.PerlinNoise(seedR, Time.unscaledTime * freq) - 0.5f) * amp * maxAngle;

            pivot.localPosition = pivotOrigLocalPos + new Vector3(ox, oy, 0f) * k;
            pivot.localRotation = Quaternion.Euler(0f, 0f, ang * k);

            yield return null;
        }

        if (pivot != null)
        {
            pivot.localPosition = pivotOrigLocalPos;
            pivot.localRotation = pivotOrigLocalRot;
        }
        running = null;
    }
}