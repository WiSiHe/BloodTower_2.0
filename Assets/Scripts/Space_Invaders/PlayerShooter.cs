using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerShooter : MonoBehaviour
{
    [Header("Shoot")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float fireCooldown = 5f;   // seconds between shots

    float cooldown;                   // timer counts down to 0
    PlayerInput playerInput;
    InputAction attackAction;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        // Find the "Attack" action in the assigned actions asset/map.
        // If it's under a map called "Player", "Player/Attack" also works.
        attackAction = playerInput.actions.FindAction("Attack", throwIfNotFound: true);
    }

    void OnEnable()
    {
        attackAction.performed += OnAttackPerformed;
    }

    void OnDisable()
    {
        attackAction.performed -= OnAttackPerformed;
    }

    void Update()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;
    }

    void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        Shoot();
    }

    void Shoot()
    {
        // Block if we're still cooling down
        if (cooldown > 0f)
            return;

        if (!arrowPrefab || !firePoint)
        {
            Debug.LogWarning("Assign arrowPrefab + firePoint");
            return;
        }

        Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        // Reset cooldown
        cooldown = fireCooldown;
    }
}
