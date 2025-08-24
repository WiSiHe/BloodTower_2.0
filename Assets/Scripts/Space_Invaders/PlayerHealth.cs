using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // <-- add this

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int startingLives = 3;
    [SerializeField] float invulnDuration = 1f;
    [SerializeField] float blinkTotal = 0.25f;   // total blink time per hit
    [SerializeField] float blinkStep  = 0.05f;   // toggle interval

    // Scene to load on death
    [SerializeField] string deathSceneName = "IsometricHall";
    [SerializeField] float deathLoadDelay = 0f;  // set >0 if you want a tiny pause after blink

    public int CurrentLives { get; private set; }

    float invulnUntil;
    Coroutine blinkCo;

    void Awake()
    {
        CurrentLives = Mathf.Max(1, startingLives);
    }

    public void TakeDamage(int amount = 1)
    {
        // ignore hits during i-frames
        if (Time.time < invulnUntil) return;

        invulnUntil = Time.time + invulnDuration;
        CurrentLives = Mathf.Max(0, CurrentLives - Mathf.Max(1, amount));

        bool lethal = CurrentLives <= 0;

        // restart blink every time you take damage
        if (blinkCo != null) StopCoroutine(blinkCo);
        blinkCo = StartCoroutine(BlinkAndMaybeDie(lethal));
    }

    IEnumerator BlinkAndMaybeDie(bool lethal)
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        float end = Time.time + blinkTotal;
        bool on = true;

        while (Time.time < end)
        {
            on = !on;
            foreach (var r in renderers) r.enabled = on;
            yield return new WaitForSeconds(blinkStep);
        }

        // ensure visible at the end
        foreach (var r in renderers) r.enabled = true;

        if (lethal)
        {
            Die();
        }
    }

    void Die()
    {
        // Optionally freeze controls/physics here if needed
        var ctrl = GetComponent<SpaceInvadersPlayer>(); if (ctrl) ctrl.enabled = false;
        var shooter = GetComponent<PlayerShooter>();    if (shooter) shooter.enabled = false;
        var rb = GetComponent<Rigidbody2D>();           if (rb) rb.simulated = false;

        if (deathLoadDelay <= 0f)
            SceneManager.LoadScene(deathSceneName);
        else
            StartCoroutine(LoadSceneAfterDelay());
    }

    IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(deathLoadDelay);
        SceneManager.LoadScene(deathSceneName);
    }

    // Keep your collision/trigger calling TakeDamage(1) as before
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Monster") || other.CompareTag("Fireball"))
            TakeDamage(1);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.CompareTag("Monster") || col.collider.CompareTag("Fireball"))
            TakeDamage(1);
    }
}
