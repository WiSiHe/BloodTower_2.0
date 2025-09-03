using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// A flexible floating text component that can display text with various animations.
/// Supports world space and screen space positioning.
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasGroup))]
public class FloatingText : MonoBehaviour
{
    [Header("Text Component")]
    [SerializeField] private TMP_Text _text;
    
    [Header("Animation Settings")]
    [SerializeField] private AnimationType _animationType = AnimationType.RiseAndFade;
    [SerializeField] private float _duration = 2f;
    [SerializeField] private float _riseDistance = 1f;
    [SerializeField] private float _bounceHeight = 0.5f;
    [SerializeField] private float _bounceDuration = 0.3f;
    
    [Header("Fade Settings")]
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    [SerializeField] private float _holdDuration = 1f;
    
    [Header("Scale Settings")]
    [SerializeField] private bool _useScaleAnimation = true;
    [SerializeField] private float _scaleInDuration = 0.2f;
    [SerializeField] private float _scaleOutDuration = 0.3f;
    [SerializeField] private Vector3 _maxScale = Vector3.one * 1.2f;
    
    [Header("Color Settings")]
    [SerializeField] private bool _useColorAnimation = false;
    [SerializeField] private Color _startColor = Color.white;
    [SerializeField] private Color _endColor = Color.red;
    
    [Header("World Space Settings")]
    [SerializeField] private bool _faceCamera = true;
    [SerializeField] private bool _useWorldSpace = true;
    
    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Vector3 _startPosition;
    private Vector3 _startScale;
    private Color _originalColor;
    private Coroutine _animationCoroutine;
    
    public enum AnimationType
    {
        RiseAndFade,
        BounceAndFade,
        ScaleAndFade,
        StaticFade,
        FloatUp
    }
    
    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();
        
        if (_canvas)
        {
            _canvas.renderMode = _useWorldSpace ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
        }
        
        if (_text == null)
        {
            _text = GetComponentInChildren<TMP_Text>();
        }
        
        _startPosition = transform.localPosition;
        _startScale = transform.localScale;
        
        if (_text)
        {
            _originalColor = _text.color;
        }
        
        // Start hidden
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
    
    private void Update()
    {
        if (_faceCamera && _useWorldSpace)
        {
            FaceCamera();
        }
    }
    
    /// <summary>
    /// Show floating text with the specified message and optional custom settings
    /// </summary>
    public void Show(string message, AnimationType? animationType = null, float? duration = null)
    {
        if (_text) _text.text = message;
        
        if (animationType.HasValue) _animationType = animationType.Value;
        if (duration.HasValue) _duration = duration.Value;
        
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        
        gameObject.SetActive(true);
        _animationCoroutine = StartCoroutine(PlayAnimation());
    }
    
    /// <summary>
    /// Show floating text with custom color
    /// </summary>
    public void Show(string message, Color color, AnimationType? animationType = null, float? duration = null)
    {
        if (_text) _text.color = color;
        Show(message, animationType, duration);
    }
    
    /// <summary>
    /// Show floating text with custom position offset
    /// </summary>
    public void Show(string message, Vector3 positionOffset, AnimationType? animationType = null, float? duration = null)
    {
        _startPosition = transform.localPosition + positionOffset;
        Show(message, animationType, duration);
    }
    
    private IEnumerator PlayAnimation()
    {
        // Reset state
        transform.localPosition = _startPosition;
        transform.localScale = _startScale;
        _canvasGroup.alpha = 0f;
        
        if (_text && _useColorAnimation)
        {
            _text.color = _startColor;
        }
        
        // Fade in
        yield return StartCoroutine(FadeIn());
        
        // Hold
        yield return new WaitForSeconds(_holdDuration);
        
        // Main animation
        yield return StartCoroutine(PlayMainAnimation());
        
        // Fade out
        yield return StartCoroutine(FadeOut());
        
        // Cleanup
        gameObject.SetActive(false);
        _animationCoroutine = null;
    }
    
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        
        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fadeInDuration;
            
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            
            if (_useScaleAnimation)
            {
                transform.localScale = Vector3.Lerp(Vector3.zero, _startScale, t);
            }
            
            yield return null;
        }
        
        _canvasGroup.alpha = 1f;
        if (_useScaleAnimation)
        {
            transform.localScale = _startScale;
        }
    }
    
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        Vector3 currentScale = transform.localScale;
        
        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fadeOutDuration;
            
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            
            if (_useScaleAnimation)
            {
                transform.localScale = Vector3.Lerp(currentScale, Vector3.zero, t);
            }
            
            if (_useColorAnimation && _text)
            {
                _text.color = Color.Lerp(_startColor, _endColor, t);
            }
            
            yield return null;
        }
        
        _canvasGroup.alpha = 0f;
        if (_useScaleAnimation)
        {
            transform.localScale = Vector3.zero;
        }
    }
    
    private IEnumerator PlayMainAnimation()
    {
        switch (_animationType)
        {
            case AnimationType.RiseAndFade:
                yield return StartCoroutine(RiseAnimation());
                break;
            case AnimationType.BounceAndFade:
                yield return StartCoroutine(BounceAnimation());
                break;
            case AnimationType.ScaleAndFade:
                yield return StartCoroutine(ScaleAnimation());
                break;
            case AnimationType.FloatUp:
                yield return StartCoroutine(FloatUpAnimation());
                break;
            case AnimationType.StaticFade:
                // Just wait for the duration
                yield return new WaitForSeconds(_duration - _fadeInDuration - _fadeOutDuration - _holdDuration);
                break;
        }
    }
    
    private IEnumerator RiseAnimation()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * _riseDistance;
        
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration;
            
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            
            yield return null;
        }
    }
    
    private IEnumerator BounceAnimation()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;
        Vector3 peakPos = startPos + Vector3.up * _bounceHeight;
        
        while (elapsed < _bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _bounceDuration;
            
            // Bounce up
            transform.localPosition = Vector3.Lerp(startPos, peakPos, t);
            
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < _bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _bounceDuration;
            
            // Bounce down
            transform.localPosition = Vector3.Lerp(peakPos, startPos, t);
            
            yield return null;
        }
    }
    
    private IEnumerator ScaleAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = _startScale;
        
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration;
            
            // Pulse scale
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.2f;
            transform.localScale = startScale * scaleMultiplier;
            
            yield return null;
        }
        
        transform.localScale = startScale;
    }
    
    private IEnumerator FloatUpAnimation()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * _riseDistance;
        
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration;
            
            // Smooth float up with slight sine wave
            Vector3 basePos = Vector3.Lerp(startPos, endPos, t);
            Vector3 waveOffset = Vector3.right * Mathf.Sin(t * Mathf.PI * 2f) * 0.1f;
            transform.localPosition = basePos + waveOffset;
            
            yield return null;
        }
    }
    
    private void FaceCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
    }
    
    /// <summary>
    /// Stop the current animation and hide the text
    /// </summary>
    public void Stop()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        
        gameObject.SetActive(false);
    }
}
