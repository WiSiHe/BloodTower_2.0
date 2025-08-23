using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour{
    public float moveSpeed = 6f;
    public float jumpForce = 10f; // Added jump force

    private Rigidbody2D rb;
    private Controls controls;
    private Vector2 move;
    private bool isGrounded = true; // Simple grounded check

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new Controls();

        // Read Move action
        controls.Player.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled  += ctx => move = Vector2.zero;
        // Read Jump action
        controls.Player.Jump.performed += ctx => Jump();
    }

    void OnEnable()  => controls.Enable();
    void OnDisable() => controls.Disable();

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);
    }

    void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;
        }
    }

    // Simple ground check using collision
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }
}