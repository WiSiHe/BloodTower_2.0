using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DragonHealth : MonoBehaviour
{
    [SerializeField] private int hp = 5;
    [SerializeField] private string keyIdOnDeath = "dragon_key";
    [SerializeField] private bool  destroyOnDeath = true;

    public void ApplyDamage(int dmg)
    {
        hp -= Mathf.Max(1, dmg);
        if (hp <= 0)
        {
            // victory in the minigame
            if (GameSession.Instance != null)
                GameSession.Instance.AddKey(keyIdOnDeath);

            MiniGameManager.Instance?.RegisterDragonDefeat();

            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}