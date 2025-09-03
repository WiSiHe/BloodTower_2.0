using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MashUI : MonoBehaviour
{
    [Header("Refs")]
    public Slider progressSlider;      // Set Min=0, Max=1
    public TMP_Text statusText;        // "Mash any button!"
    public TMP_Text tuningText;        // shows saved/eaten & tuned numbers (optional)

    [Header("Pulse")]
    public RectTransform shakeTarget;  // e.g., the slider handle or whole panel
    public float shakeDecay = 4f;

    float shakeAmp;

    void Awake()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0.5f;
        }
        if (statusText) statusText.text = "MASH ANY BUTTON!";
    }

    void Update()
    {
        if (shakeTarget && shakeAmp > 0f)
        {
            float a = shakeAmp;
            Vector2 jitter = new Vector2(
                (Mathf.PerlinNoise(Time.time * 13.7f, 1.23f) - 0.5f),
                (Mathf.PerlinNoise(2.34f, Time.time * 17.9f) - 0.5f)
            ) * a * 10f;
            shakeTarget.anchoredPosition = jitter;
            shakeAmp = Mathf.Max(0f, shakeAmp - shakeDecay * Time.deltaTime);
        }
        else if (shakeTarget)
        {
            shakeTarget.anchoredPosition = Vector2.zero;
        }
    }

    public void SetDesign(int saved, int eaten, float tapPower, float bossPull)
    {
        if (tuningText)
            tuningText.text = $"Saved: {saved}  |  Consumed: {eaten}\nTapPower: {tapPower:0.000}  BossPull/s: {bossPull:0.000}";
    }

    public void SetProgress(float progressMinus1To1)
    {
        if (!progressSlider) return;
        progressSlider.value = Mathf.InverseLerp(-1f, 1f, progressMinus1To1);
    }

    public void SetResult(string msg)
    {
        if (statusText) statusText.text = msg;
    }

    public void PulseShake(float amp) => shakeAmp = Mathf.Clamp01(shakeAmp + amp);
}