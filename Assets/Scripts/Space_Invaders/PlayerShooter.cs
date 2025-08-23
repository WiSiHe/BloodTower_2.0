using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float fireCooldown = 0.5f;

    float cooldown;

    void Update() => cooldown -= Time.deltaTime;

    // MUST be public, name must match action: Attack
public void OnAttack()
{
    Shoot();
}

    void Shoot()
    {
        if (!arrowPrefab || !firePoint)
        {
            Debug.LogWarning("Assign arrowPrefab + firePoint");
            return;
        }
        Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
    }
}
