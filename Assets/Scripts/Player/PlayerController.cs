using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 10f;
    [Tooltip("If true: free 2D movement + 8-way facing. If false: platformer + jump.")]
    public bool isIsometric = false;

    [Header("Isometric")]
    [Tooltip("Minimum input magnitude before we consider a new facing direction.")]
    public float isoFaceDeadzone = 0.15f;
    [Tooltip("Prefer 4-way (N,E,S,W) instead of 8-way when true.")]
    public bool isoFourWay = false;

    [Header("Refs")]
    [SerializeField] private GroundCheck2D groundCheck; // assign in Inspector (or auto-found in Awake)

    private Rigidbody2D rb;
    private Controls controls;
    private Vector2 move;
    private Vector2 lastNonZeroMove = Vector2.right;
    private bool isFacingRight = true;
    private Animator animator;

    // 0=N,1=NE,2=E,3=SE,4=S,5=SW,6=W,7=NW
    private enum IsoDir { N = 0, NE = 1, E = 2, SE = 3, S = 4, SW = 5, W = 6, NW = 7 }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new Controls();
        animator = GetComponent<Animator>();

        if (groundCheck == null)
            groundCheck = GetComponentInChildren<GroundCheck2D>(true);

        controls.Player.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => move = Vector2.zero;
        controls.Player.Jump.performed += ctx => Jump();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void FixedUpdate()
    {
        if (isIsometric)
        {
            // free 2D movement
            rb.linearVelocity = move * moveSpeed;

            // remember last meaningful input for idle facing
            if (move.sqrMagnitude > isoFaceDeadzone * isoFaceDeadzone)
                lastNonZeroMove = move;

            // update animator facing (8-way or 4-way)
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
            // platformer: x-only, keep gravity on y
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
        if (!isIsometric && groundCheck != null && groundCheck.IsGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            if (animator) animator.SetBool("isJumping", true);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (groundCheck != null && groundCheck.IsGrounded)
        {
            if (animator) animator.SetBool("isJumping", false);
        }
    }

    // PLATFORMER-ONLY: left/right sprite flip
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

    // ----- helpers -----

    // Map a vector to an isometric facing index (8-way or 4-way)
    IsoDir GetIsoDirection(Vector2 v, bool fourWay)
    {
        // angle in degrees: 0 = right(E), 90 = up(N)
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        // remap so 0 = North to make indices intuitive
        angle = 90f - angle;
        if (angle < 0f) angle += 360f;

        if (fourWay)
        {
            // 0=N (315..45), 2=E (45..135), 4=S (135..225), 6=W (225..315)
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
            // 8 sectors of 45°
            int idx8 = Mathf.RoundToInt(angle / 45f) % 8;
            return (IsoDir)idx8; // 0=N,1=NE,2=E,3=SE,4=S,5=SW,6=W,7=NW
        }
    }
}
