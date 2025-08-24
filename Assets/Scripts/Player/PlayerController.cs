using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 10f;
    public bool isIsometric = false;
    public bool canJump = true;

    [Header("Double Jump")]
    [Tooltip("If true, allows an additional jump while airborne.")]
    public bool doubleJumpEnabled = true;
    [Tooltip("How many extra jumps allowed in the air (1 = classic double jump).")]
    public int extraAirJumps = 1;

    [Header("Isometric")]
    public float isoFaceDeadzone = 0.15f;
    public bool isoFourWay = false;

    [Header("Refs")]
    [SerializeField] private GroundCheck2D groundCheck; // assign in Inspector (or auto-found in Awake)
    [SerializeField] private AudioSource jumpAudio;
    private Rigidbody2D rb;
    private Controls controls;
    private Vector2 move;
    private Vector2 lastNonZeroMove = Vector2.right;
    private bool isFacingRight = true;
    private Animator animator;

    // Jump state
    private bool wasGrounded = false;
    private int airJumpsLeft = 0;

    private enum IsoDir { N = 0, NE = 1, E = 2, SE = 3, S = 4, SW = 5, W = 6, NW = 7 }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (groundCheck == null)
            groundCheck = GetComponentInChildren<GroundCheck2D>(true);
        if (jumpAudio == null)
            jumpAudio = GetComponentInChildren<AudioSource>();
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
            // Note: Unsubscribing inline lambdas will not detach; keeping for parity with your original.
            controls.Player.Move.performed -= ctx => move = ctx.ReadValue<Vector2>();
            controls.Player.Move.canceled -= ctx => move = Vector2.zero;
            controls.Player.Jump.performed -= ctx => Jump();
            controls.Dispose();
            controls = null;
        }
    }

    void FixedUpdate()
    {
        bool groundedNow = groundCheck != null && groundCheck.IsGrounded;

        // Grounded state transition: reset air jumps when you touch ground
        if (groundedNow && !wasGrounded)
        {
            airJumpsLeft = Mathf.Max(0, extraAirJumps);
            if (animator) animator.SetBool("isJumping", false);
        }
        wasGrounded = groundedNow;

        if (isIsometric)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = move * moveSpeed;
#else
            rb.velocity = move * moveSpeed;
#endif
            if (move.sqrMagnitude > isoFaceDeadzone * isoFaceDeadzone)
                lastNonZeroMove = move;

            if (animator)
            {
#if UNITY_6000_0_OR_NEWER
                var v = move.sqrMagnitude > isoFaceDeadzone * isoFaceDeadzone ? move : lastNonZeroMove;
                int dirIndex = (int)GetIsoDirection(v, isoFourWay);
                animator.SetInteger("dirIndex", dirIndex);
                animator.SetFloat("speed", rb.linearVelocity.magnitude);
#else
                var v = move.sqrMagnitude > isoFaceDeadzone * isoFaceDeadzone ? move : lastNonZeroMove;
                int dirIndex = (int)GetIsoDirection(v, isoFourWay);
                animator.SetInteger("dirIndex", dirIndex);
                animator.SetFloat("speed", rb.velocity.magnitude);
#endif
            }
        }
        else
        {
            // Side-scroller horizontal
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector2(move.x * moveSpeed, rb.linearVelocity.y);
            float xVel = rb.linearVelocity.x;
            float yVel = rb.linearVelocity.y;
#else
            rb.velocity = new Vector2(move.x * moveSpeed, rb.velocity.y);
            float xVel = rb.velocity.x;
            float yVel = rb.velocity.y;
#endif
            FlipSprite();

            if (animator)
            {
                animator.SetFloat("xVelocity", Mathf.Abs(xVel));
                animator.SetFloat("yVelocity", yVel);
            }
        }
    }

    void Jump()
    {
        // Only in side-scroller mode and if jump allowed at all
        if (isIsometric || !canJump) return;

        bool groundedNow = groundCheck != null && groundCheck.IsGrounded;

        // Ground jump
        if (groundedNow)
        {
            DoJump();
            // Reset air jumps for the upcoming airtime
            airJumpsLeft = Mathf.Max(0, extraAirJumps);
            return;
        }

        // Air jump(s)
        if (doubleJumpEnabled && airJumpsLeft > 0)
        {
            // Optional: zero out vertical velocity for consistent second jump feel
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
#else
            rb.velocity = new Vector2(rb.velocity.x, 0f);
#endif
            DoJump();
            airJumpsLeft--;
        }
    }

    void DoJump()
    {
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        if (animator) animator.SetBool("isJumping", true);
        if (jumpAudio) jumpAudio.Play();
    }

    void FlipSprite()
    {
#if UNITY_6000_0_OR_NEWER
        float x = rb.linearVelocity.x;
#else
        float x = rb.velocity.x;
#endif
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
