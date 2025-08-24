using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 3f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // Launch straight up like Space Invaders
        rb.linearVelocity = Vector2.up * speed;

        // Destroy after a while to avoid clutter
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Monster"))
        {
            Destroy(other.gameObject); // kill monster
            Destroy(gameObject);       // destroy arrow
        }
    }
}
