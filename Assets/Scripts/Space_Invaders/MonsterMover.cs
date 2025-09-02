// Assets/Scripts/Monsters/MonsterMover.cs
using UnityEngine;

public class MonsterMover : MonoBehaviour
{
    [Header("Downward motion")]
    public float minSpeed = 0.8f;
    public float maxSpeed = 1.6f;

    [Header("Optional horizontal wobble")]
    public bool wobble = false;
    public float wobbleAmplitude = 0.25f;
    public float wobbleFrequency = 1.2f;

    [Header("Auto-despawn when far below camera")]
    public float extraDespawnBelow = 3.0f;

    float speed;
    float baseX;
    Camera cam;

    void Start()
    {
        speed = Random.Range(minSpeed, maxSpeed);
        baseX = transform.position.x;
        cam = Camera.main;
    }

    void Update()
    {
        // go down
        transform.position += Vector3.down * speed * Time.deltaTime;

        // slight left-right wobble (optional)
        if (wobble)
        {
            float x = baseX + Mathf.Sin(Time.time * wobbleFrequency) * wobbleAmplitude;
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }

        // Despawn when well below the screen (works even if nothing is tagged Ground)
        if (cam && cam.orthographic)
        {
            float bottomY = cam.transform.position.y - cam.orthographicSize - extraDespawnBelow;
            if (transform.position.y < bottomY) Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}