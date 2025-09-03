using System.Collections;
using UnityEngine;

/// Simple visual feedback when powered: tint + scale pulse + optional particles.
[RequireComponent(typeof(SpriteRenderer))]
public class PowerUpVisual : MonoBehaviour
{
    [Header("Pulse")]
    [SerializeField] private float pulseScale = 1.08f;
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private Color poweredTint = new Color(1f, 0.95f, 0.4f, 1f);

    [Header("Optional VFX")]
    [SerializeField] private ParticleSystem auraParticles; // optional

    SpriteRenderer sr;
    Color baseColor;
    Vector3 baseScale;
    Coroutine co;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        baseScale = transform.localScale;
    }

    public void SetPowered(bool powered)
    {
        if (powered)
        {
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(Pulse());
            if (auraParticles) auraParticles.Play();
        }
        else
        {
            if (co != null) StopCoroutine(co);
            co = null;
            sr.color = baseColor;
            transform.localScale = baseScale;
            if (auraParticles) auraParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    IEnumerator Pulse()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            float k = (Mathf.Sin(t) * 0.5f + 0.5f); // 0..1
            sr.color = Color.Lerp(baseColor, poweredTint, 0.55f + 0.45f * k);
            float s = Mathf.Lerp(1f, pulseScale, k);
            transform.localScale = baseScale * s;
            yield return null;
        }
    }
}