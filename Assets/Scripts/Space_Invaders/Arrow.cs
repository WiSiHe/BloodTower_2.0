using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 8f; // Increased from 4f to 8f for longer travel
    [SerializeField] private int   damage = 1;

    [Header("Tags")]
    [SerializeField] private string monsterTag = "Monster";
    [SerializeField] private string dragonTag  = "Dragon";

    [Header("Powered Effects")]
    [SerializeField] private GameObject poweredEffectPrefab;
    [SerializeField] private Color poweredColor = Color.yellow;
    [SerializeField] private float poweredGlowIntensity = 2.0f;

    private Rigidbody2D rb;
    private bool canDamageDragon;
    private GameObject poweredEffect;
    private SpriteRenderer spriteRenderer;

    public void SetCanDamageDragon(bool can) 
    { 
        canDamageDragon = can;
        UpdatePoweredVisuals();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
    #if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = transform.up * speed; // fire along local +Y
    #else
        rb.velocity = (Vector2)transform.up * speed;
    #endif
        Invoke(nameof(SelfDestruct), lifeTime);
        
        // Update visuals based on powered state
        UpdatePoweredVisuals();
    }

    void OnDisable() => CancelInvoke();

    void OnTriggerEnter2D(Collider2D other)
    {
        // Hit monster?
        if (other.CompareTag(monsterTag))
        {
            other.GetComponent<MonsterHealth>()?.ApplyDamage(damage);
            SelfDestruct();
            return;
        }

        // Hit dragon?
        if (other.CompareTag(dragonTag))
        {
            if (canDamageDragon)
            {
                other.GetComponent<DragonHealth>()?.ApplyDamage(damage);
                SelfDestruct();
            }
            else
            {
                // bounce off / ignore when not powered
                SelfDestruct();
            }
        }
    }

    private void UpdatePoweredVisuals()
    {
        if (canDamageDragon)
        {
            // Change arrow color to powered color with glow intensity
            if (spriteRenderer != null)
            {
                Color glowColor = poweredColor * poweredGlowIntensity;
                glowColor.a = 1f; // Keep alpha at 1
                spriteRenderer.color = glowColor;
            }

            // Spawn powered effect if prefab is assigned
            if (poweredEffectPrefab != null && poweredEffect == null)
            {
                poweredEffect = Instantiate(poweredEffectPrefab, transform);
                poweredEffect.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            // Reset to normal color
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            // Remove powered effect
            if (poweredEffect != null)
            {
                Destroy(poweredEffect);
                poweredEffect = null;
            }
        }
    }

    void SelfDestruct() 
    { 
        // Clean up powered effect before destroying
        if (poweredEffect != null)
        {
            Destroy(poweredEffect);
        }
        
        if (gameObject) Destroy(gameObject); 
    }
}
