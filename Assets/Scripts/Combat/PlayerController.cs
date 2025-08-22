using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour{
    public float moveSpeed = 6f;

    private Rigidbody2D rb;
    private Controls controls;
    private Vector2 move;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new Controls();

        // Read Move action
        controls.Player.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled  += ctx => move = Vector2.zero;
    }

    void OnEnable()  => controls.Enable();
    void OnDisable() => controls.Disable();

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }
}