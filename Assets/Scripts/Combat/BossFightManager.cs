using UnityEngine;
using UnityEngine.SceneManagement;

public class BossFightManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string victorySceneFallback = "Victory_Neutral";
    [SerializeField] private string gameOverSceneName    = "GameOver";
    [SerializeField] private EndingRouter endingRouter; // optional; auto-found

    [Header("Auto‑Balance")]
    [SerializeField] private int parityKills    = 3;
    [SerializeField] private int overpowerKills = 5;

    [Header("Player Shove (delta‑V)")]
    [SerializeField] private float playerDeltaV_0Kills = 2.8f;
    [SerializeField] private float playerDeltaV_Parity = 3.6f;
    [SerializeField] private float playerDeltaV_Over   = 4.5f;
    [SerializeField] private float playerRelBoost      = 0.4f;
    [SerializeField] private float playerShoveCooldown = 0.25f;

    [Header("King Settings")]
    [SerializeField] private float kingShove_0Kills = 12f;
    [SerializeField] private float kingShove_Parity = 16f;
    [SerializeField] private float kingShove_Over   = 18f;
    [SerializeField] private float kingTargetSpeed  = 6f;
    [SerializeField] private float kingAccelGain    = 40f;
    [SerializeField] private float kingMaxSpeed     = 10f;

    [Header("Mass & Friction (optional)")]
    [SerializeField] private float playerMass = 1.8f;
    [SerializeField] private float kingMass   = 2.6f;
    [SerializeField] private PhysicsMaterial2D lowFriction; // friction ~0.05–0.1

    [Header("Refs (optional)")]
    [SerializeField] private GameObject boss; // King root
    [SerializeField] private PlayerShove playerShove;
    [SerializeField] private BossKingController bossController;

    private bool ended;

    private void Awake()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        // Auto-find refs (no deprecation warnings)
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        if (!endingRouter)   endingRouter   = Object.FindFirstObjectByType<EndingRouter>(FindObjectsInactive.Include);
        if (!playerShove)    playerShove    = Object.FindFirstObjectByType<PlayerShove>(FindObjectsInactive.Include);
        if (!bossController) bossController = Object.FindFirstObjectByType<BossKingController>(FindObjectsInactive.Include);
#else
        if (!endingRouter)   endingRouter   = Object.FindObjectOfType<EndingRouter>(true);
        if (!playerShove)    playerShove    = Object.FindObjectOfType<PlayerShove>(true);
        if (!bossController) bossController = Object.FindObjectOfType<BossKingController>(true);
#endif
        if (!boss)
        {
            var b = GameObject.FindGameObjectWithTag("Boss");
            if (b) boss = b;
        }

        ApplyAutoBalance();
    }

    private void ApplyAutoBalance()
    {
        int kills = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;

        float pDeltaV = LerpByKills(kills, 0, parityKills, overpowerKills,
                                    playerDeltaV_0Kills, playerDeltaV_Parity, playerDeltaV_Over);

        if (playerShove)
        {
            float perChild = (playerDeltaV_Over - playerDeltaV_0Kills) / Mathf.Max(1f, overpowerKills);
            playerShove.SetDesign(pDeltaV, perChild, playerRelBoost, playerShoveCooldown);

            var rb = playerShove.GetComponent<Rigidbody2D>();
            if (rb) { rb.mass = playerMass; rb.freezeRotation = true; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; rb.sleepMode = RigidbodySleepMode2D.NeverSleep; }
            var col = playerShove.GetComponent<Collider2D>();
            if (col && lowFriction) col.sharedMaterial = lowFriction;
        }

        float kShove = LerpByKills(kills, 0, parityKills, overpowerKills,
                                   kingShove_0Kills, kingShove_Parity, kingShove_Over);

        if (bossController)
        {
            bossController.SetDesign(kShove, 0.35f, kingTargetSpeed, kingAccelGain, kingMaxSpeed);

            var rb = bossController.GetComponent<Rigidbody2D>();
            if (rb) { rb.mass = kingMass; rb.freezeRotation = true; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; rb.sleepMode = RigidbodySleepMode2D.NeverSleep; }
            var col = bossController.GetComponent<Collider2D>();
            if (col && lowFriction) col.sharedMaterial = lowFriction;
        }

        Debug.Log($"[BossFightManager] Kills={kills} -> PlayerΔV={pDeltaV:0.00} m/s, KingShove={kShove:0.0}");
    }

    private static float LerpByKills(int kills, int k0, int k1, int k2, float v0, float v1, float v2)
    {
        if (kills <= k0) return v0;
        if (kills >= k2) return v2;
        if (kills <= k1) return Mathf.Lerp(v0, v1, (kills - k0) / Mathf.Max(1f, (float)(k1 - k0)));
        return Mathf.Lerp(v1, v2, (kills - k1) / Mathf.Max(1f, (float)(k2 - k1)));
    }

    // ---- Called by fall zones ----
    public void OnBossFell(GameObject bossGO)
    {
        if (ended) return; ended = true;
        if (bossGO) Destroy(bossGO);

        if (endingRouter != null) endingRouter.LoadEndingScene();
        else LoadScene(victorySceneFallback);
    }

    public void OnPlayerDied()
    {
        if (ended) return; ended = true;
        LoadScene(gameOverSceneName);
    }

    private void LoadScene(string scene)
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        var fader = Object.FindFirstObjectByType<SceneFader>(FindObjectsInactive.Include);
#else
        var fader = Object.FindObjectOfType<SceneFader>(true);
#endif
        Time.timeScale = 1f;
        if (fader != null) fader.FadeToScene(scene);
        else SceneManager.LoadScene(scene);
    }
}