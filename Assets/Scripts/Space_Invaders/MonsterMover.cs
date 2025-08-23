using UnityEngine;

public class MonsterMover : MonoBehaviour
{
    [Header("Downward motion")]
    public float minSpeed = 0.8f;
    public float maxSpeed = 1.6f;

    [Header("Optional horizontal wobble")]
    public bool wobble = true;
    public float wobbleAmplitude = 0.25f;
    public float wobbleFrequency = 1.2f;

    float speed;
    float baseX;

    void Start()
    {
        speed = Random.Range(minSpeed, maxSpeed);
        baseX = transform.position.x;
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
    }

    // Clean up when off any camera
   void OnBecameInvisible()
{
    Destroy(gameObject);
}

}
