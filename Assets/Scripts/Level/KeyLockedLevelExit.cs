using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class KeyLockedLevelExit : MonoBehaviour
{
    public enum RequirementMode { Any, All }

    [Header("Next Level")]
    [SerializeField] private string nextSceneName = "Tutorial";

    [Header("Key Requirement")]
    [SerializeField] private List<string> requiredKeyIds = new List<string> { "tower_key" };
    [SerializeField] private RequirementMode requirement = RequirementMode.Any;
    [SerializeField] private bool consumeKeysOnOpen = true;

    [Header("Who can trigger")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool oneShot = true;

    [Header("Feedback")]
    [SerializeField] private AudioSource exitAudio;
    [SerializeField] private AudioClip lockedClip;

    [Tooltip("Optional: child prompt that fades 'Needs a key' over the door.")]
    [SerializeField] private LockedPrompt lockedPrompt;

    [Tooltip("Optional: SpriteRenderer to flash when locked.")]
    [SerializeField] private SpriteRenderer flashSprite;
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashTime = 0.12f;

    [Tooltip("Small shake on the transform when locked.")]
    [SerializeField] private float shakeTime = 0.12f;
    [SerializeField] private float shakeAmount = 0.06f;

    [Header("Unity Events")]
    [SerializeField] private UnityEngine.Events.UnityEvent onUnlockedTriggered;
    [SerializeField] private UnityEngine.Events.UnityEvent onLockedTried;

#if UNITY_EDITOR
    [SerializeField] private UnityEngine.Object sceneAsset;
    private void OnValidate()
    {
        if (sceneAsset != null)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(sceneAsset);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".unity"))
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrEmpty(file)) nextSceneName = file;
            }
        }
        var box = GetComponent<BoxCollider2D>();
        if (box && !box.isTrigger) box.isTrigger = true;
        if (exitAudio == null) exitAudio = GetComponentInChildren<AudioSource>();
        if (!flashSprite) flashSprite = GetComponentInChildren<SpriteRenderer>();
    }
#endif

    private bool _used;
    private Color _origColor;
    private bool _flashing;
    private Vector3 _basePos;
    private float _shakeT;

    private void Awake()
    {
        if (flashSprite) _origColor = flashSprite.color;
        _basePos = transform.localPosition;
    }

    private void Reset()
    {
        var box = GetComponent<BoxCollider2D>();
        if (!box) box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void Update()
    {
        // handle shake
        if (_shakeT > 0f)
        {
            _shakeT -= Time.deltaTime;
            float t = Mathf.Clamp01(_shakeT / Mathf.Max(0.0001f, shakeTime));
            Vector2 r = UnityEngine.Random.insideUnitCircle * (shakeAmount * t);
            transform.localPosition = _basePos + new Vector3(r.x, r.y, 0f);
            if (_shakeT <= 0f) transform.localPosition = _basePos;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_used && oneShot) return;
        if (!other.CompareTag(targetTag)) return;

        if (!IsUnlocked())
        {
            DoLockedFeedback();
            onLockedTried?.Invoke();
            return;
        }

        if (consumeKeysOnOpen) ConsumeRequiredKeys();

        _used = true;
        onUnlockedTriggered?.Invoke();
        LoadNext();
    }

    // ---------- Keys ----------
    private bool IsUnlocked()
    {
        if (requiredKeyIds == null || requiredKeyIds.Count == 0) 
        {
            Debug.Log("[KeyLockedLevelExit] No keys required - door is unlocked.");
            return true;
        }

        var gs = GameSession.Instance;
        if (gs == null)
        {
            Debug.LogWarning("[KeyLockedLevelExit] No GameSession found.");
            return false;
        }

        Debug.Log($"[KeyLockedLevelExit] Checking keys. Required: [{string.Join(", ", requiredKeyIds)}], Mode: {requirement}");
        Debug.Log($"[KeyLockedLevelExit] Player has {gs.KeyCount} keys total: [{string.Join(", ", gs.GetAllKeys())}]");

        if (requirement == RequirementMode.Any)
        {
            foreach (var id in requiredKeyIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    bool hasKey = gs.HasKey(id);
                    Debug.Log($"[KeyLockedLevelExit] Checking key '{id}': {hasKey}");
                    if (hasKey) 
                    {
                        Debug.Log($"[KeyLockedLevelExit] Found required key '{id}' - door unlocked!");
                        return true;
                    }
                }
            }
            Debug.Log("[KeyLockedLevelExit] No required keys found - door locked.");
            return false;
        }
        else // All
        {
            foreach (var id in requiredKeyIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning($"[KeyLockedLevelExit] Empty key ID in required list!");
                    return false;
                }
                bool hasKey = gs.HasKey(id);
                Debug.Log($"[KeyLockedLevelExit] Checking key '{id}': {hasKey}");
                if (!hasKey) 
                {
                    Debug.Log($"[KeyLockedLevelExit] Missing required key '{id}' - door locked.");
                    return false;
                }
            }
            Debug.Log("[KeyLockedLevelExit] All required keys found - door unlocked!");
            return true;
        }
    }

    private void ConsumeRequiredKeys()
    {
        var gs = GameSession.Instance;
        if (gs == null || requiredKeyIds == null) return;

        if (requirement == RequirementMode.Any)
        {
            foreach (var id in requiredKeyIds)
            {
                if (!string.IsNullOrWhiteSpace(id) && gs.HasKey(id))
                {
                    gs.RemoveKey(id);
                    break;
                }
            }
        }
        else // All
        {
            foreach (var id in requiredKeyIds)
                if (!string.IsNullOrWhiteSpace(id)) gs.RemoveKey(id);
        }
    }

    // ---------- Feedback ----------
    private void DoLockedFeedback()
    {
        if (lockedClip && exitAudio) exitAudio.PlayOneShot(lockedClip);
        if (lockedPrompt) lockedPrompt.Show("Needs a key");

        // flash
        if (flashSprite && !_flashing) StartCoroutine(FlashColor());

        // shake
        _shakeT = shakeTime;
    }

    private System.Collections.IEnumerator FlashColor()
    {
        _flashing = true;
        float t = 0f;
        while (t < flashTime)
        {
            t += Time.deltaTime;
            float k = Mathf.PingPong(t * 2f / flashTime, 1f);
            flashSprite.color = Color.Lerp(_origColor, flashColor, k);
            yield return null;
        }
        flashSprite.color = _origColor;
        _flashing = false;
    }

    // ---------- Scene load / fade ----------
    private void LoadNext()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("[KeyLockedLevelExit] nextSceneName is empty.");
            _used = false;
            return;
        }

        Time.timeScale = 1f;

        // Freeze player briefly (optional)
        var player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
#if UNITY_6000_OR_NEWER
                rb.velocity = Vector2.zero;
#else
                rb.linearVelocity = Vector2.zero;
#endif
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }

        var fader = FindFirstObjectByType<SceneFader>(FindObjectsInactive.Include);
        if (fader != null && TryInvokePublicFade(fader, nextSceneName))
            return;

        SceneManager.LoadScene(nextSceneName);
    }

    private bool TryInvokePublicFade(object fader, string sceneName)
    {
        var type = fader.GetType();
        string[] names = { "FadeAndLoad", "FadeToScene", "FadeOutAndLoad" };
        foreach (var n in names)
        {
            try
            {
                // Be more specific about the method signature to avoid AmbiguousMatchException
                var mi = type.GetMethod(n, BindingFlags.Instance | BindingFlags.Public, null, 
                    new Type[] { typeof(string) }, null);
                if (mi != null)
                {
                    mi.Invoke(fader, new object[] { sceneName });
                    return true;
                }
            }
            catch (AmbiguousMatchException)
            {
                // If there are multiple methods with the same name, try to find the one with string parameter
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => m.Name == n && m.GetParameters().Length == 1 && 
                               m.GetParameters()[0].ParameterType == typeof(string));
                
                var method = methods.FirstOrDefault();
                if (method != null)
                {
                    method.Invoke(fader, new object[] { sceneName });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KeyLockedLevelExit] Failed to invoke method '{n}' on {type.Name}: {ex.Message}");
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        if (!box) return;

        bool unlockedNow = Application.isPlaying ? IsUnlocked() : (requiredKeyIds == null || requiredKeyIds.Count == 0);
        Gizmos.color = unlockedNow ? new Color(0.2f, 1f, 0.2f, 0.25f) : new Color(1f, 0.25f, 0.1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.offset, box.size);
        Gizmos.color = unlockedNow ? new Color(0.2f, 0.9f, 0.2f, 0.9f) : new Color(1f, 0.3f, 0.1f, 0.9f);
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
