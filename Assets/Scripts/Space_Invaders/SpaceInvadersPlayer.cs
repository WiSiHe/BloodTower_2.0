using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class SpaceInvadersPlayer : MonoBehaviour
{
    public float moveSpeed = 6f;

    private Rigidbody2D rb;
    private Controls controls;
    private Vector2 move;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new Controls();

        controls.Player.Move.performed += ctx =>
        {
            var input = ctx.ReadValue<Vector2>();
            move = new Vector2(input.x, 0f); // restrict to X
        };
        controls.Player.Move.canceled += _ => move = Vector2.zero;
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }
}
