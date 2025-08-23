using UnityEngine;

public class Monster : MonoBehaviour
{
    // Optional health if you want to expand later
    public int health = 1;

    public void TakeHit(int damage = 1)
    {
        health -= damage;
        if (health <= 0) Destroy(gameObject);
    }
}
