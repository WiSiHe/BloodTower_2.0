using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KingFallZone : MonoBehaviour
{
    [SerializeField] private string bossTag = "Boss";
    [SerializeField] private BossFightManager manager; // auto-found if empty

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (manager == null)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            manager = Object.FindFirstObjectByType<BossFightManager>(FindObjectsInactive.Include);
#else
            manager = Object.FindObjectOfType<BossFightManager>(true);
#endif
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(bossTag)) return;
        var go = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        manager?.OnBossFell(go);
    }
}