using UnityEngine;

/// Kill zone specifically for the Boss (King). When the King enters, route to victory.
[RequireComponent(typeof(Collider2D))]
public class KingFallZone : MonoBehaviour
{
    [Header("Who triggers victory")]
    [SerializeField] private string bossTag = "Boss";

    [Header("Optional: also kill player if they fall here")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool killPlayerToo = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(bossTag))
        {
            Debug.Log("[KingFallZone] Boss fell — triggering victory.");
            EndingRouter.GoToVictory();
            return;
        }

        if (killPlayerToo && other.CompareTag(playerTag))
        {
            Debug.Log("[KingFallZone] Player fell into boss pit — GAME OVER.");
            // If you have a GameOver loader, call it here. Otherwise just reload Tutorial:
            // SceneManager.LoadScene("GameOver");
        }
    }
}