using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class BlueGoat : MonoBehaviour
{
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

    private Rigidbody2D rb;
    private float actionTimer;
    private int moveDirection; // -1 left, 0 idle, 1 right
    private bool isFacingRight = true;

    private GameObject currentRider;
    private Rigidbody2D riderRB;
    private FixedJoint2D riderJoint;
    private float buckTimer;
    private Animator anim;
    private SpriteRenderer sr;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim) anim.SetBool("HasRider", false);

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        if (!saddlePoint)
            Debug.LogWarning("[BlueGoat] Assign saddlePoint in the inspector.");

        ChooseNewAction();
        ResetBuckTimer();
    }

    void Update()
    {
        actionTimer -= Time.deltaTime;
        buckTimer -= Time.deltaTime;

        if (actionTimer <= 0f) ChooseNewAction();

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
#else
        rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
#endif

        FlipSprite();

        HandleMountInput();

        if (currentRider && saddlePoint)
            currentRider.transform.position = saddlePoint.position;

        if (currentRider && buckTimer <= 0f)
        {
            Buck();
            ResetBuckTimer();
        }
    }

    void ChooseNewAction()
    {
        int choice = Random.Range(0, 3);
        if (choice == 0) moveDirection = -1;
        else if (choice == 1) moveDirection = 1;
        else moveDirection = 0;

        actionTimer = Random.Range(minActionTime, maxActionTime);
    }

    void FlipSprite()
    {
        if (moveDirection > 0 && !isFacingRight)
        {
            isFacingRight = true;
            if (sr) sr.flipX = false;
            else { var s = transform.localScale; s.x = Mathf.Abs(s.x); transform.localScale = s; }
        }
        else if (moveDirection < 0 && isFacingRight)
        {
            isFacingRight = false;
            if (sr) sr.flipX = true;
            else { var s = transform.localScale; s.x = -Mathf.Abs(s.x); transform.localScale = s; }
        }
    }

    void HandleMountInput()
    {
        if (!MountPressedThisFrame()) return;

        if (currentRider == null)
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
        // If only legacy input is enabled, map to E by default.
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
        if (currentRider != null) return;

        currentRider = rider;
        riderRB = rider.GetComponent<Rigidbody2D>();
        if (riderRB == null)
        {
            Debug.LogWarning("Rider has no Rigidbody2D; adding one for mounting.");
            riderRB = rider.AddComponent<Rigidbody2D>();
        }

        riderJoint = currentRider.AddComponent<FixedJoint2D>();
        riderJoint.connectedBody = rb;
        riderJoint.autoConfigureConnectedAnchor = false;
        riderJoint.anchor = Vector2.zero;
        riderJoint.connectedAnchor = Vector2.zero;
        riderJoint.enableCollision = false;
        riderJoint.breakForce = jointBreakForce;

        if (saddlePoint) currentRider.transform.position = saddlePoint.position;
#if UNITY_6000_0_OR_NEWER
        riderRB.linearVelocity = Vector2.zero;
#else
        riderRB.velocity = Vector2.zero;
#endif

        if (anim) anim.SetBool("HasRider", true);
    }

    public void Dismount(bool knockedOff)
    {
        if (currentRider == null) return;

        if (riderJoint) Destroy(riderJoint);

        if (knockedOff && riderRB)
        {
#if UNITY_6000_0_OR_NEWER
            riderRB.AddForce(new Vector2(Random.Range(-1f, 1f) * 3f, 4f), ForceMode2D.Impulse);
#else
            riderRB.AddForce(new Vector2(Random.Range(-1f, 1f) * 3f, 4f), ForceMode2D.Impulse);
#endif
        }

        if (anim) anim.SetBool("HasRider", false);

        currentRider = null;
        riderRB = null;
        riderJoint = null;
    }

    void OnDisable()
    {
        if (anim) anim.SetBool("HasRider", false);
    }

    void ResetBuckTimer()
    {
        buckTimer = Random.Range(minBuckInterval, maxBuckInterval);
    }

    void Buck()
    {
#if UNITY_6000_0_OR_NEWER
        rb.AddForce(Vector2.up * buckUpwardImpulse, ForceMode2D.Impulse);
#else
        rb.AddForce(Vector2.up * buckUpwardImpulse, ForceMode2D.Impulse);
#endif

        float side = Random.value < 0.5f ? -1f : 1f;
#if UNITY_6000_0_OR_NEWER
        rb.AddForce(new Vector2(side * buckLateralImpulse, 0f), ForceMode2D.Impulse);
#else
        rb.AddForce(new Vector2(side * buckLateralImpulse, 0f), ForceMode2D.Impulse);
#endif

        if (riderRB)
        {
#if UNITY_6000_0_OR_NEWER
            riderRB.AddForce(new Vector2(side * buckLateralImpulse, buckUpwardImpulse * 0.75f), ForceMode2D.Impulse);
#else
            riderRB.AddForce(new Vector2(side * buckLateralImpulse, buckUpwardImpulse * 0.75f), ForceMode2D.Impulse);
#endif
        }

        if (Random.value < forcedKnockoffChance)
        {
            Dismount(true);
        }
    }
}
