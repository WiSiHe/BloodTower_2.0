using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private int   damage = 1;

    [Header("Tags")]
    [SerializeField] private string monsterTag = "Monster";
    [SerializeField] private string dragonTag  = "Dragon";

    private Rigidbody2D rb;
    private bool canDamageDragon;

    public void SetCanDamageDragon(bool can) => canDamageDragon = can;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
    #if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = transform.up * speed; // fire along local +Y
    #else
        rb.velocity = (Vector2)transform.up * speed;
    #endif
        Invoke(nameof(SelfDestruct), lifeTime);
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

    void SelfDestruct() { if (gameObject) Destroy(gameObject); }
}