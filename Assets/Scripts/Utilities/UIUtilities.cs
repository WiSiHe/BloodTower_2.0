using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIUtilities
{
    // Keep these public names stable so other scripts can call them.
    public static void EnsureSingleEventSystem()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var systems = Object.FindObjectsOfType<EventSystem>(true);
#endif
        if (systems.Length == 0)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            return;
        }

        // Keep the first, remove extras
        for (int i = 1; i < systems.Length; i++)
            Object.Destroy(systems[i].gameObject);
        if (!systems[0].gameObject.activeInHierarchy)
            systems[0].gameObject.SetActive(true);
    }

    /// <summary>Turn off raycast blocking for any invisible CanvasGroup (scene + DDOL).</summary>
    public static void ForceUnblockAllRaycastsEverywhere()
    {
        var groups = Resources.FindObjectsOfTypeAll<CanvasGroup>();
        foreach (var g in groups)
        {
            if (!g) continue;
            // If invisible or disabled, it should not block
            if (g.alpha <= 0.001f || !g.gameObject.activeInHierarchy)
            {
                g.blocksRaycasts = false;
                g.interactable = false;
            }
        }
    }

    // Backwards-compatible names (no-ops that call the new routine)
    public static void UnlockAllCanvasGroupsInChildren(GameObject _) => ForceUnblockAllRaycastsEverywhere();
    public static void EnableGraphicRaycastersInChildren(GameObject root)
    {
        var rcs = root.GetComponentsInChildren<GraphicRaycaster>(true);
        foreach (var rc in rcs) rc.enabled = true;
    }
}