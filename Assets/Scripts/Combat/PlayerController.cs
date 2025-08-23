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
    private bool isFacingRight = true; // Track player facing direction
    private Animator animator; // Reference to Animator

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new Controls();
        animator = GetComponent<Animator>(); // Get Animator component

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
        FlipSprite(); // Flip sprite based on movement direction
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);

        // Update Animator parameters
        animator.SetFloat("xVelocity", move.magnitude); // Set Speed parameter
    }

    void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;

            // Update Animator parameter
            // animator.SetBool("IsJumping", true);
        }
    }

    // Simple ground check using collision
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;

            // Update Animator parameter
            // animator.SetBool("IsJumping", false);
        }
    }

    // Flip the player sprite based on movement direction
    void FlipSprite()
    {
       if (move.x > 0 && !isFacingRight || move.x < 0 && isFacingRight)
       {
           isFacingRight = !isFacingRight;
           Vector3 scale = transform.localScale;
           scale.x *= -1f;
           transform.localScale = scale;
       }
       
       
       
    }
}