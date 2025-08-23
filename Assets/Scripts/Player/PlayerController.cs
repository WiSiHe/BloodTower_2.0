using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 10f;

    [Header("Refs")]
    [SerializeField] private GroundCheck2D groundCheck; // assign in Inspector (or auto-found in Awake)

    private Rigidbody2D rb;
    private Controls controls;
    private Vector2 move;
    private bool isFacingRight = true;
    private Animator animator;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new Controls();
        animator = GetComponent<Animator>();

        // Auto-find GroundCheck2D if you forgot to drag it
        if (groundCheck == null)
            groundCheck = GetComponentInChildren<GroundCheck2D>(true);

        // Input bindings
        controls.Player.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => move = Vector2.zero;
        controls.Player.Jump.performed += ctx => Jump();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void FixedUpdate()
    {
        // Horizontal movement via velocity (lets physics handle vertical)
        rb.linearVelocity = new Vector2(move.x * moveSpeed, rb.linearVelocity.y);

        FlipSprite();

        // Animator params (guard if missing)
        if (animator)
            animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
    }

    void Jump()
    {

        if (groundCheck != null && groundCheck.IsGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            // if (animator) animator.SetBool("IsJumping", true);
        }
    }

    // Sprite flip based on movement direction
    void FlipSprite()
    {
        float x = rb.linearVelocity.x; // use actual movement, not just input
        if ((x > 0f && !isFacingRight) || (x < 0f && isFacingRight))
        {
            isFacingRight = !isFacingRight;
            var scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }
    }
}
