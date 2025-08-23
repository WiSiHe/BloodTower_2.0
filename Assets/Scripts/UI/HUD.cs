using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
    [Header("Health (Hearts)")]
    [SerializeField] private Image[] heartImages; // size = 3
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartEmpty;

    [Header("Sanity")]
    [SerializeField] private Slider sanitySlider; // min=0, max=100, non-interactable

    [Header("Time")]
    [SerializeField] private TMP_Text timeText;   // MM:SS

    private void Start()
    {
        if (sanitySlider != null)
        {
            sanitySlider.minValue = 0f;
            sanitySlider.maxValue = 100f;
            sanitySlider.interactable = false;
        }
        RefreshAll();
    }

    private void Update()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        var gs = GameSession.Instance;
        if (gs == null) return;

        // Hearts
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (!heartImages[i]) continue;
            heartImages[i].sprite = (i < gs.Hearts) ? heartFull : heartEmpty;
            heartImages[i].enabled = (i < gs.MaxHearts);
        }

        // Sanity
        if (sanitySlider != null) sanitySlider.value = gs.Sanity;

        // Time
        if (timeText != null)
        {
            float t = gs.RunTime;
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}