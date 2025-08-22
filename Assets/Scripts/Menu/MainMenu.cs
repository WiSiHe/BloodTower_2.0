using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject creditsPanel; // assign in Inspector

    // Called when clicking Start Game
    public void StartGame()
    {
        Debug.Log("[MainMenu] StartGame clicked");
        Time.timeScale = 1f;                   // unpause just in case
        SceneManager.LoadScene("Tutorial");    // make sure it's in Build Settings
    }

    // Show credits panel
    public void ShowCredits()
    {
        Debug.Log("[MainMenu] ShowCredits clicked");
        creditsPanel.SetActive(true);
    }

    // Hide credits panel
    public void HideCredits()
    {
        Debug.Log("[MainMenu] HideCredits clicked");
        creditsPanel.SetActive(false);
    }

    // Quit the game
    public void QuitGame()
    {
        Debug.Log("[MainMenu] QuitGame clicked");
        Application.Quit(); // only works in build
    }
}