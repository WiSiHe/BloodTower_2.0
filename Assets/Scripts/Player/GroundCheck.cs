using UnityEngine;

public class GroundCheck2D : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayers;
    public bool IsGrounded { get; private set; }

    int overlaps = 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & groundLayers) != 0) overlaps++;
        if (overlaps > 0) IsGrounded = true;
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & groundLayers) != 0) overlaps = Mathf.Max(0, overlaps - 1);
        if (overlaps == 0) IsGrounded = false;
    }
}