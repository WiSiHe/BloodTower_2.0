using UnityEngine;
using System.Collections;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }

    [Header("Defaults")]
    [SerializeField] private float defaultAmplitude = 1.2f;
    [SerializeField] private float defaultFrequency = 6f;
    [SerializeField] private float defaultDuration  = 0.20f;

    // Fallback transform-based shake (no Cinemachine required)
    private Transform camTransform;     // main camera or vcam transform
    private Vector3   originalPos;
    private Coroutine co;

#if USE_CINEMACHINE   // <- set this Scripting Define Symbol once Cinemachine ref is fixed
    // We avoid compile-time references to Cinemachine by reflecting them.
    private Component vcam;             // CinemachineVirtualCamera
    private Component noise;            // CinemachineBasicMultiChannelPerlin
    private System.Type vcamType;
    private System.Type noiseType;
    private System.Reflection.PropertyInfo ampProp;
    private System.Reflection.PropertyInfo freqProp;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        TryBindCamera();
    }

    private void OnEnable() => TryBindCamera();

    private void TryBindCamera()
    {
        // Always cache the main camera transform for fallback
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
            originalPos  = camTransform.localPosition;
        }

#if USE_CINEMACHINE
        // Try to find Cinemachine vcam and its Perlin noise component via reflection
        vcamType  = System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Unity.Cinemachine");
        noiseType = System.Type.GetType("Cinemachine.CinemachineBasicMultiChannelPerlin, Unity.Cinemachine");

        if (vcamType != null)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            var allBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var allBehaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
            foreach (var mb in allBehaviours)
            {
                if (mb == null) continue;
                var t = mb.GetType();
                if (t == vcamType)
                {
                    vcam = mb;
                    camTransform = mb.transform; // use vcam transform for fallback pos too
                    originalPos  = camTransform.localPosition;

                    noise = mb.GetComponent(noiseType);
                    if (noise == null) noise = mb.gameObject.AddComponent(noiseType);

                    ampProp  = noiseType.GetProperty("AmplitudeGain");
                    freqProp = noiseType.GetProperty("FrequencyGain");
                    break;
                }
            }
        }
#endif
    }

    /// <summary>Shake the camera. If any parameter &lt;= 0, default is used.</summary>
    public void Shake(float amplitude = -1f, float frequency = -1f, float duration = -1f)
    {
        if (amplitude <= 0f) amplitude = defaultAmplitude;
        if (frequency <= 0f) frequency = defaultFrequency;
        if (duration  <= 0f) duration  = defaultDuration;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(DoShake(amplitude, frequency, duration));
    }

    private IEnumerator DoShake(float amp, float freq, float dur)
    {
        TryBindCamera();

#if USE_CINEMACHINE
        if (noise != null && ampProp != null && freqProp != null)
        {
            // Perlin noise shake (real Cinemachine)
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / dur);
                ampProp.SetValue(noise, amp * k, null);
                freqProp.SetValue(noise, freq, null);
                yield return null;
            }
            ampProp.SetValue(noise, 0f, null);
            yield break;
        }
#endif
        // Fallback: screenshake by jittering localPosition (works everywhere)
        if (camTransform == null)
        {
            if (Camera.main != null) camTransform = Camera.main.transform;
            if (camTransform == null) yield break;
            originalPos = camTransform.localPosition;
        }

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(elapsed / dur);
            float x = (Random.value * 2f - 1f) * amp * k * 0.05f; // 0.05 scales world units to a subtle shake
            float y = (Random.value * 2f - 1f) * amp * k * 0.05f;
            camTransform.localPosition = originalPos + new Vector3(x, y, 0f);
            yield return null;
        }
        camTransform.localPosition = originalPos;
    }
}