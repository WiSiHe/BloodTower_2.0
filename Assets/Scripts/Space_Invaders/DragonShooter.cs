using UnityEngine;

public class DragonShooter : MonoBehaviour
{
    public GameObject fireballPrefab;
    public Transform firePoint;
    public Transform player;

    [Header("Shooting Settings")]
    public float minInterval = 3f;
    public float maxInterval = 7f;

    private GameObject currentFireball;

    void Start()
    {
        Invoke(nameof(ScheduleShoot), Random.Range(minInterval, maxInterval));
    }

    void ScheduleShoot()
    {
        Shoot();
        Invoke(nameof(ScheduleShoot), Random.Range(minInterval, maxInterval));
    }

    void Shoot()
    {
        if (currentFireball != null) return; // already have one in scene

        currentFireball = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        var fb = currentFireball.GetComponent<Fireball>();
        if (fb != null) fb.LaunchTowards(player);

        // When destroyed, clear reference
        Destroy(currentFireball, 5f); // safety lifetime
    }
}
