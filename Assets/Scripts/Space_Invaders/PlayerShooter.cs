using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerShooter : MonoBehaviour
{
    [Header("Shoot")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform  firePoint;
    [SerializeField] private float      fireCooldown = 0.25f; // faster feel for minigame

    [Header("Powered state")]
    [SerializeField] private bool canDamageDragon = false; // set by MiniGameManager when powered

    private float cooldown;                   // timer counts down to 0
    private PlayerInput playerInput;
    private InputAction attackAction;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        attackAction = playerInput.actions.FindAction("Attack", throwIfNotFound: true);
        if (!firePoint) firePoint = transform;
    }

    void OnEnable()  => attackAction.performed += OnAttackPerformed;
    void OnDisable() => attackAction.performed -= OnAttackPerformed;

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;
    }

    void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        Shoot();
    }

    void Shoot()
    {
        if (cooldown > 0f) return;
        if (!arrowPrefab || !firePoint)
        {
            Debug.LogWarning("[PlayerShooter] Assign arrowPrefab + firePoint");
            return;
        }

        var go = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
        // pass the powered flag into the projectile
        var arrow = go.GetComponent<Arrow>();
        if (arrow) arrow.SetCanDamageDragon(canDamageDragon);

        cooldown = fireCooldown;
    }

    /// Called by MiniGameManager when the player becomes powered.
    public void SetPowered(bool powered)
    {
        canDamageDragon = powered;
    }
}