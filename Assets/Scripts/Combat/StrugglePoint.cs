using UnityEngine;

/// <summary>
/// Dynamically positions itself at the midpoint between hero and king characters.
/// Perfect for struggle particles, clash effects, and other contact-point visuals.
/// </summary>
public class StrugglePoint : MonoBehaviour
{
    [Header("Character References")]
    [Tooltip("The hero character transform.")]
    public Transform hero;
    [Tooltip("The king character transform.")]
    public Transform king;
    
    [Header("Positioning")]
    [Tooltip("Offset from the calculated midpoint (useful for fine-tuning).")]
    public Vector3 positionOffset = Vector3.zero;
    [Tooltip("How smoothly to follow the midpoint (0 = instant, higher = smoother).")]
    public float smoothing = 0f;
    [Tooltip("If true, also rotates to face the direction between characters.")]
    public bool faceDirection = false;
    
    [Header("Auto-Find References")]
    [Tooltip("If true, will try to find BossMashDuel and get character references from it.")]
    public bool autoFindCharacters = true;
    
    Vector3 _targetPosition;
    
    void Start()
    {
        // Auto-find character references if enabled
        if (autoFindCharacters && (!hero || !king))
        {
            var duel = FindFirstObjectByType<BossMashDuel>();
            if (duel)
            {
                if (!hero) hero = duel.hero;
                if (!king) king = duel.king;
                
                if (hero && king)
                {
                    Debug.Log($"[StrugglePoint] Auto-found characters: {hero.name} and {king.name}");
                }
            }
        }
        
        // Initialize position
        if (hero && king)
        {
            _targetPosition = CalculateMidpoint();
            transform.position = _targetPosition;
        }
    }
    
    void Update()
    {
        if (!hero || !king) return;
        
        // Calculate target position
        _targetPosition = CalculateMidpoint();
        
        // Apply position with optional smoothing
        if (smoothing > 0f)
        {
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * smoothing);
        }
        else
        {
            transform.position = _targetPosition;
        }
        
        // Optional rotation to face the direction between characters
        if (faceDirection)
        {
            Vector3 direction = (king.position - hero.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    
    Vector3 CalculateMidpoint()
    {
        Vector3 midpoint = Vector3.Lerp(hero.position, king.position, 0.5f);
        return midpoint + positionOffset;
    }
    
    /// <summary>
    /// Get the distance between the two characters.
    /// </summary>
    public float GetCharacterDistance()
    {
        if (!hero || !king) return 0f;
        return Vector3.Distance(hero.position, king.position);
    }
    
    /// <summary>
    /// Get the direction from hero to king.
    /// </summary>
    public Vector3 GetDirectionHeroToKing()
    {
        if (!hero || !king) return Vector3.zero;
        return (king.position - hero.position).normalized;
    }
    
    void OnDrawGizmos()
    {
        if (!hero || !king) return;
        
        // Draw line between characters
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(hero.position, king.position);
        
        // Draw midpoint
        Gizmos.color = Color.red;
        Vector3 midpoint = CalculateMidpoint();
        Gizmos.DrawWireSphere(midpoint, 0.1f);
        
        // Draw position offset if any
        if (positionOffset != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(Vector3.Lerp(hero.position, king.position, 0.5f), midpoint);
        }
    }
}
