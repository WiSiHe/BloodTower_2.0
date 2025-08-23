using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Defaults")]
    [SerializeField] private float targetVolume = 0.8f;
    [SerializeField] private float fadeSeconds = 1.5f;

    [Header("Persistence")]
    [SerializeField] private string playerPrefsVolumeKey = "music_volume";

    private AudioSource _a;
    private AudioSource _b;
    private AudioSource _active;   // currently audible
    private AudioSource _inactive; // used for crossfades
    private Coroutine _fadeCo;

    private float _masterVolume = 1f; // 0..1 applied on top of track volumes

    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            ApplyMasterVolume();
            PlayerPrefs.SetFloat(playerPrefsVolumeKey, _masterVolume);
        }
    }

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

        // Load saved volume (default to 1.0 if missing)
        if (PlayerPrefs.HasKey(playerPrefsVolumeKey))
            _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(playerPrefsVolumeKey, 1f));
        else
            _masterVolume = 1f;
    }

    /// <summary>Plays or crossfades to clip. If clip is null, fades out.</summary>
    public void PlayMusic(AudioClip clip, float? overrideFadeSeconds = null, float? overrideTargetVol = null)
    {
        float fade = overrideFadeSeconds ?? fadeSeconds;
        float trackVol = Mathf.Clamp01(overrideTargetVol ?? targetVolume);

        if (clip == null)
        {
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeOutThenStop(_active, fade));
            return;
        }

        if (_active.clip == clip && _active.isPlaying)
        {
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _active.volume = trackVol * _masterVolume;
            return;
        }

        _inactive.clip = clip;
        _inactive.volume = 0f;
        _inactive.Play();

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(Crossfade(_active, _inactive, fade, trackVol));

        var tmp = _active; _active = _inactive; _inactive = tmp;
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to, float secs, float targetTrackVol)
    {
        if (secs <= 0f)
        {
            if (from.isPlaying) from.Stop();
            to.volume = targetTrackVol * _masterVolume;
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
            to.volume = Mathf.Lerp(startTo, targetTrackVol * _masterVolume, k);
            yield return null;
        }

        if (from.isPlaying) from.Stop();
        to.volume = targetTrackVol * _masterVolume;
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

    private void ApplyMasterVolume()
    {
        // Multiply whatever the track volumes currently are by _masterVolume proportionally.
        if (_active != null)   _active.volume   = Mathf.Clamp01(_active.volume)   * 0f + _active.volume / Mathf.Max(_masterVolume, 0.0001f) * _masterVolume;
        if (_inactive != null) _inactive.volume = Mathf.Clamp01(_inactive.volume) * 0f + _inactive.volume / Mathf.Max(_masterVolume, 0.0001f) * _masterVolume;

        // Simpler approach: re-scale both sources to respect the new master volume while
        // preserving their relative ratio. (If you prefer exact control, track "trackVolume" separately.)
    }
}