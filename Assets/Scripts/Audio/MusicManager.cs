using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Defaults")]
    [SerializeField] private float targetVolume = 0.8f;
    [SerializeField] private float fadeSeconds = 1.5f;

    private AudioSource _a;
    private AudioSource _b;
    private AudioSource _active;   // currently audible
    private AudioSource _inactive; // will fade in next

    private Coroutine _fadeCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        foreach (var src in new[] { _a, _b })
        {
            src.playOnAwake = false;
            src.loop = true;
        }

        _active = _a;
        _inactive = _b;
    }

    /// <summary>Plays or crossfades to the given clip. If clip is null, fades out.</summary>
    public void PlayMusic(AudioClip clip, float? overrideFadeSeconds = null, float? overrideTargetVol = null)
    {
        float fade = overrideFadeSeconds ?? fadeSeconds;
        float vol  = Mathf.Clamp01(overrideTargetVol ?? targetVolume);

        if (clip == null)
        {
            // Fade out currently active
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeOutThenStop(_active, fade));
            return;
        }

        if (_active.clip == clip && _active.isPlaying)
        {
            // Already playing this clip; ensure volume is correct
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _active.volume = vol;
            return;
        }

        // Prepare inactive for the new clip
        _inactive.clip = clip;
        _inactive.volume = 0f;
        _inactive.Play();

        // Crossfade: inactive -> in, active -> out
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(Crossfade(_active, _inactive, fade, vol));

        // Swap roles
        var tmp = _active;
        _active = _inactive;
        _inactive = tmp;
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to, float secs, float targetVol)
    {
        if (secs <= 0f)
        {
            if (from.isPlaying) from.Stop();
            to.volume = targetVol;
            yield break;
        }

        float t = 0f;
        float startFrom = from.isPlaying ? from.volume : 0f;
        float startTo = to.volume;

        while (t < secs)
        {
            t += Time.unscaledDeltaTime; // unaffected by pause
            float k = t / secs;
            if (from.isPlaying) from.volume = Mathf.Lerp(startFrom, 0f, k);
            to.volume = Mathf.Lerp(startTo, targetVol, k);
            yield return null;
        }

        if (from.isPlaying) from.Stop();
        to.volume = targetVol;
    }

    private IEnumerator FadeOutThenStop(AudioSource src, float secs)
    {
        if (!src.isPlaying || secs <= 0f) { src.Stop(); yield break; }
        float t = 0f;
        float start = src.volume;
        while (t < secs)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / secs);
            yield return null;
        }
        src.Stop();
        src.volume = 0f;
    }
}