//using UnityEngine;
//using UnityEngine.InputSystem; // <- new Input System

//[RequireComponent(typeof(Rigidbody2D))]
//public class PlayerController : MonoBehaviour
//{
//    [Header("Movement")]
//    public float moveSpeed = 5f;
//    public float jumpForce = 7f;

//    [Header("Grounding")]
//    public Transform groundCheck;         // empty child at feet
//    public float groundCheckRadius = 0.1f;
//    public LayerMask groundMask;

//    private Rigidbody2D rb;
//    private Vector2 moveInput;            // set by OnMove()
//    private bool jumpQueued;              // set by OnJump()

//    void Awake()
//    {
//        rb = GetComponent<Rigidbody2D>();
//    }

//    void Update()
//    {
//        // Horizontal movement (no old Input API here)
//        var v = rb.linearVelocity;
//        v.x = moveInput.x * moveSpeed;
//        rb.linearVelocity = v;

//        // Jump (edge-triggered)
//        if (jumpQueued)
//        {
//            jumpQueued = false;
//            if (IsGrounded())
//            {
//                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
//            }
//        }
//    }

//    // Called automatically by PlayerInput (Behavior: Send Messages)
//    void OnMove(InputValue value)
//    {
//        moveInput = value.Get<Vector2>();
//    }

//    // Called automatically by PlayerInput
//    void OnJump(InputValue value)
//    {
//        if (value.isPressed)
//            jumpQueued = true;
//    }

//    bool IsGrounded()
//    {
//        if (!groundCheck) return true; // fallback so you don't get stuck if not set
//        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask) != null;
//    }

//    // Optional: visualize ground check in editor
//    void OnDrawGizmosSelected()
//    {
//        if (groundCheck)
//        {
//            Gizmos.color = Color.yellow;
//            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
//        }
//    }
//}
