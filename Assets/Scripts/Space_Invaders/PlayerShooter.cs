using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [Header("Shoot")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform  firePoint;
    [SerializeField] private float      fireCooldown = 0.25f; // faster feel for minigame

    [Header("Powered state")]
    [SerializeField] private bool canDamageDragon = false; // set by MiniGameManager when powered

    private float cooldown;                   // timer counts down to 0
    private InputSystem_Actions inputActions;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        if (!firePoint) firePoint = transform;
    }

    void OnEnable()  
    { 
        inputActions.Enable();
        inputActions.Player.Attack.performed += OnAttackPerformed;
    }
    void OnDisable() 
    { 
        inputActions.Player.Attack.performed -= OnAttackPerformed;
        inputActions.Disable();
    }

    void OnDestroy()
    {
        inputActions?.Dispose();
    }

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

        // Always shoot upward (Vector2.up = 0, 1)
        Quaternion shootRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        var go = Instantiate(arrowPrefab, firePoint.position, shootRotation);
        
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
