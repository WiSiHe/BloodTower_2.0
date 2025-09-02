using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MonsterHealth : MonoBehaviour
{
    [SerializeField] private int hp = 1;
    [SerializeField] private bool destroyOnDeath = true;

    public void ApplyDamage(int dmg)
    {
        hp -= Mathf.Max(1, dmg);
        if (hp <= 0)
        {
            MiniGameManager.Instance?.RegisterMonsterKill();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}