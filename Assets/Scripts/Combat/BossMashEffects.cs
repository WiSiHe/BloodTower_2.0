using UnityEngine;

/// <summary>
/// Provides visual and audio feedback for the boss mash duel.
/// Attach to the same GameObject as BossMashDuel or reference it.
/// </summary>
public class BossMashEffects : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The boss mash duel to get feedback data from.")]
    public BossMashDuel duel;
    
    [Header("Screen Shake")]
    [Tooltip("Maximum screen shake intensity.")]
    public float maxShakeIntensity = 0.5f;
    [Tooltip("Camera to shake (leave empty to find main camera).")]
    public Camera shakeCamera;
    
    [Header("Audio")]
    [Tooltip("Sound played when player gets instant push bonus.")]
    public AudioSource instantPushSound;
    [Tooltip("Sound played when momentum reaches maximum.")]
    public AudioSource momentumMaxSound;
    [Tooltip("Pitch range for struggle intensity sounds.")]
    public Vector2 strugglePitchRange = new Vector2(0.8f, 1.2f);
    
    [Header("Particle Systems")]
    [Tooltip("Particle system for momentum buildup effect (attached to hero).")]
    public ParticleSystem momentumParticles;
    [Tooltip("Particle system for struggle intensity (should be on StrugglePoint GameObject).")]
    public ParticleSystem struggleParticles;
    [Tooltip("Dark aura particles when player has blood power.")]
    public ParticleSystem bloodAuraParticles;
    [Tooltip("Particles showing king getting stronger over time.")]
    public ParticleSystem kingPowerParticles;
    [Tooltip("Burst effect for instant push bonuses.")]
    public ParticleSystem instantPushBurst;
    
    [Header("Struggle Point")]
    [Tooltip("Optional reference to StrugglePoint script for additional control.")]
    public StrugglePoint strugglePoint;
    
    [Header("UI")]
    [Tooltip("UI element to show momentum (optional).")]
    public UnityEngine.UI.Image momentumMeter;
    [Tooltip("UI element to show blood power (optional).")]
    public UnityEngine.UI.Image bloodPowerMeter;
    [Tooltip("UI element to show king strength (optional).")]
    public UnityEngine.UI.Image kingStrengthMeter;
    
    [Header("Colors")]
    [Tooltip("Color gradient for momentum effects.")]
    public Gradient momentumColors = new Gradient();
    [Tooltip("Color gradient for blood power effects.")]
    public Gradient bloodColors = new Gradient();
    [Tooltip("Color gradient for king power effects.")]
    public Gradient kingColors = new Gradient();
    
    Vector3 _originalCameraPosition;
    bool _wasMaxMomentum = false;
    bool _hadInstantPush = false;
    
    void Start()
    {
        // Auto-find references if not set
        if (!duel) duel = GetComponent<BossMashDuel>();
        if (!shakeCamera) shakeCamera = Camera.main;
        if (!strugglePoint) strugglePoint = FindFirstObjectByType<StrugglePoint>();
        
        if (shakeCamera)
            _originalCameraPosition = shakeCamera.transform.localPosition;
            
        // Initialize color gradients if empty
        InitializeGradients();
    }
    
    void Update()
    {
        if (!duel) return;
        
        // Screen shake based on struggle intensity
        UpdateScreenShake();
        
        // Audio feedback
        UpdateAudioFeedback();
        
        // Visual effects
        UpdateVisualEffects();
    }
    
    void UpdateScreenShake()
    {
        if (!shakeCamera) return;
        
        float intensity = duel.StruggleIntensity * maxShakeIntensity;
        
        if (intensity > 0.01f)
        {
            Vector3 shake = Random.insideUnitCircle * intensity;
            shakeCamera.transform.localPosition = _originalCameraPosition + shake;
        }
        else
        {
            // Smoothly return to original position
            shakeCamera.transform.localPosition = Vector3.Lerp(
                shakeCamera.transform.localPosition, 
                _originalCameraPosition, 
                Time.deltaTime * 10f
            );
        }
    }
    
    void UpdateAudioFeedback()
    {
        // Instant push sound
        bool hasInstantPush = duel.HasInstantPush;
        if (hasInstantPush && !_hadInstantPush && instantPushSound)
        {
            instantPushSound.pitch = Random.Range(0.9f, 1.1f);
            instantPushSound.Play();
        }
        _hadInstantPush = hasInstantPush;
        
        // Momentum max sound
        bool isMaxMomentum = duel.CurrentMomentum > 0.95f;
        if (isMaxMomentum && !_wasMaxMomentum && momentumMaxSound)
        {
            momentumMaxSound.Play();
        }
        _wasMaxMomentum = isMaxMomentum;
        
        // Adjust struggle sound pitch based on intensity
        if (instantPushSound && duel.StruggleIntensity > 0.1f)
        {
            float targetPitch = Mathf.Lerp(strugglePitchRange.x, strugglePitchRange.y, duel.StruggleIntensity);
            instantPushSound.pitch = Mathf.Lerp(instantPushSound.pitch, targetPitch, Time.deltaTime * 5f);
        }
    }
    
    void UpdateVisualEffects()
    {
        // Momentum particles (hero aura)
        if (momentumParticles)
        {
            var emission = momentumParticles.emission;
            emission.rateOverTime = duel.CurrentMomentum * 50f;
            
            var main = momentumParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(momentumColors.Evaluate(duel.CurrentMomentum));
        }
        
        // Struggle particles (clash effects)
        if (struggleParticles)
        {
            var emission = struggleParticles.emission;
            emission.rateOverTime = duel.StruggleIntensity * 150f;
            
            var main = struggleParticles.main;
            Color struggleColor = Color.Lerp(Color.yellow, Color.white, duel.StruggleIntensity);
            main.startColor = new ParticleSystem.MinMaxGradient(struggleColor);
        }
        
        // Blood power aura (dark energy)
        if (bloodAuraParticles)
        {
            var emission = bloodAuraParticles.emission;
            emission.rateOverTime = duel.BloodPowerStrength * 30f;
            
            var main = bloodAuraParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(bloodColors.Evaluate(duel.BloodPowerStrength));
        }
        
        // King power particles (growing threat)
        if (kingPowerParticles)
        {
            var emission = kingPowerParticles.emission;
            float kingPowerNormalized = Mathf.Clamp01((duel.KingStrength - 1f) / 4f);
            emission.rateOverTime = kingPowerNormalized * 75f;
            
            var main = kingPowerParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(kingColors.Evaluate(kingPowerNormalized));
        }
        
        // Instant push burst
        if (instantPushBurst && duel.HasInstantPush && !_hadInstantPush)
        {
            instantPushBurst.Play();
        }
        
        // Update UI meters
        UpdateUIMeters();
    }
    
    void InitializeGradients()
    {
        // Initialize momentum colors if empty
        if (momentumColors.colorKeys.Length == 0)
        {
            var keys = new GradientColorKey[3];
            keys[0] = new GradientColorKey(Color.white, 0f);
            keys[1] = new GradientColorKey(Color.yellow, 0.5f);
            keys[2] = new GradientColorKey(Color.red, 1f);
            
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(0.5f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            momentumColors.SetKeys(keys, alphaKeys);
        }
        
        // Initialize blood colors if empty
        if (bloodColors.colorKeys.Length == 0)
        {
            var keys = new GradientColorKey[3];
            keys[0] = new GradientColorKey(Color.clear, 0f);
            keys[1] = new GradientColorKey(new Color(0.5f, 0f, 0f, 0.7f), 0.5f); // Dark red
            keys[2] = new GradientColorKey(new Color(0.8f, 0f, 0.2f, 1f), 1f);   // Bright dark red
            
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0.8f, 1f);
            
            bloodColors.SetKeys(keys, alphaKeys);
        }
        
        // Initialize king colors if empty
        if (kingColors.colorKeys.Length == 0)
        {
            var keys = new GradientColorKey[3];
            keys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.5f, 0.5f), 0f); // Dark blue
            keys[1] = new GradientColorKey(new Color(0.4f, 0f, 0.6f, 0.8f), 0.5f);   // Purple
            keys[2] = new GradientColorKey(new Color(0.6f, 0f, 0.8f, 1f), 1f);       // Bright purple
            
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(0.3f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            kingColors.SetKeys(keys, alphaKeys);
        }
    }
    
    void UpdateUIMeters()
    {
        // Momentum meter
        if (momentumMeter)
        {
            momentumMeter.fillAmount = duel.CurrentMomentum;
            momentumMeter.color = momentumColors.Evaluate(duel.CurrentMomentum);
        }
        
        // Blood power meter
        if (bloodPowerMeter)
        {
            bloodPowerMeter.fillAmount = duel.BloodPowerStrength;
            bloodPowerMeter.color = bloodColors.Evaluate(duel.BloodPowerStrength);
        }
        
        // King strength meter
        if (kingStrengthMeter)
        {
            float kingPowerNormalized = Mathf.Clamp01((duel.KingStrength - 1f) / 4f);
            kingStrengthMeter.fillAmount = kingPowerNormalized;
            kingStrengthMeter.color = kingColors.Evaluate(kingPowerNormalized);
        }
    }
    
    void OnDisable()
    {
        // Reset camera position when disabled
        if (shakeCamera)
            shakeCamera.transform.localPosition = _originalCameraPosition;
    }
}
