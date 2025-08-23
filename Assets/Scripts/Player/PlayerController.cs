using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 10f;
    public bool isIsometric = false;
    public bool canJump = true; // <--- NEW toggle

    [Header("Isometric")]
    public float isoFaceDeadzone = 0.15f;
    public bool isoFourWay = false;

    [Header("Refs")]
    [SerializeField] private GroundCheck2D groundCheck;

    private Rigidbody2D rb;
    private Controls controls;
    private Vector2 move;
    private Vector2 lastNonZeroMove = Vector2.right;
    private bool isFacingRight = true;
    private Animator animator;

    private enum IsoDir { N = 0, NE = 1, E = 2, SE = 3, S = 4, SW = 5, W = 6, NW = 7 }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (groundCheck == null)
            groundCheck = GetComponentInChildren<GroundCheck2D>(true);
    }

    void OnEnable()
    {
        if (controls == null)
        {
            controls = new Controls();
            controls.Player.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
            controls.Player.Move.canceled += ctx => move = Vector2.zero;
            controls.Player.Jump.performed += ctx => Jump();
        }
        controls.Enable();
    }

    void OnDisable()
    {
        if (controls != null)
            controls.Disable();
    }

    void OnDestroy()
    {
        if (controls != null)
        {
            controls.Player.Move.performed -= ctx => move = ctx.ReadValue<Vector2>();
            controls.Player.Move.canceled -= ctx => move = Vector2.zero;
            controls.Player.Jump.performed -= ctx => Jump();
            controls.Dispose();
            controls = null;
        }
    }

    void FixedUpdate()
    {
        if (isIsometric)
        {
            rb.linearVelocity = move * moveSpeed;

            if (move.sqrMagnitude > isoFaceDeadzone * isoFaceDeadzone)
                lastNonZeroMove = move;

            if (animator)
            {
                var v = move.sqrMagnitude > isoFaceDeadzone * isoFaceDeadzone ? move : lastNonZeroMove;
                int dirIndex = (int)GetIsoDirection(v, isoFourWay);
                animator.SetInteger("dirIndex", dirIndex);
                animator.SetFloat("speed", rb.linearVelocity.magnitude);
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(move.x * moveSpeed, rb.linearVelocity.y);
            FlipSprite();

            if (animator)
            {
                animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
                animator.SetFloat("yVelocity", rb.linearVelocity.y);
            }
        }
    }

    void Jump()
    {
        // Now checks both isIsometric and canJump
        if (canJump && !isIsometric && groundCheck != null && groundCheck.IsGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            if (animator) animator.SetBool("isJumping", true);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (groundCheck != null && groundCheck.IsGrounded)
            if (animator) animator.SetBool("isJumping", false);
    }

    void FlipSprite()
    {
        float x = rb.linearVelocity.x;
        if ((x > 0f && !isFacingRight) || (x < 0f && isFacingRight))
        {
            isFacingRight = !isFacingRight;
            var scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }
    }

    IsoDir GetIsoDirection(Vector2 v, bool fourWay)
    {
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        angle = 90f - angle;
        if (angle < 0f) angle += 360f;

        if (fourWay)
        {
            int idx4 = Mathf.RoundToInt(angle / 90f) % 4;
            switch (idx4)
            {
                case 0: return IsoDir.N;
                case 1: return IsoDir.E;
                case 2: return IsoDir.S;
                default: return IsoDir.W;
            }
        }
        else
        {
            int idx8 = Mathf.RoundToInt(angle / 45f) % 8;
            return (IsoDir)idx8;
        }
    }
}
