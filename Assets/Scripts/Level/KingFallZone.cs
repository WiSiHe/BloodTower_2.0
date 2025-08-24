using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KingFallZone : MonoBehaviour
{
    [SerializeField] private string bossTag = "Boss";
    [SerializeField] private BossFightManager manager; // auto-found if not set

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (manager == null) manager = FindObjectOfType<BossFightManager>(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(bossTag)) return;
        manager?.OnBossFell(other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject);
    }
}