using UnityEngine;

/// <summary>
/// Simple audio manager for playing sound effects
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource jumpAudioSource;
    [SerializeField] private AudioSource dashAudioSource;
    [SerializeField] private AudioSource damageAudioSource;
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip damageClip;
    
    public static AudioManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Auto-setup audio sources if not assigned
        if (jumpAudioSource == null)
        {
            jumpAudioSource = gameObject.AddComponent<AudioSource>();
            jumpAudioSource.playOnAwake = false;
            jumpAudioSource.volume = 0.7f;
        }
        
        if (dashAudioSource == null)
        {
            dashAudioSource = gameObject.AddComponent<AudioSource>();
            dashAudioSource.playOnAwake = false;
            dashAudioSource.volume = 0.5f;
        }
        
        if (damageAudioSource == null)
        {
            damageAudioSource = gameObject.AddComponent<AudioSource>();
            damageAudioSource.playOnAwake = false;
            damageAudioSource.volume = 0.8f;
        }
    }
    
    public void PlayJumpSound()
    {
        if (jumpClip != null && jumpAudioSource != null)
        {
            jumpAudioSource.PlayOneShot(jumpClip);
        }
    }
    
    public void PlayDashSound()
    {
        if (dashClip != null && dashAudioSource != null)
        {
            dashAudioSource.PlayOneShot(dashClip);
        }
    }
    
    public void PlayDamageSound()
    {
        if (damageClip != null && damageAudioSource != null)
        {
            damageAudioSource.PlayOneShot(damageClip);
        }
    }
}
