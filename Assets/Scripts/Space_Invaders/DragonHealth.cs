using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DragonHealth : MonoBehaviour
{
    [SerializeField] private int hp = 5;
    [SerializeField] private string keyIdOnDeath = "dragon_key";
    [SerializeField] private bool  destroyOnDeath = true;

    [Header("Hit Effects")]
    [SerializeField] private GameObject hitParticlePrefab;
    [SerializeField] private Transform hitEffectPoint;
    [SerializeField] private float hitEffectDuration = 1.0f;
    [SerializeField] private bool useScreenShake = true;
    [SerializeField] private float screenShakeIntensity = 0.5f;
    [SerializeField] private float screenShakeDuration = 0.3f;

    public void ApplyDamage(int dmg)
    {
        hp -= Mathf.Max(1, dmg);
        
        // Play hit effects
        PlayHitEffects();
        
        if (hp <= 0)
        {
            // victory in the minigame
            if (GameSession.Instance != null)
                GameSession.Instance.AddKey(keyIdOnDeath);

            MiniGameManager.Instance?.RegisterDragonDefeat();

            if (destroyOnDeath) Destroy(gameObject);
        }
    }

    private void PlayHitEffects()
    {
        // Play particle effect
        if (hitParticlePrefab != null)
        {
            Vector3 effectPosition = hitEffectPoint != null ? hitEffectPoint.position : transform.position;
            GameObject effect = Instantiate(hitParticlePrefab, effectPosition, Quaternion.identity);
            
            // Auto-destroy the effect after duration
            Destroy(effect, hitEffectDuration);
        }

        // Screen shake effect
        if (useScreenShake)
        {
            StartCoroutine(ScreenShake());
        }
    }

    private System.Collections.IEnumerator ScreenShake()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPosition = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < screenShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * screenShakeIntensity;
            float y = Random.Range(-1f, 1f) * screenShakeIntensity;
            
            cam.transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.transform.localPosition = originalPosition;
    }
}
