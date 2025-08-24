using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BounceBell2D : MonoBehaviour
{
	[Header("Bounce")]
	public float bounceImpulse = 12f;
	public bool resetAirJumps = true;

	[Tooltip("Require the player to be moving downward to trigger (prevents side hits).")]
	public bool requireDownwardEntry = true;
	public float minDownwardSpeed = -0.5f;

	[Header("Flow")]
	[Tooltip("Prevent double-triggering on the same frame.")]
	public float cooldown = 0.05f;
	public bool oneUse = false;
	public float respawnDelay = 10f;   // time before coming back

	[Header("FX (optional)")]
	public AudioSource sfx;
	public ParticleSystem hitVfx;

	private bool _cooling;
	private Collider2D _collider;
	private SpriteRenderer _renderer; // assumes a sprite, adjust if mesh/other

	void Awake()
	{
		_collider = GetComponent<Collider2D>();
		_renderer = GetComponent<SpriteRenderer>();
	}

	void Reset()
	{
		var col = GetComponent<Collider2D>();
		col.isTrigger = true;
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (_cooling) return;

		// Find the PlayerController on the root or parent
		var player = other.GetComponentInParent<PlayerController>();
		if (player == null) return;

		var rb = other.attachedRigidbody;
		if (requireDownwardEntry && rb != null && rb.linearVelocity.y > minDownwardSpeed) return;

		_cooling = true;

		// Do the bounce
		player.ExternalBounce(bounceImpulse, resetAirJumps);

		if (sfx) sfx.Play();
		if (hitVfx) hitVfx.Play();

		if (oneUse)
		{
			// Optional: delay destroy to let SFX/VFX fire
			Destroy(gameObject, 0.02f);
		}
		else
		{
			StartCoroutine(DisappearAndRespawn());
		}
	}

	IEnumerator DisappearAndRespawn()
	{
		// Disable interaction & visuals
		_collider.enabled = false;
		if (_renderer) _renderer.enabled = false;

		yield return new WaitForSeconds(respawnDelay);

		// Re-enable
		_collider.enabled = true;
		if (_renderer) _renderer.enabled = true;

		_cooling = false;
	}
}
