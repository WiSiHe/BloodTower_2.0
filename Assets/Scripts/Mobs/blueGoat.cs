using UnityEngine;

public class BlueGoat : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;           // How fast the goat moves
    public float minActionTime = 1f;       // Minimum time per action
    public float maxActionTime = 3f;       // Maximum time per action

    private Rigidbody2D rb;
    private float actionTimer;
    private int moveDirection;             // -1 = left, 0 = idle, 1 = right

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
    }

    void ChooseNewAction()
    {
        // Pick a random action: -1, 0, or 1
        int choice = Random.Range(0, 3); // 0,1,2
        if (choice == 0) moveDirection = -1;  // left
        else if (choice == 1) moveDirection = 1;  // right
        else moveDirection = 0;  // idle

        // Pick a random duration
        actionTimer = Random.Range(minActionTime, maxActionTime);
    }
}
