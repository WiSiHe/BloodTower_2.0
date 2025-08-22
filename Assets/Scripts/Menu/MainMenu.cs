using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Called when clicking Start Game
    public void StartGame()
    {
        // Load your first gameplay scene (set in Build Settings)
        SceneManager.LoadScene("GameScene"); 
    }

    // Called when clicking Credits
    public void ShowCredits()
    {
        SceneManager.LoadScene("CreditsScene");
    }

    // Called when clicking Quit
    public void QuitGame()
    {
        Debug.Log("Quit Game!"); // works in Editor
        Application.Quit();      // works in build
    }
}