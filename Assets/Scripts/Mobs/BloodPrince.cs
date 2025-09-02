using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BloodprinceAI : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Explicit spawn locations (e.g. 4 corners). One will be chosen at random at Start.")]
    public Transform[] spawnPoints;

    [Tooltip("If true, skip spawn points blocked by walls/players.")]
    public bool validateSpawn = true;

    [Tooltip("Layers that make a spawn point invalid (e.g., Walls, Player).")]
    public LayerMask spawnBlockLayers;

    [Tooltip("Radius used to test if spawn point is blocked.")]
    public float spawnCheckRadius = 0.25f;

    [Header("Detection")]
    public float detectionRadius = 5f;
    private Transform player;

    [Header("Movement")]
    public float chaseSpeed = 3f;
    public float wanderSpeed = 1.6f;

    [Header("Wandering")]
    public float wanderChangeInterval = 3f;
    public float wanderJitter = 0.75f;

    [Header("Collision")]
    public LayerMask wallLayers;
    public float skinWidth = 0.02f;
    [Range(0f, 1f)] public float bounceKeepFactor = 0.35f;
    
    [Header("Combat")]
    [Tooltip("Damage dealt to player on collision")]
    public int damageAmount = 1;
    [Tooltip("Time to back off after damaging player")]
    public float backoffDuration = 2f;
    [Tooltip("Speed when backing off from player")]
    public float backoffSpeed = 4f;
    [Tooltip("Cooldown between damage attacks")]
    public float damageCooldown = 1f;

    // ---- private state ----
    Rigidbody2D rb;
    Vector2 moveDir = Vector2.zero;
    float wanderTimer = 0f;
    
    // Combat state
    private float _backoffTimer = 0f;
    private float _damageCooldownTimer = 0f;
    private bool _isBackingOff = false;

    // Cast buffers
    readonly RaycastHit2D[] castHits = new RaycastHit2D[4];
    ContactFilter2D castFilter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        castFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = wallLayers,
            useTriggers = false
        };
    }

    void Start()
    {
        // Player ref
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        // Choose explicit spawn
        Vector2? chosen = ChooseSpawnPoint();
        if (chosen.HasValue) rb.position = chosen.Value;

        PickNewWanderDirection(true);
    }

    void FixedUpdate()
    {
        // Update timers
        _backoffTimer -= Time.fixedDeltaTime;
        _damageCooldownTimer -= Time.fixedDeltaTime;
        
        // Check if backoff period is over
        if (_isBackingOff && _backoffTimer <= 0f)
        {
            _isBackingOff = false;
        }
        
        // 1) Desired velocity (backoff, chase, or wander)
        Vector2 desiredVel;
        if (_isBackingOff && player)
        {
            // Back off from player
            Vector2 awayFromPlayer = (rb.position - (Vector2)player.position).normalized;
            desiredVel = awayFromPlayer * backoffSpeed;
        }
        else if (player && Vector2.Distance(rb.position, (Vector2)player.position) <= detectionRadius)
        {
            Vector2 toPlayer = ((Vector2)player.position - rb.position).normalized;
            desiredVel = toPlayer * chaseSpeed;
        }
        else
        {
            wanderTimer -= Time.fixedDeltaTime;
            if (wanderTimer <= 0f) PickNewWanderDirection(false);
            desiredVel = moveDir * wanderSpeed;
        }

        // 2) Collision-safe move with rb.Cast
        Vector2 from = rb.position;
        Vector2 to   = from + desiredVel * Time.fixedDeltaTime;
        Vector2 delta = to - from;
        float distance = delta.magnitude;

        if (distance > 0f)
        {
            Vector2 dir = delta / distance;
            int hits = rb.Cast(dir, castFilter, castHits, distance + skinWidth);

            if (hits > 0)
            {
                float shortest = distance;
                Vector2 normalSum = Vector2.zero;

                for (int i = 0; i < hits; i++)
                {
                    var h = castHits[i];
                    if (!h.collider) continue;
                    shortest = Mathf.Min(shortest, Mathf.Max(0f, h.distance - skinWidth));
                    normalSum += h.normal;
                }

                rb.MovePosition(from + dir * shortest);

                // Bounce the wander direction if we hit something while NOT chasing
                if (!player || Vector2.Distance(rb.position, (Vector2)player.position) > detectionRadius)
                {
                    if (normalSum.sqrMagnitude > 0.0001f)
                    {
                        Vector2 avgNormal = (normalSum / hits).normalized;
                        Vector2 reflected = Vector2.Reflect(moveDir, avgNormal).normalized;
                        moveDir = (reflected * (1f - bounceKeepFactor) + moveDir * bounceKeepFactor).normalized;
                        wanderTimer = Random.Range(0.35f, 0.75f);
                    }
                }
            }
            else
            {
                rb.MovePosition(to);
            }
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we collided with the player and can deal damage
        if (collision.gameObject.CompareTag("Player") && _damageCooldownTimer <= 0f)
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }
    
    void DealDamageToPlayer(GameObject playerObj)
    {
        // Use the global GameSession to deal damage
        if (GameSession.Instance != null)
        {
            // Deal damage to the global health system
            GameSession.Instance.Damage(damageAmount);
            
            // Start backoff behavior
            _isBackingOff = true;
            _backoffTimer = backoffDuration;
            _damageCooldownTimer = damageCooldown;
            
            Debug.Log($"[BloodPrince] Dealt {damageAmount} damage to player via GameSession. Backing off for {backoffDuration}s");
        }
        else
        {
            Debug.LogWarning("[BloodPrince] GameSession.Instance is null! Cannot deal damage.");
        }
    }

    // ---------- helpers ----------
    Vector2? ChooseSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[BloodprinceAI] No spawn points assigned. Using current position.");
            return rb.position;
        }

        // Randomize order
        List<int> idxs = new List<int>(spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++) idxs.Add(i);
        for (int i = 0; i < idxs.Count; i++) // Fisher-Yates shuffle
        {
            int j = Random.Range(i, idxs.Count);
            (idxs[i], idxs[j]) = (idxs[j], idxs[i]);
        }

        foreach (int i in idxs)
        {
            if (!spawnPoints[i]) continue;
            Vector2 pos = spawnPoints[i].position;

            if (!validateSpawn) return pos;

            // blocked by walls/players?
            Collider2D hit = Physics2D.OverlapCircle(pos, spawnCheckRadius, spawnBlockLayers);
            if (hit == null) return pos;
        }

        // If all blocked, take the first valid transform’s position anyway
        foreach (var t in spawnPoints) if (t) return (Vector2)t.position;
        return rb.position;
    }

    void PickNewWanderDirection(bool instant)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        moveDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

        float baseT = wanderChangeInterval;
        wanderTimer = baseT + Random.Range(-wanderJitter, wanderJitter);
        if (instant) wanderTimer *= 0.5f;
    }

    void OnDrawGizmosSelected()
    {
        // detection radius
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // backoff indicator
        if (_isBackingOff)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f); // Yellow for backoff
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        // spawn points
        if (spawnPoints != null)
        {
            foreach (var t in spawnPoints)
            {
                if (!t) continue;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(t.position, spawnCheckRadius);
                Gizmos.DrawLine(t.position + Vector3.left * 0.2f, t.position + Vector3.right * 0.2f);
                Gizmos.DrawLine(t.position + Vector3.up * 0.2f,   t.position + Vector3.down * 0.2f);
            }
        }
    }
}
