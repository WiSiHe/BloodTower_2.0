using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Prefabs to spawn (pick one at random)")]
    [SerializeField] private GameObject[] monsterPrefabs;

    [Header("Spawn cadence (seconds)")]
    [SerializeField] private float minInterval = 0.8f;
    [SerializeField] private float maxInterval = 2.0f;

    [Header("Screen margins (world units)")]
    [SerializeField] private float xMargin = 0.5f;
    [SerializeField] private float topPadding = 0.5f;

    [Header("Parenting (optional)")]
    [SerializeField] private Transform spawnParent;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.8f, 0.2f, 0.9f);

    [SerializeField] private Camera cam; // assign in Inspector if you like

    private Coroutine loop;

    void Awake()
    {
        if (!cam)
        {
#if UNITY_2023_1_OR_NEWER
            // One-parameter overload (widely available)
            cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (!cam) cam = Camera.main;
#else
            cam = Camera.main;
            if (!cam) cam = Object.FindObjectOfType<Camera>();
#endif
        }
    }

    void OnEnable()
    {
        if (loop != null) StopCoroutine(loop);
        loop = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    IEnumerator SpawnLoop()
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogWarning("[MonsterSpawner] No prefabs assigned.");
            yield break;
        }

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        if (!cam) { Debug.LogWarning("[MonsterSpawner] No camera found (tag MainCamera or assign reference)."); return; }
        if (monsterPrefabs == null || monsterPrefabs.Length == 0) return;

        Vector3 c = cam.transform.position;
        float halfH = cam.orthographic
            ? cam.orthographicSize
            : Mathf.Abs(c.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfW = halfH * cam.aspect;

        float leftX  = c.x - halfW + xMargin;
        float rightX = c.x + halfW - xMargin;
        float topY   = c.y + halfH + topPadding;

        float x = Random.Range(leftX, rightX);
        float y = topY;

        var prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        if (!prefab) return;

        var go = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, spawnParent);
        if (debugLogs) Debug.Log($"[MonsterSpawner] Spawned {go.name} at ({x:F2},{y:F2})");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var drawCam = cam ? cam : Camera.main;
        if (!drawCam) return;

        Vector3 c = drawCam.transform.position;
        float halfH = drawCam.orthographic
            ? drawCam.orthographicSize
            : Mathf.Abs(c.z) * Mathf.Tan(drawCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfW = halfH * drawCam.aspect;

        float leftX  = c.x - halfW + xMargin;
        float rightX = c.x + halfW - xMargin;
        float topY   = c.y + halfH + topPadding;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(new Vector3(leftX, topY, 0f), new Vector3(rightX, topY, 0f));
        Gizmos.DrawSphere(new Vector3(leftX, topY, 0f), 0.05f);
        Gizmos.DrawSphere(new Vector3(rightX, topY, 0f), 0.05f);
    }
#endif
}