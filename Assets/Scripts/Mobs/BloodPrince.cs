using UnityEngine;

public class BloodprinceAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 5f;        // Distance at which he detects player
    private Transform player;                 // Reference to player

    [Header("Movement")]
    public float chaseSpeed = 3f;             // Speed when chasing
    public float wanderSpeed = 1.5f;          // Speed when wandering
    private Rigidbody2D rb;                   // For physics-safe movement
    private Vector2 movement;

    [Header("Wandering")]
    public float wanderChangeInterval = 3f;   // How often he picks a new random direction
    private float wanderTimer = 0f;
    private Vector2 wanderDirection;

    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        rb = GetComponent<Rigidbody2D>();
        PickNewWanderDirection();
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= detectionRadius)
        {
            // Chase player
            Vector2 direction = (player.position - transform.position).normalized;
            movement = direction * chaseSpeed;
        }
        else
        {
            // Wander around randomly
            wanderTimer -= Time.fixedDeltaTime;
            if (wanderTimer <= 0f)
            {
                PickNewWanderDirection();
            }
            movement = wanderDirection * wanderSpeed;
        }

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    void PickNewWanderDirection()
    {
        // Pick a random direction in isometric space (normalized vector)
        float angle = Random.Range(0f, 360f);
        wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

        // Reset timer
        wanderTimer = wanderChangeInterval;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
