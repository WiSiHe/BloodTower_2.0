// Assets/Scripts/Space_Invaders/MonsterMover.cs
using UnityEngine;

/// <summary>
/// Dynamic monster movement controller with varied patterns, speed changes, and visual effects
/// </summary>
public class MonsterMover : MonoBehaviour
{
    [Header("Movement Patterns")]
    public MovementPattern movementPattern = MovementPattern.StraightDown;
    public float minSpeed = 0.8f;
    public float maxSpeed = 1.6f;
    public float speedVariation = 0.3f;
    public float speedChangeInterval = 2.0f;

    [Header("Wobble Movement")]
    public bool wobble = false;
    public float wobbleAmplitude = 0.25f;
    public float wobbleFrequency = 1.2f;
    public float wobbleVariation = 0.5f;

    [Header("Spiral Movement")]
    public float spiralRadius = 1.0f;
    public float spiralSpeed = 2.0f;
    public float spiralTightness = 0.1f;

    [Header("Zigzag Movement")]
    public float zigzagAmplitude = 1.5f;
    public float zigzagFrequency = 1.0f;
    public float zigzagPhase = 0f;

    [Header("Direction Changes")]
    public bool canChangeDirection = true;
    public float directionChangeChance = 0.1f;
    public float directionChangeInterval = 3.0f;

    [Header("Visual Effects")]
    public bool enableTrail = false;
    public Color trailColor = Color.red;
    public float trailTime = 0.5f;

    [Header("Damage Settings")]
    public bool canDamagePlayer = true;
    public int damageAmount = 1;
    public float damageCooldown = 1.0f;
    public bool destroyOnPlayerHit = true;

    [Header("Auto-despawn")]
    public float extraDespawnBelow = 3.0f;

    private float _currentSpeed;
    private float _baseX;
    private float _baseY;
    private float _spiralAngle;
    private float _lastSpeedChange;
    private float _lastDirectionChange;
    private float _lastDamageTime;
    private Vector3 _direction = Vector3.down;
    private Camera _cam;
    private TrailRenderer _trail;

    public enum MovementPattern
    {
        StraightDown,
        Wobble,
        Spiral,
        Zigzag,
        Random
    }

    void Start()
    {
        InitializeMovement();
        SetupVisualEffects();
        _cam = Camera.main;
    }

    void Update()
    {
        UpdateMovement();
        CheckDespawn();
    }

    private void InitializeMovement()
    {
        _currentSpeed = Random.Range(minSpeed, maxSpeed);
        _baseX = transform.position.x;
        _baseY = transform.position.y;
        _spiralAngle = Random.Range(0f, 360f);
        _lastSpeedChange = Time.time;
        _lastDirectionChange = Time.time;
        _lastDamageTime = 0f;

        // Randomize some parameters for variety
        if (wobble)
        {
            wobbleFrequency += Random.Range(-wobbleVariation, wobbleVariation);
        }
    }

    private void SetupVisualEffects()
    {
        if (enableTrail)
        {
            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.material = new Material(Shader.Find("Sprites/Default"));
            
            // Create gradient for trail color
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(trailColor, 0.0f), new GradientColorKey(trailColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            _trail.colorGradient = gradient;
            
            _trail.time = trailTime;
            _trail.startWidth = 0.1f;
            _trail.endWidth = 0.01f;
        }
    }

    private void UpdateMovement()
    {
        // Update speed dynamically
        if (Time.time - _lastSpeedChange > speedChangeInterval)
        {
            float speedChange = Random.Range(-speedVariation, speedVariation);
            _currentSpeed = Mathf.Clamp(_currentSpeed + speedChange, minSpeed, maxSpeed);
            _lastSpeedChange = Time.time;
        }

        // Change direction occasionally
        if (canChangeDirection && Time.time - _lastDirectionChange > directionChangeInterval)
        {
            if (Random.value < directionChangeChance)
            {
                ChangeDirection();
                _lastDirectionChange = Time.time;
            }
        }

        // Apply movement based on pattern
        Vector3 movement = CalculateMovement();
        transform.position += movement * Time.deltaTime;
    }

    private Vector3 CalculateMovement()
    {
        Vector3 movement = Vector3.zero;

        switch (movementPattern)
        {
            case MovementPattern.StraightDown:
                movement = _direction * _currentSpeed;
                break;

            case MovementPattern.Wobble:
                movement = _direction * _currentSpeed;
                ApplyWobble();
                break;

            case MovementPattern.Spiral:
                ApplySpiralMovement();
                movement = _direction * _currentSpeed;
                break;

            case MovementPattern.Zigzag:
                ApplyZigzagMovement();
                movement = _direction * _currentSpeed;
                break;

            case MovementPattern.Random:
                ApplyRandomMovement();
                movement = _direction * _currentSpeed;
                break;
        }

        return movement;
    }

    private void ApplyWobble()
    {
        if (wobble)
        {
            float x = _baseX + Mathf.Sin(Time.time * wobbleFrequency) * wobbleAmplitude;
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }
    }

    private void ApplySpiralMovement()
    {
        _spiralAngle += spiralSpeed * Time.deltaTime;
        float spiralX = _baseX + Mathf.Cos(_spiralAngle) * spiralRadius * spiralTightness;
        transform.position = new Vector3(spiralX, transform.position.y, transform.position.z);
    }

    private void ApplyZigzagMovement()
    {
        float zigzagX = _baseX + Mathf.Sin((Time.time + zigzagPhase) * zigzagFrequency) * zigzagAmplitude;
        transform.position = new Vector3(zigzagX, transform.position.y, transform.position.z);
    }

    private void ApplyRandomMovement()
    {
        // Randomly switch between different movement patterns
        if (Random.value < 0.01f) // 1% chance per frame
        {
            int randomPattern = Random.Range(0, 4);
            switch (randomPattern)
            {
                case 0:
                    ApplyWobble();
                    break;
                case 1:
                    ApplySpiralMovement();
                    break;
                case 2:
                    ApplyZigzagMovement();
                    break;
                // case 3: straight down (no additional movement)
            }
        }
    }

    private void ChangeDirection()
    {
        // Add some horizontal movement occasionally
        if (Random.value < 0.5f)
        {
            _direction = new Vector3(Random.Range(-0.5f, 0.5f), -1f, 0f).normalized;
        }
        else
        {
            _direction = Vector3.down;
        }
    }

    private void CheckDespawn()
    {
        if (_cam && _cam.orthographic)
        {
            float bottomY = _cam.transform.position.y - _cam.orthographicSize - extraDespawnBelow;
            if (transform.position.y < bottomY)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            // Add some visual feedback before destroying
            if (_trail)
            {
                _trail.time = 0.1f; // Shorten trail on impact
            }
            Destroy(gameObject);
        }
        else if (collision.collider.CompareTag("Player"))
        {
            HandlePlayerCollision();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HandlePlayerCollision();
        }
    }

    private void HandlePlayerCollision()
    {
        if (!canDamagePlayer) return;
        
        // Check damage cooldown
        if (Time.time - _lastDamageTime < damageCooldown) return;
        
        _lastDamageTime = Time.time;
        
        // Damage the player through GameSession
        if (GameSession.Instance != null)
        {
            GameSession.Instance.Damage(damageAmount);
            Debug.Log($"[MonsterMover] Player hit! Damage: {damageAmount}");
        }
        
        // Visual feedback
        if (_trail)
        {
            _trail.time = 0.1f; // Shorten trail on impact
        }
        
        // Destroy monster if configured to do so
        if (destroyOnPlayerHit)
        {
            Destroy(gameObject);
        }
    }

    // Public methods for external control
    public void SetMovementPattern(MovementPattern pattern)
    {
        movementPattern = pattern;
    }

    public void SetSpeed(float newSpeed)
    {
        _currentSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);
    }

    public void BoostSpeed(float multiplier)
    {
        _currentSpeed *= multiplier;
        _currentSpeed = Mathf.Clamp(_currentSpeed, minSpeed, maxSpeed * 2f);
    }

    // Damage-related public methods
    public void SetDamageAmount(int amount)
    {
        damageAmount = Mathf.Max(1, amount);
    }

    public void SetCanDamagePlayer(bool canDamage)
    {
        canDamagePlayer = canDamage;
    }

    public void SetDestroyOnHit(bool destroy)
    {
        destroyOnPlayerHit = destroy;
    }
}
