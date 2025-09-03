using System.Collections;
using UnityEngine;

public class DragonShooter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform player;  // tag your player "Player" or assign

    [Header("Shooting Settings")]
    [SerializeField] private float minInterval = 3f;
    [SerializeField] private float maxInterval = 7f;

    [Header("Aim")]
    [SerializeField] private bool rotateToAim = true;

    private GameObject currentFireball;
    private Coroutine loop;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!firePoint) firePoint = transform;
    }

    void OnEnable()  { loop = StartCoroutine(ShootLoop()); }
    void OnDisable() { if (loop != null) StopCoroutine(loop); loop = null; }

    IEnumerator ShootLoop()
    {
        if (!fireballPrefab)
        {
            Debug.LogWarning("[DragonShooter] No fireball prefab assigned.");
            yield break;
        }

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            if (!currentFireball) Shoot();
        }
    }

    void Shoot()
    {
        if (!player || !firePoint || !fireballPrefab) return;

        // optional rotate to aim
        if (rotateToAim)
        {
            Vector2 dir = (player.position - firePoint.position).normalized;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // Add 90 degrees to make the dragon face the player (assuming dragon sprite faces right by default)
            firePoint.rotation = Quaternion.AngleAxis(ang + 90f, Vector3.forward);
        }

        // Use the firePoint's rotation for the fireball
        currentFireball = Instantiate(fireballPrefab, firePoint.position, firePoint.rotation);

        // Use your Fireball API
        var fb = currentFireball.GetComponent<Fireball>();
        if (fb != null)
        {
            fb.LaunchTowards(player);
        }

        // Ensure we clear the reference when it dies
        var note = currentFireball.AddComponent<ProjectileDespawnNotifier>();
        note.onGone = (go) => { if (currentFireball == go) currentFireball = null; };
    }
}
