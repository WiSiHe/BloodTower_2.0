using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI References (optional)")]
    [SerializeField] private GameObject creditsPanel; // assign if you use a credits panel

    [Header("Scene Names")]
    [SerializeField] private string introSceneName = "Intro";   // MainMenu -> Intro

    // === Buttons ===
    public void StartGame()
    {
        Debug.Log("[MainMenu] StartGame clicked -> loading Intro");
        Time.timeScale = 1f;
        SceneManager.LoadScene(introSceneName); // <-- make sure this is "Intro"
    }

    public void ShowCredits()
    {
        if (!creditsPanel)
        {
            Debug.LogWarning("[MainMenu] ShowCredits clicked but no creditsPanel assigned.");
            return;
        }
        var cg = creditsPanel.GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
        creditsPanel.SetActive(true);
        Debug.Log("[MainMenu] CreditsPanel shown");
    }

    public void HideCredits()
    {
        if (!creditsPanel) return;
        var cg = creditsPanel.GetComponent<CanvasGroup>();
        if (cg) { cg.interactable = false; cg.blocksRaycasts = false; cg.alpha = 0f; }
        creditsPanel.SetActive(false);
        Debug.Log("[MainMenu] CreditsPanel hidden");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[MainMenu] QuitGame (Editor) — stopping Play Mode.");
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Debug.Log("[MainMenu] QuitGame (WebGL) — not supported.");
#else
        Debug.Log("[MainMenu] QuitGame — exiting application.");
        Application.Quit();
#endif
    }
}