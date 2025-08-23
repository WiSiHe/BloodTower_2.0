using UnityEngine;

public class BlueGoat : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float minActionTime = 1f;
    public float maxActionTime = 3f;

    private Rigidbody2D rb;
    private float actionTimer;
    private int moveDirection;           // -1 = left, 0 = idle, 1 = right
    private bool isFacingRight = true;   // start facing right

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ChooseNewAction();
    }

    void Update()
    {
        // Countdown timer
        actionTimer -= Time.deltaTime;

        // If time is up, pick a new random action
        if (actionTimer <= 0f)
        {
            ChooseNewAction();
        }

        // Apply movement (x velocity only)
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        // Flip sprite to face movement direction (ignore when idle)
        FlipSprite();
    }

    void ChooseNewAction()
    {
        // Pick a random action: -1, 0, or 1
        int choice = Random.Range(0, 3); // 0,1,2
        if (choice == 0) moveDirection = -1;   // left
        else if (choice == 1) moveDirection = 1;   // right
        else moveDirection = 0;   // idle

        // Pick a random duration
        actionTimer = Random.Range(minActionTime, maxActionTime);
    }

    void FlipSprite()
    {
        // Only flip when actually moving left/right
        if (moveDirection > 0 && !isFacingRight || moveDirection < 0 && isFacingRight)
        {
            isFacingRight = !isFacingRight;
            var scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }
    }
}
