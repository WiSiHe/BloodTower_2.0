using UnityEngine;

/// Tracks 3 lives for the minigame. On defeat -> removes 1 real heart.
public class MiniPlayerHealth : MonoBehaviour
{
    [SerializeField] private int lives = 3;

    public int Lives => lives;

    public void TakeHit(int amount = 1)
    {
        lives = Mathf.Max(0, lives - Mathf.Abs(amount));
        MiniGameManager.Instance?.NotifyLivesChanged(lives);

        if (lives <= 0)
        {
            // Penalize the main run (one real heart)
            GameSession.Instance?.Damage(1);
            MiniGameManager.Instance?.PlayerFailed();
        }
    }

    public void ResetLives(int to = 3)
    {
        lives = to;
        MiniGameManager.Instance?.NotifyLivesChanged(lives);
    }
}