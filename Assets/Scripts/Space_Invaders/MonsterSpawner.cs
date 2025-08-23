using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Prefabs to spawn (pick one at random)")]
    public GameObject[] monsterPrefabs;

    [Header("Spawn cadence (seconds)")]
    public float minInterval = 0.8f;
    public float maxInterval = 2.0f;

    [Header("Screen margins")]
    public float xMargin = 0.5f;     // keep a bit inside the edges
    public float topPadding = 0.5f;  // spawn slightly above the camera

    Camera cam;

    void Awake() => cam = Camera.main;

    void OnEnable() => StartCoroutine(SpawnLoop());
    void OnDisable() => StopAllCoroutines();

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0) return;

        // Camera viewport → world bounds
        Vector3 leftTop  = cam.ViewportToWorldPoint(new Vector3(0f, 1f, 0f));
        Vector3 rightTop = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        float xMin = leftTop.x + xMargin;
        float xMax = rightTop.x - xMargin;
        float x    = Random.Range(xMin, xMax);
        float y    = leftTop.y + topPadding; // a bit above the visible top

        // pick a random prefab
        var prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity);
    }
}
