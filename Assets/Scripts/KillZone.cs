using UnityEngine;

public class KillZone : MonoBehaviour
{
    [SerializeField] private string targetTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[KillZone] Something entered: " + other.name + " (tag: " + other.tag + ")");

        if (!other.CompareTag(targetTag)) return;

        Debug.Log("[KillZone] Player entered KillZone — GAME OVER");

        if (GameSession.Instance != null)
        {
            GameSession.Instance.TriggerGameOver();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
        }
    }
}