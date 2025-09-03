using System;
using UnityEngine;

public class ProjectileDespawnNotifier : MonoBehaviour
{
    public Action<GameObject> onGone;
    void OnDestroy() { onGone?.Invoke(gameObject); }
}