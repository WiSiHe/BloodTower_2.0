using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// A singleton manager for spawning and managing floating text instances.
/// Handles object pooling for performance and provides easy-to-use static methods.
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    [Header("Floating Text Prefab")]
    [SerializeField] private GameObject _floatingTextPrefab;
    
    [Header("Pool Settings")]
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private int _maxPoolSize = 50;
    
    [Header("Default Settings")]
    [SerializeField] private FloatingText.AnimationType _defaultAnimationType = FloatingText.AnimationType.RiseAndFade;
    [SerializeField] private float _defaultDuration = 2f;
    [SerializeField] private Color _defaultColor = Color.white;
    
    private static FloatingTextManager _instance;
    private Queue<FloatingText> _textPool = new Queue<FloatingText>();
    private List<FloatingText> _activeTexts = new List<FloatingText>();
    
    public static FloatingTextManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<FloatingTextManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("FloatingTextManager");
                    _instance = go.AddComponent<FloatingTextManager>();
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePool()
    {
        if (_floatingTextPrefab == null)
        {
            Debug.LogError("[FloatingTextManager] No floating text prefab assigned!");
            return;
        }
        
        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewTextInstance();
        }
    }
    
    private FloatingText CreateNewTextInstance()
    {
        GameObject instance = Instantiate(_floatingTextPrefab, transform);
        FloatingText floatingText = instance.GetComponent<FloatingText>();
        
        if (floatingText == null)
        {
            Debug.LogError("[FloatingTextManager] Floating text prefab doesn't have FloatingText component!");
            Destroy(instance);
            return null;
        }
        
        instance.SetActive(false);
        _textPool.Enqueue(floatingText);
        return floatingText;
    }
    
    private FloatingText GetTextInstance()
    {
        FloatingText instance = null;
        
        if (_textPool.Count > 0)
        {
            instance = _textPool.Dequeue();
        }
        else if (_activeTexts.Count < _maxPoolSize)
        {
            instance = CreateNewTextInstance();
            if (instance != null)
            {
                _textPool.Dequeue(); // Remove from pool since we're about to use it
            }
        }
        else
        {
            // Reuse the oldest active text
            instance = _activeTexts[0];
            _activeTexts.RemoveAt(0);
            instance.Stop();
        }
        
        if (instance != null)
        {
            _activeTexts.Add(instance);
        }
        
        return instance;
    }
    
    private void ReturnToPool(FloatingText floatingText)
    {
        if (floatingText == null) return;
        
        _activeTexts.Remove(floatingText);
        floatingText.Stop();
        _textPool.Enqueue(floatingText);
    }
    
    /// <summary>
    /// Spawn floating text at a world position
    /// </summary>
    public static FloatingText SpawnText(Vector3 worldPosition, string message, 
        FloatingText.AnimationType? animationType = null, float? duration = null, Color? color = null)
    {
        return Instance.SpawnTextInternal(worldPosition, message, animationType, duration, color);
    }
    
    /// <summary>
    /// Spawn floating text above a transform
    /// </summary>
    public static FloatingText SpawnText(Transform target, string message, Vector3 offset = default,
        FloatingText.AnimationType? animationType = null, float? duration = null, Color? color = null)
    {
        Vector3 position = target.position + offset;
        return Instance.SpawnTextInternal(position, message, animationType, duration, color);
    }
    
    /// <summary>
    /// Spawn floating text at screen position (for UI elements)
    /// </summary>
    public static FloatingText SpawnScreenText(Vector2 screenPosition, string message,
        FloatingText.AnimationType? animationType = null, float? duration = null, Color? color = null)
    {
        return Instance.SpawnScreenTextInternal(screenPosition, message, animationType, duration, color);
    }
    
    private FloatingText SpawnTextInternal(Vector3 worldPosition, string message,
        FloatingText.AnimationType? animationType, float? duration, Color? color)
    {
        FloatingText instance = GetTextInstance();
        if (instance == null) return null;
        
        // Set position
        instance.transform.position = worldPosition;
        
        // Set color
        Color textColor = color ?? _defaultColor;
        if (instance.GetComponentInChildren<TMP_Text>())
        {
            instance.GetComponentInChildren<TMP_Text>().color = textColor;
        }
        
        // Show with animation
        instance.Show(message, animationType ?? _defaultAnimationType, duration ?? _defaultDuration);
        
        // Auto-return to pool after animation
        StartCoroutine(ReturnToPoolAfterDelay(instance, duration ?? _defaultDuration + 1f));
        
        return instance;
    }
    
    private FloatingText SpawnScreenTextInternal(Vector2 screenPosition, string message,
        FloatingText.AnimationType? animationType, float? duration, Color? color)
    {
        FloatingText instance = GetTextInstance();
        if (instance == null) return null;
        
        // Convert screen position to world position
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            instance.transform.position = worldPos;
        }
        else
        {
            instance.transform.position = screenPosition;
        }
        
        // Set color
        Color textColor = color ?? _defaultColor;
        if (instance.GetComponentInChildren<TMP_Text>())
        {
            instance.GetComponentInChildren<TMP_Text>().color = textColor;
        }
        
        // Show with animation
        instance.Show(message, animationType ?? _defaultAnimationType, duration ?? _defaultDuration);
        
        // Auto-return to pool after animation
        StartCoroutine(ReturnToPoolAfterDelay(instance, duration ?? _defaultDuration + 1f));
        
        return instance;
    }
    
    private System.Collections.IEnumerator ReturnToPoolAfterDelay(FloatingText floatingText, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(floatingText);
    }
    
    /// <summary>
    /// Clear all active floating texts
    /// </summary>
    public static void ClearAll()
    {
        Instance.ClearAllInternal();
    }
    
    private void ClearAllInternal()
    {
        for (int i = _activeTexts.Count - 1; i >= 0; i--)
        {
            ReturnToPool(_activeTexts[i]);
        }
    }
    
    /// <summary>
    /// Get the number of active floating texts
    /// </summary>
    public static int GetActiveCount()
    {
        return Instance._activeTexts.Count;
    }
    
    /// <summary>
    /// Get the number of available pooled texts
    /// </summary>
    public static int GetPooledCount()
    {
        return Instance._textPool.Count;
    }
    
    // Convenience methods for common use cases
    public static FloatingText ShowDamage(Vector3 position, int damage, Color? color = null)
    {
        return SpawnText(position, $"-{damage}", FloatingText.AnimationType.RiseAndFade, 1.5f, color ?? Color.red);
    }
    
    public static FloatingText ShowHeal(Vector3 position, int heal, Color? color = null)
    {
        return SpawnText(position, $"+{heal}", FloatingText.AnimationType.RiseAndFade, 1.5f, color ?? Color.green);
    }
    
    public static FloatingText ShowScore(Vector3 position, int score, Color? color = null)
    {
        return SpawnText(position, $"+{score}", FloatingText.AnimationType.BounceAndFade, 2f, color ?? Color.yellow);
    }
    
    public static FloatingText ShowMessage(Vector3 position, string message, Color? color = null)
    {
        return SpawnText(position, message, FloatingText.AnimationType.FloatUp, 3f, color ?? Color.white);
    }
    
    public static FloatingText ShowCritical(Vector3 position, string message, Color? color = null)
    {
        return SpawnText(position, message, FloatingText.AnimationType.ScaleAndFade, 2.5f, color ?? Color.red);
    }
}
