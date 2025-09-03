using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f; // Increased for more responsive movement
    public float jumpForce = 35f; // Increased to work with higher gravity scale (3x)
    public bool isIsometric = false;
    public bool canJump = true;
    
    [Header("Movement Feel")]
    [Tooltip("How quickly the player accelerates to max speed")]
    public float acceleration = 35f; // Increased for more responsive acceleration
    [Tooltip("How quickly the player decelerates when stopping")]
    public float deceleration = 75f; // Increased for snappier stops
    [Tooltip("Additional friction when grounded to prevent sliding")]
    public float groundFriction = 10f;
    [Tooltip("Gravity scale for the Rigidbody2D (higher = snappier jumps)")]
    [Range(1f, 5f)] public float gravityScale = 3f; // Higher gravity for snappier jumps as per Game Dev Beginner article
    [Tooltip("Air control multiplier (0 = no air control, 1 = full control)")]
    [Range(0f, 1f)] public float airControl = 0.8f; // Increased for better mid-air precision
    [Tooltip("Time in seconds after leaving ground where you can still jump")]
    public float coyoteTime = 0.15f; // Slightly increased for more forgiving platforming
    [Tooltip("Time in seconds before landing where jump input is buffered")]
    public float jumpBufferTime = 0.15f; // Slightly increased for better responsiveness
    [Tooltip("Variable jump height - release jump early for shorter jumps")]
    public bool variableJumpHeight = true;
    [Tooltip("Multiplier for jump velocity when releasing jump early")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.4f; // Lower for more dramatic jump height variation

    [Header("Double Jump")]
    [Tooltip("If true, allows an additional jump while airborne.")]
    public bool doubleJumpEnabled = true;
    [Tooltip("How many extra jumps allowed in the air (1 = classic double jump).")]
    public int extraAirJumps = 1;

    [Header("Dash")]
    [Tooltip("If true, allows dashing in the air and on ground")]
    public bool dashEnabled = true;
    [Tooltip("Dash force/magnitude (works in all directions for isometric, horizontal only for side-scroller)")]
    public float dashForce = 25f; // Increased for more impactful dash
    [Tooltip("Dash duration in seconds")]
    public float dashDuration = 0.15f; // Slightly shorter for snappier feel
    [Tooltip("Cooldown between dashes in seconds")]
    public float dashCooldown = 0.4f; // Reduced for more responsive gameplay
    [Tooltip("How much control player has during dash (0 = no control, 1 = full control)")]
    [Range(0f, 1f)] public float dashControl = 0.2f; // Reduced for more committed dash feel
    [Tooltip("Brief invincibility duration during dash")]
    public float dashInvincibilityTime = 0.1f;
    [Tooltip("Screen shake intensity during dash")]
    public float dashScreenShake = 0.3f;

    [Header("Isometric")]
    public float isoFaceDeadzone = 0.15f;
    public bool isoFourWay = false;
    [Tooltip("Additional deceleration multiplier for isometric mode to reduce floaty feel")]
    [Range(0.5f, 2f)] public float isoDecelerationMultiplier = 1.5f;
    [Tooltip("Additional acceleration multiplier for isometric mode for more responsive movement")]
    [Range(0.5f, 2f)] public float isoAccelerationMultiplier = 1.2f;

    [Header("Refs")]
    [SerializeField] private GroundCheck2D groundCheck; // assign in Inspector (or auto-found in Awake)
    
    
    
    [Header("Collision")]
    [Tooltip("Force applied to push away from colliding objects to prevent sticking")]
    public float collisionPushForce = 1f;
    private Rigidbody2D rb;
    private InputSystem_Actions controls;
    private Vector2 move;
    private Vector2 lastNonZeroMove = Vector2.right;
    private bool isFacingRight = true;
    private Animator animator;

    // Jump state
    private bool wasGrounded = false;
    private int airJumpsLeft = 0;
    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter = 0f;
    private bool isJumping = false;
    private bool jumpInputReleased = false;
    
    // Ground detection timing
    private float groundCheckDelay = 0.05f;
    private float lastAirTime = 0f;
    private bool trulyGrounded = false;
    
    // Dash state
    private bool isDashing = false;
    private bool isDashInvincible = false;
    private float dashInvincibilityTimer = 0f;
    
    // Bounce state

    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection = Vector2.right;
    
    // Input system callbacks (to prevent memory leaks)
    private System.Action<InputAction.CallbackContext> movePerformed;
    private System.Action<InputAction.CallbackContext> moveCanceled;
    private System.Action<InputAction.CallbackContext> jumpPerformed;
    private System.Action<InputAction.CallbackContext> jumpCanceled;
    private System.Action<InputAction.CallbackContext> dashPerformed;

    private enum IsoDir { N = 0, NE = 1, E = 2, SE = 3, S = 4, SW = 5, W = 6, NW = 7 }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (groundCheck == null)
            groundCheck = GetComponentInChildren<GroundCheck2D>(true);
            
        // Set gravity scale for snappier jumps (Game Dev Beginner approach)
        rb.gravityScale = gravityScale;
        // Ensure continuous collision detection so high-speed jumps still trigger pads
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void OnEnable()
    {
        if (controls == null)
        {
            controls = new InputSystem_Actions();
            movePerformed = ctx => move = ctx.ReadValue<Vector2>();
            moveCanceled = ctx => move = Vector2.zero;
            jumpPerformed = ctx => OnJumpPressed();
            jumpCanceled = ctx => OnJumpReleased();
            dashPerformed = ctx => OnDashPressed();
            
            controls.Player.Move.performed += movePerformed;
            controls.Player.Move.canceled += moveCanceled;
            controls.Player.Jump.performed += jumpPerformed;
            controls.Player.Jump.canceled += jumpCanceled;
            controls.Player.Sprint.performed += dashPerformed;
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
            controls.Player.Move.performed -= movePerformed;
            controls.Player.Move.canceled -= moveCanceled;
            controls.Player.Jump.performed -= jumpPerformed;
            controls.Player.Jump.canceled -= jumpCanceled;
            controls.Player.Sprint.performed -= dashPerformed;
            controls.Dispose();
            controls = null;
        }
    }


    private bool isBouncing = false;
    private float bounceLockTimer = 0f; // duration to lock movement after bounce
    
    void FixedUpdate()
    {
        // Update bounce lock timer
        if (bounceLockTimer > 0f)
        {
            bounceLockTimer -= Time.fixedDeltaTime;
            if (bounceLockTimer <= 0f)
            {
                isBouncing = false;
            }
        }
        

        
        bool groundedNow = groundCheck != null && groundCheck.IsGrounded;
        
        // Track air time for better ground detection
        if (!groundedNow)
        {
            lastAirTime = Time.time;
        }
        
        // Only consider truly grounded if we've been on ground for a short time
        trulyGrounded = groundedNow && (Time.time - lastAirTime) > groundCheckDelay;

        // Update coyote time and jump buffer
        if (groundedNow)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.fixedDeltaTime;
        }

        // Grounded state transition: reset air jumps when you touch ground
        if (trulyGrounded && !wasGrounded)
        {
            airJumpsLeft = Mathf.Max(0, extraAirJumps);
            isJumping = false;
        }
        wasGrounded = trulyGrounded;

        // Handle dash
        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }
        
        // Update dash cooldown
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.fixedDeltaTime;
        }
        
        // Update dash invincibility
        if (isDashInvincible)
        {
            dashInvincibilityTimer -= Time.fixedDeltaTime;
            if (dashInvincibilityTimer <= 0f)
            {
                isDashInvincible = false;
            }
        }

        // Handle variable jump height (skip if bouncing to prevent interference)
        if (variableJumpHeight && isJumping && jumpInputReleased && !isBouncing)
        {
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }
        
        // Set gravity scale for snappier jumps (Game Dev Beginner approach)
        if (rb.gravityScale != gravityScale)
        {
            rb.gravityScale = gravityScale;
        }

        // Handle movement (skip if bouncing)
        if (!isBouncing)
        {
            if (isIsometric)
            {
                HandleIsometricMovement();
            }
            else
            {
                HandleSideScrollerMovement(groundedNow);
            }
        }
        else
        {

        }
    }
    
    void HandleIsometricMovement()
    {
        // Handle dashing
        if (isDashing)
        {
            // During dash, maintain dash velocity with limited control
            Vector2 dashVelocity = dashDirection * dashForce;
            Vector2 currentVelocity = rb.linearVelocity;
            
            // Allow some control during dash
            if (move.sqrMagnitude > 0.01f)
            {
                dashVelocity = move.normalized * dashForce;
                dashDirection = move.normalized;
            }
            
            // Blend between current velocity and dash velocity
            rb.linearVelocity = Vector2.Lerp(currentVelocity, dashVelocity, dashControl);
        }
        else
        {
            // AGGRESSIVE ANTI-SLIDING for isometric (same as side-scroller)
            bool shouldBeMoving = move.sqrMagnitude > 0.01f;
            
            if (!shouldBeMoving)
            {
                // FORCE STOP - completely override physics when not moving
                rb.linearVelocity = new Vector2(0f, 0f);
                

            }
            else
            {
                // Normal movement when input is active
                Vector2 targetVelocity = move * moveSpeed;
                Vector2 currentVelocity = rb.linearVelocity;
                
                Vector2 velocityDifference = targetVelocity - currentVelocity;
                float accelerationRate = acceleration * isoAccelerationMultiplier;
                
                Vector2 newVelocity = currentVelocity + velocityDifference.normalized * accelerationRate * Time.fixedDeltaTime;
                
                // Clamp to max speed
                if (newVelocity.magnitude > moveSpeed)
                {
                    newVelocity = newVelocity.normalized * moveSpeed;
                }
                
                rb.linearVelocity = newVelocity;
            }
        }
        
        if (move.sqrMagnitude > isoFaceDeadzone * isoFaceDeadzone)
            lastNonZeroMove = move;

        // Handle sprite flipping for isometric mode
        if (Mathf.Abs(move.x) > 0.01f)
        {
            if ((move.x > 0f && !isFacingRight) || (move.x < 0f && isFacingRight))
            {
                isFacingRight = !isFacingRight;
                var scale = transform.localScale;
                scale.x *= -1f;
                transform.localScale = scale;
            }
        }

        if (animator)
        {
            var v = move.sqrMagnitude > isoFaceDeadzone * isoFaceDeadzone ? move : lastNonZeroMove;
            int dirIndex = (int)GetIsoDirection(v, isoFourWay);
            animator.SetInteger("dirIndex", dirIndex);
            
            // Use direct physics values for more responsive animation
            float currentSpeed = rb.linearVelocity.magnitude;
            
            // Ensure speed goes to 0 when not moving for idle animation
            if (move.sqrMagnitude < 0.01f && currentSpeed < 0.1f)
            {
                currentSpeed = 0f;
            }
            
            animator.SetFloat("speed", currentSpeed);
            

            
            // Set X and Y velocity for isometric movement
            animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
            animator.SetFloat("yVelocity", rb.linearVelocity.y);
            
            // In isometric mode, never set isJumping to true (no jumping in isometric)
            animator.SetBool("isJumping", false);
        }
    }
    
    void HandleSideScrollerMovement(bool groundedNow)
    {
        // Handle dashing
        if (isDashing)
        {
            // During dash, maintain dash velocity with limited control
            Vector2 dashVelocity = dashDirection * dashForce;
            Vector2 currentVelocity = rb.linearVelocity;
            
            // Allow some control during dash
            if (Mathf.Abs(move.x) > 0.01f)
            {
                dashVelocity.x = move.x * dashForce;
                dashDirection.x = move.x;
            }
            
            // Blend between current velocity and dash velocity
            rb.linearVelocity = Vector2.Lerp(currentVelocity, dashVelocity, dashControl);
        }
        else
        {
            // Direct velocity manipulation like the old code - much more responsive
            Vector2 newVelocity;
            newVelocity.x = move.x * moveSpeed;
            newVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = newVelocity;
        }
        
        FlipSprite();

        if (animator)
        {
            // Use direct physics values for more responsive animation
            animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
            animator.SetFloat("yVelocity", rb.linearVelocity.y);
            
            // Set isJumping parameter based on actual physics state
            bool shouldBeJumping = !trulyGrounded && Mathf.Abs(rb.linearVelocity.y) > 0.1f;
            animator.SetBool("isJumping", shouldBeJumping);
        }
    }

    void OnJumpPressed()
    {
        jumpBufferCounter = jumpBufferTime;
        TryJump();
    }
    
    void OnJumpReleased()
    {
        jumpInputReleased = true;
    }
    
    void OnDashPressed()
    {
        if (dashEnabled && dashCooldownTimer <= 0f && !isDashing)
        {
            StartDash();
        }
    }
    
    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        // Set dash invincibility
        isDashInvincible = true;
        dashInvincibilityTimer = dashInvincibilityTime;
        
        // Set dash direction based on movement mode
        if (isIsometric)
        {
            // For isometric: use full 2D movement input
            if (move.sqrMagnitude > 0.01f)
            {
                dashDirection = move.normalized;
            }
            else
            {
                // Default to last movement direction or right
                dashDirection = lastNonZeroMove.normalized;
            }
        }
        else
        {
            // For side-scroller: only horizontal movement
            if (Mathf.Abs(move.x) > 0.01f)
            {
                dashDirection = new Vector2(move.x, 0f).normalized;
            }
            else
            {
                dashDirection = new Vector2(isFacingRight ? 1f : -1f, 0f);
            }
        }
        
        // Apply dash force with more impact
        if (isIsometric)
        {
            // For isometric: apply force in dash direction, preserve Y velocity
            rb.linearVelocity = new Vector2(dashDirection.x * dashForce, dashDirection.y * dashForce);
        }
        else
        {
            // For side-scroller: only horizontal dash, preserve Y velocity
            rb.linearVelocity = new Vector2(dashDirection.x * dashForce, rb.linearVelocity.y);
        }
        
        // Play dash sound if available
        if (AudioManager.Instance) AudioManager.Instance.PlayDashSound();
        
        // Trigger dash effects
        TriggerDashEffects();
        

    }
    
    void TriggerDashEffects()
    {
        // Screen shake effect
        if (dashScreenShake > 0f)
        {
            // Try to find a camera shake component or use a simple shake
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                StartCoroutine(SimpleScreenShake(mainCam, dashScreenShake, dashDuration));
            }
        }
        
        // Set dash animation parameter (only if it exists)
        if (animator)
        {
            // Check if the "Dash" parameter exists before trying to trigger it
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "Dash" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger("Dash");
                    break;
                }
            }
        }
    }
    
    IEnumerator SimpleScreenShake(Camera cam, float intensity, float duration)
    {
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            
            cam.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cam.transform.localPosition = originalPos;
    }
    
    // Public method to check if player is invincible (useful for other systems)
    public bool IsInvincible()
    {
        return isDashInvincible;
    }
    
    void TryJump()
    {
        // Only in side-scroller mode and if jump allowed at all
        if (isIsometric || !canJump) return;

        bool groundedNow = groundCheck != null && groundCheck.IsGrounded;

        // Ground jump or coyote time jump
        if (groundedNow || coyoteTimeCounter > 0f)
        {
            DoJump();
            coyoteTimeCounter = 0f; // Use up coyote time
            jumpBufferCounter = 0f; // Use up jump buffer
            isJumping = true;
            jumpInputReleased = false;
            // Reset air jumps for the upcoming airtime
            airJumpsLeft = Mathf.Max(0, extraAirJumps);
            return;
        }

        // Air jump(s)
        if (doubleJumpEnabled && airJumpsLeft > 0)
        {
            // Optional: zero out vertical velocity for consistent second jump feel
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            DoJump();
            airJumpsLeft--;
            isJumping = true;
            jumpInputReleased = false;
        }
    }
    
    void Jump()
    {
        // Legacy method for compatibility - redirects to new system
        TryJump();
    }

    void DoJump()
    {
        // Physics-based jumping as recommended by Game Dev Beginner
        // First, cancel any existing vertical velocity for consistent jump height
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        
        // Apply upward force using Impulse mode for immediate effect
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        
        if (animator) animator.SetBool("isJumping", true);
        if (AudioManager.Instance) AudioManager.Instance.PlayJumpSound();
        

    }
    
    void ResetAnimationParameters()
    {
        // Reset animation parameters when landing
        if (animator)
        {
            animator.SetFloat("yVelocity", 0f);
            animator.SetBool("isJumping", false);
        }
    }

    void FlipSprite()
    {
        // Only flip based on input, not velocity (prevents unwanted flipping when stopping)
        if (Mathf.Abs(move.x) > 0.01f)
        {
            if ((move.x > 0f && !isFacingRight) || (move.x < 0f && isFacingRight))
            {
                isFacingRight = !isFacingRight;
                var scale = transform.localScale;
                scale.x *= -1f;
                transform.localScale = scale;
            }
        }
    }
   

    public void ExternalBounce(float force, bool resetAirJumps)
    {

        
        if (isIsometric)
        {
            return;
        }

        // Calculate initial velocity for a fixed bounce height (force = desired height)
        // v0 = sqrt(2 * g * h), where g = Physics2D.gravity.y * gravityScale (negative)
        float g = Physics2D.gravity.y * rb.gravityScale;
        float v0 = Mathf.Sqrt(2f * Mathf.Abs(g) * force);

        // Cancel existing vertical velocity and apply exact bounce velocity
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, v0);

        // Reset air jumps if requested
        if (resetAirJumps)
        {
            airJumpsLeft = Mathf.Max(0, extraAirJumps);
        }

        // Set bounce lock to prevent movement override
        isBouncing = true;
        bounceLockTimer = 0.3f; // lock movement for 0.3 seconds (increased for more reliable bounce)


        // Trigger jump animation & sound
        if (animator) animator.SetBool("isJumping", true);
        if (AudioManager.Instance) AudioManager.Instance.PlayJumpSound();
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
    
    void OnCollisionStay2D(Collision2D collision)
    {
        // Handle collision with other objects to prevent sticking
        GameObject other = collision.gameObject;
        
        // Skip if it's a ground object or if we're dashing
        if (other.CompareTag("Ground") || isDashing) return;
        
        // Calculate push direction (away from the other object)
        Vector2 pushDirection = (transform.position - other.transform.position).normalized;
        
        // Apply a gentle push force to prevent sticking
        rb.AddForce(pushDirection * collisionPushForce, ForceMode2D.Force);
        

    }
    
    private IEnumerator ReduceGravityTemporarily()
    {
        // Store original gravity scale
        float originalGravityScale = rb.gravityScale;
        
        // Reduce gravity for smoother bounce (less floaty)
        rb.gravityScale = originalGravityScale * 0.6f; // 60% of normal gravity (less floaty)
        
        // Wait for bounce to peak (shorter duration)
        yield return new WaitForSeconds(0.2f);
        
        // Gradually restore gravity
        float restoreTime = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < restoreTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / restoreTime;
            rb.gravityScale = Mathf.Lerp(originalGravityScale * 0.6f, originalGravityScale, progress);
            yield return null;
        }
        
        // Ensure gravity is fully restored
        rb.gravityScale = originalGravityScale;
    }
    

}


