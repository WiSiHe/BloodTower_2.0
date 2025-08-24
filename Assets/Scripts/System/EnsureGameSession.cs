using UnityEngine;

public class EnsureGameSession : MonoBehaviour
{
    private void Awake()
    {
        if (GameSession.Instance != null) return;

        var go = new GameObject("GameSession (Auto)");
        go.AddComponent<GameSession>();      // GameSession’s Awake will DontDestroyOnLoad
        Debug.Log("[EnsureGameSession] Created GameSession at runtime.");
    }
}