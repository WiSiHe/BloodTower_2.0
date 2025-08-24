using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float fireCooldown = 5f;   // seconds between shots

    float cooldown; // timer counts down to 0

    void Update()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;
    }

    // MUST be public, name must match action: Attack
    public void OnAttack()
    {
        Shoot();
    }

    void Shoot()
    {
        // Block if we're still cooling down
        if (cooldown > 0f)
            return;

        if (!arrowPrefab || !firePoint)
        {
            Debug.LogWarning("Assign arrowPrefab + firePoint");
            return;
        }

        Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        // Reset cooldown
        cooldown = fireCooldown;
    }
}
