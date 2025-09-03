using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BounceBell : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float bounceForce = 60f; // Increased to work with higher gravity scale
    public bool resetAirJumps = true;
    
    [Header("Trigger Settings")]
    // No entry speed checks: any contact triggers bounce
    
    [Header("Cooldown")]
    public float cooldown = 0.02f; // Very short cooldown for responsive bouncing
    
    [Header("Effects")]
    public AudioSource sfx;
    public ParticleSystem hitVfx;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        
        if (!other.CompareTag("Player")) return;
        
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        
        // No requirement: bounce on any trigger enter
        

        
        // Apply the bounce
        player.ExternalBounce(bounceForce, resetAirJumps);
        
        // Play effects
        if (sfx) sfx.Play();
        if (hitVfx) hitVfx.Play();
        
        // Play bounce sound via AudioManager
        if (AudioManager.Instance) 
        {
            AudioManager.Instance.PlayBounceSound();
        }

    }
    
}
