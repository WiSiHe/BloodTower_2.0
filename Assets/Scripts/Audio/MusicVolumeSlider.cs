using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicVolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text valueLabel; // optional: shows 0–100%

    [Header("Slider Ranges")]
    [SerializeField] private float min = 0f;
    [SerializeField] private float max = 1f;

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null) { Debug.LogError("[MusicVolumeSlider] No Slider found."); return; }

        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;

        float initial = 1f;
        if (MusicManager.Instance != null) initial = MusicManager.Instance.MasterVolume;

        slider.SetValueWithoutNotify(initial);
        UpdateLabel(initial);

        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float v)
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.MasterVolume = v;

        UpdateLabel(v);
    }

    private void UpdateLabel(float v)
    {
        if (valueLabel != null)
            valueLabel.text = Mathf.RoundToInt(v * 100f) + "%";
    }
}