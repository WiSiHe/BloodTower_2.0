using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Fireball : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 5f;

    private Rigidbody2D rb;

    // Call this right after Instantiate
    public void LaunchTowards(Transform target)
    {
        Vector2 dir = (target.position - transform.position).normalized;
        rb.linearVelocity = dir * speed;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void OnEnable() => Invoke(nameof(DestroySelf), lifeTime);
    void OnDisable() => CancelInvoke();

    void DestroySelf() => Destroy(gameObject);

    // Optional damage
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // TODO: deal damage
            DestroySelf();
        }
        // Also destroy on walls, etc., if you like
    }
}
