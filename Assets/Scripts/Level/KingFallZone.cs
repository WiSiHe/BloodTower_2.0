using UnityEngine;

public class KingFallZone : MonoBehaviour
{
    [SerializeField] private string bossTag = "Boss";
    [SerializeField] private BossFightManager manager; // assign in Inspector, or it will auto-find

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (!col) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (manager == null) manager = FindObjectOfType<BossFightManager>(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(bossTag)) return;

        Debug.Log("[KingFallZone] Boss fell!");
        if (manager != null) manager.OnBossFell(other.gameObject);
        else Destroy(other.gameObject);
    }
}