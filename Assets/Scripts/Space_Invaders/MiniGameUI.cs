using UnityEngine;
using TMPro;           // if you use TextMeshPro
using UnityEngine.UI; // if you use legacy Text/Images

public class MiniGameUI : MonoBehaviour
{
    [Header("Texts (either TMP_Text or legacy Text)")]
    [SerializeField] private TMP_Text livesTMP;
    [SerializeField] private TMP_Text killsTMP;
    [SerializeField] private TMP_Text poweredTMP;

    [SerializeField] private Text livesText;   // legacy fallback
    [SerializeField] private Text killsText;   // legacy fallback
    [SerializeField] private Text poweredText; // legacy fallback

    public void SetLives(int lives)
    {
        if (livesTMP) livesTMP.text = $"Lives: {lives}";
        if (livesText) livesText.text = $"Lives: {lives}";
    }

    public void SetKills(int kills, int target)
    {
        if (killsTMP) killsTMP.text = $"Kills: {kills}/{target}";
        if (killsText) killsText.text = $"Kills: {kills}/{target}";
    }

    public void SetPowered(bool on)
    {
        string msg = on ? "POWERED!" : "";
        if (poweredTMP) poweredTMP.text = msg;
        if (poweredText) poweredText.text = msg;
    }
}