using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class BlueGoat : MonoBehaviour
{
    [Header("Space")]
    [Tooltip("If true: top-down isometric wandering (NE/NW/SE/SW). If false: 2D side-scroller left/right only.")]
    public bool isIsometricMovement = false;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float minActionTime = 1f;
    public float maxActionTime = 3f;

    [Header("Riding / Mounting")]
    public Transform saddlePoint;
    public float mountRange = 1.2f;
    public string playerTag = "Player";

    [Header("Bucking")]
    public float minBuckInterval = 3f;
    public float maxBuckInterval = 7f;
    public float buckUpwardImpulse = 6f;
    public float buckLateralImpulse = 2f;
    public float forcedKnockoffChance = 0.3f;
    public float jointBreakForce = 150f;
    
    [Header("Collision")]
    [Tooltip("Force applied to push away colliding players to prevent sticking")]
    public float pushForce = 2f;
    [Tooltip("Layers that should be pushed away (e.g., Player layer)")]
    public LayerMask pushLayers = -1;

    private Rigidbody2D rb;
    private float actionTimer;

    // 2D side-scroller direction: -1 left, 0 idle, 1 right
    private int moveDirection = 0;

    // Isometric top-down direction (NE/NW/SE/SW or idle)
    private Vector2 isoDirection = Vector2.zero;

    private bool isFacingRight = true;

    private GameObject _currentRider;
    private Rigidbody2D _riderRB;
    private FixedJoint2D _riderJoint;
    private float _buckTimer;
    private Animator _anim;
    private SpriteRenderer _sr;

    void Start()
    {
        _anim = GetComponent<Animator>();
        if (_anim) _anim.SetBool("HasRider", false);

        rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();

        if (!saddlePoint)
            Debug.LogWarning("[BlueGoat] Assign saddlePoint in the inspector.");

        ChooseNewAction();
        ResetBuckTimer();

        // Tip: In isometric mode, set Rigidbody2D.gravityScale = 0 in the inspector to avoid gravity pulling on Y.
        // In side-scroller mode, keep your normal gravity for jumping/bucking feel.
    }

    void Update()
    {
        actionTimer -= Time.deltaTime;
        _buckTimer -= Time.deltaTime;

        if (actionTimer <= 0f) ChooseNewAction();

        // Compute target velocity based on selected space
        Vector2 targetVelocity;

        if (isIsometricMovement)
        {
            // Top-down: drive both X and Y (normalize to keep speed consistent on diagonals)
            targetVelocity = isoDirection * moveSpeed;

            // Anti-sliding: force stop when idle
            if (isoDirection.sqrMagnitude < 0.01f)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.linearVelocity = targetVelocity;
            }
        }
        else
        {
            // Side-scroller: only X movement, keep existing Y velocity (gravity/jumps/buck)
            // Anti-sliding: force stop when idle
            if (moveDirection == 0)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
            }
        }

        FlipSprite(isIsometricMovement ? isoDirection.x : moveDirection);

        HandleMountInput();

        if (_currentRider && saddlePoint)
            _currentRider.transform.position = saddlePoint.position;

        if (_currentRider && _buckTimer <= 0f)
        {
            Buck();
            ResetBuckTimer();
        }
    }

    void ChooseNewAction()
    {
        if (isIsometricMovement)
        {
            // 0..4 -> NE, NW, SE, SW, Idle
            int choice = Random.Range(0, 5);
            switch (choice)
            {
                case 0: isoDirection = new Vector2(1f, 1f).normalized; break;   // NE
                case 1: isoDirection = new Vector2(-1f, 1f).normalized; break;  // NW
                case 2: isoDirection = new Vector2(1f, -1f).normalized; break;  // SE
                case 3: isoDirection = new Vector2(-1f, -1f).normalized; break; // SW
                default: isoDirection = Vector2.zero; break;                     // Idle
            }
            moveDirection = Mathf.RoundToInt(Mathf.Sign(isoDirection.x));
        }
        else
        {
            // Side-scroller: -1, 0, 1
            int choice = Random.Range(0, 3);
            if (choice == 0) moveDirection = -1;
            else if (choice == 1) moveDirection = 1;
            else moveDirection = 0;

            isoDirection = new Vector2(moveDirection, 0f);
        }

        actionTimer = Random.Range(minActionTime, maxActionTime);
    }

    void FlipSprite(float xDir)
    {
        if (xDir > 0.01f && !isFacingRight)
        {
            isFacingRight = true;
            if (_sr) _sr.flipX = false;
            else { var s = transform.localScale; s.x = Mathf.Abs(s.x); transform.localScale = s; }
        }
        else if (xDir < -0.01f && isFacingRight)
        {
            isFacingRight = false;
            if (_sr) _sr.flipX = true;
            else { var s = transform.localScale; s.x = -Mathf.Abs(s.x); transform.localScale = s; }
        }
    }

    void HandleMountInput()
    {
        if (!MountPressedThisFrame()) return;

        if (_currentRider == null)
        {
            GameObject player = FindClosestPlayerWithinRange();
            if (player != null) Mount(player);
            else Debug.Log("[BlueGoat] Mount key pressed but no player in range or wrong tag.");
        }
        else
        {
            Dismount(false);
        }
    }

    bool MountPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    GameObject FindClosestPlayerWithinRange()
    {
        if (!saddlePoint) return null;

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        GameObject best = null;
        float bestDist = mountRange;

        foreach (var p in players)
        {
            float d = Vector2.Distance(p.transform.position, saddlePoint.position);
            if (d <= bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }

    public void Mount(GameObject rider)
    {
        if (_currentRider != null) return;

        _currentRider = rider;
        _riderRB = rider.GetComponent<Rigidbody2D>();
        if (_riderRB == null)
        {
            Debug.LogWarning("Rider has no Rigidbody2D; adding one for mounting.");
            _riderRB = rider.AddComponent<Rigidbody2D>();
        }

        _riderJoint = _currentRider.AddComponent<FixedJoint2D>();
        _riderJoint.connectedBody = rb;
        _riderJoint.autoConfigureConnectedAnchor = false;
        _riderJoint.anchor = Vector2.zero;
        _riderJoint.connectedAnchor = Vector2.zero;
        _riderJoint.enableCollision = false;
        _riderJoint.breakForce = jointBreakForce;

        if (saddlePoint) _currentRider.transform.position = saddlePoint.position;
        _riderRB.linearVelocity = Vector2.zero;

        if (_anim) _anim.SetBool("HasRider", true);
    }

    public void Dismount(bool knockedOff)
    {
        if (_currentRider == null) return;

        if (_riderJoint) Destroy(_riderJoint);

        if (knockedOff && _riderRB)
        {
            _riderRB.AddForce(new Vector2(Random.Range(-1f, 1f) * 3f, 4f), ForceMode2D.Impulse);
        }

        if (_anim) _anim.SetBool("HasRider", false);

        _currentRider = null;
        _riderRB = null;
        _riderJoint = null;
    }

    void OnDisable()
    {
        if (_anim) _anim.SetBool("HasRider", false);
    }

    void ResetBuckTimer()
    {
        _buckTimer = Random.Range(minBuckInterval, maxBuckInterval);
    }

    void Buck()
    {
        rb.AddForce(Vector2.up * buckUpwardImpulse, ForceMode2D.Impulse);

        float side = Random.value < 0.5f ? -1f : 1f;
        rb.AddForce(new Vector2(side * buckLateralImpulse, 0f), ForceMode2D.Impulse);

        if (_riderRB)
        {
            _riderRB.AddForce(new Vector2(side * buckLateralImpulse, buckUpwardImpulse * 0.75f), ForceMode2D.Impulse);
        }

        if (Random.value < forcedKnockoffChance)
        {
            Dismount(true);
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        // Only push if not mounted and collision is with a pushable layer
        if (_currentRider != null) return;
        
        GameObject other = collision.gameObject;
        if (((1 << other.layer) & pushLayers) == 0) return;
        
        // Don't push the current rider
        if (other == _currentRider) return;
        
        // Calculate push direction (away from goat)
        Vector2 pushDirection = (other.transform.position - transform.position).normalized;
        
        // Apply push force to the other object
        Rigidbody2D otherRB = other.GetComponent<Rigidbody2D>();
        if (otherRB != null)
        {
            otherRB.AddForce(pushDirection * pushForce, ForceMode2D.Force);
        }
    }
}
