using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 2D moving platform that follows waypoints using a kinematic Rigidbody2D.
/// - Supports Loop, PingPong, or Once
/// - Optional wait time at each point
/// - Automatically sticks player to platform while standing on top (normal.y > 0.5)
/// - Draws path gizmos in the editor
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class MovingPlatform2D : MonoBehaviour
{
    public enum LoopMode { PingPong, Loop, Once }

    [Header("Motion")]
    [Tooltip("Units per second the platform moves.")]
    [SerializeField] private float speed = 2.5f;

    [Tooltip("Optional wait time (seconds) when reaching each waypoint.")]
    [SerializeField] private float waitAtPoints = 0.2f;

    [Tooltip("How to cycle through waypoints.")]
    [SerializeField] private LoopMode loopMode = LoopMode.PingPong;

    [Tooltip("Start moving on Start(). If false, call StartMoving() manually.")]
    [SerializeField] private bool startOnAwake = true;

    [Header("Waypoints")]
    [Tooltip("If empty, waypoints will be read from children of 'Path' (local positions).")]
    [SerializeField] private List<Vector2> localWaypoints = new List<Vector2>();

    [Tooltip("Optional parent transform containing child empties as waypoints.")]
    [SerializeField] private Transform pathParent;

    [Header("Stick To Top Surface")]
    [Tooltip("Tag of riders that should be parented to the platform when standing on top.")]
    [SerializeField] private string riderTag = "Player";

    [Tooltip("Minimum contact normal.y to treat as 'standing on top'.")]
    [SerializeField] private float topNormalThreshold = 0.5f;

    private Rigidbody2D rb;
    private Vector2[] worldPoints;
    private int currentIndex = 0;
    private int dir = 1; // +1 forward, -1 backward for PingPong
    private bool moving = false;
    private Coroutine waitCo;

    private void Reset()
    {
        // Make 2D collider solid and RB kinematic by default
        var col = GetComponent<BoxCollider2D>();
        col.usedByEffector = false;
        col.isTrigger = false;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Create a sample path
        if (transform.Find("Path") == null)
        {
            var p = new GameObject("Path").transform;
            p.SetParent(transform, false);
            var a = new GameObject("A").transform;
            var b = new GameObject("B").transform;
            a.SetParent(p, false);
            b.SetParent(p, false);
            a.localPosition = Vector3.zero;
            b.localPosition = new Vector3(3f, 0f, 0f);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb.bodyType != RigidbodyType2D.Kinematic) rb.bodyType = RigidbodyType2D.Kinematic;

        BuildWaypointArray();
        // Snap to first waypoint at start
        if (worldPoints.Length > 0)
        {
            rb.position = worldPoints[0];
            transform.position = worldPoints[0];
        }
    }

    private void Start()
    {
        if (startOnAwake) StartMoving();
    }

    public void StartMoving()
    {
        if (worldPoints.Length < 2) return;
        moving = true;
    }

    public void StopMoving()
    {
        moving = false;
    }

    private void FixedUpdate()
    {
        if (!moving || worldPoints.Length < 2) return;

        Vector2 currentPos = rb.position;
        Vector2 target = worldPoints[currentIndex];
        if ((target - currentPos).sqrMagnitude <= 0.0001f)
        {
            // Arrived exactly (snaps can happen if scene moved)
            AdvanceIndex();
            target = worldPoints[currentIndex];
        }

        // Move towards target
        Vector2 next = Vector2.MoveTowards(currentPos, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        // Reached target this tick?
        if ((next - target).sqrMagnitude <= 0.0000001f && waitCo == null && waitAtPoints > 0f)
        {
            waitCo = StartCoroutine(WaitThenAdvance());
        }
        else if ((next - target).sqrMagnitude <= 0.0000001f && waitAtPoints <= 0f)
        {
            AdvanceIndex();
        }
    }

    private IEnumerator WaitThenAdvance()
    {
        moving = false;
        yield return new WaitForSeconds(waitAtPoints);
        AdvanceIndex();
        moving = true;
        waitCo = null;
    }

    private void AdvanceIndex()
    {
        if (loopMode == LoopMode.Loop)
        {
            currentIndex = (currentIndex + 1) % worldPoints.Length;
            return;
        }

        if (loopMode == LoopMode.Once)
        {
            if (currentIndex < worldPoints.Length - 1)
            {
                currentIndex++;
            }
            else
            {
                moving = false; // stop at last point
            }
            return;
        }

        // PingPong
        if (dir > 0 && currentIndex >= worldPoints.Length - 1)
            dir = -1;
        else if (dir < 0 && currentIndex <= 0)
            dir = 1;

        currentIndex += dir;
    }

    private void BuildWaypointArray()
    {
        // If list empty, try to read from Path children
        if ((localWaypoints == null || localWaypoints.Count == 0) && pathParent == null)
        {
            pathParent = transform.Find("Path");
        }

        if (localWaypoints == null || localWaypoints.Count == 0)
        {
            if (pathParent != null)
            {
                var pts = new List<Vector2>();
                foreach (Transform child in pathParent)
                    pts.Add(child.localPosition);
                localWaypoints = pts;
            }
        }

        if (localWaypoints == null || localWaypoints.Count < 2)
        {
            worldPoints = new[] { (Vector2)transform.position, (Vector2)transform.position + Vector2.right * 2f };
        }
        else
        {
            worldPoints = new Vector2[localWaypoints.Count];
            for (int i = 0; i < worldPoints.Length; i++)
                worldPoints[i] = (Vector2)transform.TransformPoint(localWaypoints[i]);
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, worldPoints.Length - 1));
    }

    // --- Stick riders to the top surface by parenting while in contact ---
    private void OnCollisionStay2D(Collision2D c)
    {
        if (string.IsNullOrEmpty(riderTag)) return;
        if (!c.collider.CompareTag(riderTag)) return;

        for (int i = 0; i < c.contactCount; i++)
        {
            var n = c.GetContact(i).normal;
            // Contact normal points from platform into rider.
            // If rider stands on top, normal is roughly +Y.
            if (n.y > topNormalThreshold)
            {
                // Parent rider to platform so its motion is carried
                if (c.collider.transform.parent != transform)
                    c.collider.transform.SetParent(transform);
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D c)
    {
        if (string.IsNullOrEmpty(riderTag)) return;
        if (!c.collider.CompareTag(riderTag)) return;

        if (c.collider.transform.parent == transform)
            c.collider.transform.SetParent(null);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw local path (in editor)
        Color colA = new Color(1f, 0.2f, 0.1f, 0.9f);
        Color colB = new Color(1f, 0.6f, 0.3f, 0.9f);

        // Gather points similarly to runtime
        var temp = new List<Vector3>();
        if (localWaypoints != null && localWaypoints.Count > 1)
        {
            foreach (var lp in localWaypoints)
                temp.Add(transform.TransformPoint(lp));
        }
        else
        {
            var p = pathParent ? pathParent : transform.Find("Path");
            if (p != null)
                foreach (Transform child in p) temp.Add(child.position);
        }

        if (temp.Count < 2) return;

        Gizmos.color = colA;
        for (int i = 0; i < temp.Count; i++)
        {
            Gizmos.DrawSphere(temp[i], 0.06f);
            if (i < temp.Count - 1)
            {
                Gizmos.color = Color.Lerp(colA, colB, (float)i / (temp.Count - 2));
                Gizmos.DrawLine(temp[i], temp[i + 1]);
            }
        }
    }
#endif
}