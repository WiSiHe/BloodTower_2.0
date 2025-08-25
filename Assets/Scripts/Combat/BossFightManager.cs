using UnityEngine;

/// Hardens the duel when the player has 0 consumed children, while keeping parity/overpower pleasant.
/// Plug this onto the TowerTop scene and drag refs (PlayerShove, Player RB, BossKingController,
/// BossKnockback, Boss RB). It logs applied values on Start.
[DefaultExecutionOrder(-100)]
public class BossFightManager : MonoBehaviour
{
    [Header("Scene Refs")]
    [SerializeField] private PlayerShove playerShove;
    [SerializeField] private Rigidbody2D playerRB;
    [SerializeField] private BossKingController bossCtrl;
    [SerializeField] private BossKnockback bossKnock;
    [SerializeField] private Rigidbody2D bossRB;

    [Header("Kills curve anchors")]
    [SerializeField] private int parityKills    = 2;
    [SerializeField] private int overpowerKills = 4;

    // ---------- Player shove curve (ΔV in m/s) ----------
    [Header("Player Shove")]
    [Tooltip("Lower = weaker shove at 0 kills (hard mode).")]
    [SerializeField] private float playerDeltaV_0 = 3.0f;   // was ~3.6–4.8 → make it stingy
    [SerializeField] private float playerDeltaV_P = 5.4f;   // parity feels fair
    [SerializeField] private float playerDeltaV_O = 6.6f;   // overpowered feels strong

    [Tooltip("Extra boost if moving into the boss.")]
    [SerializeField] private float playerRelBoost = 0.9f;   // keep modest at baseline

    [Tooltip("Seconds between shove impulses.")]
    [SerializeField] private float playerCD_0 = 0.22f;      // slower shoves when 0 kills
    [SerializeField] private float playerCD_P = 0.14f;
    [SerializeField] private float playerCD_O = 0.11f;

    [Header("Assist & Minimum Impulse")]
    [SerializeField] private float pushAssist_0 = 20f;      // almost no “magnet” help at 0
    [SerializeField] private float pushAssist_P = 90f;
    [SerializeField] private float pushAssist_O = 120f;

    [SerializeField] private float minImpulse_0 = 34f;      // need a big hit to budge the king
    [SerializeField] private float minImpulse_P = 26f;
    [SerializeField] private float minImpulse_O = 28f;

    // ---------- King power ----------
    [Header("King Shove & Movement")]
    [SerializeField] private float kingShove_0 = 14f;       // hits harder at 0 kills
    [SerializeField] private float kingShove_P = 10f;
    [SerializeField] private float kingShove_O = 8.5f;

    [SerializeField] private float kingSpeed_0 = 7.0f;
    [SerializeField] private float kingSpeed_P = 5.2f;
    [SerializeField] private float kingSpeed_O = 4.6f;

    [SerializeField] private float kingAccel_0 = 42f;       // chases aggressively at 0
    [SerializeField] private float kingAccel_P = 28f;
    [SerializeField] private float kingAccel_O = 22f;

    [SerializeField] private float kingMax_0 = 9f;
    [SerializeField] private float kingMax_P = 7.2f;
    [SerializeField] private float kingMax_O = 6.2f;

    // ---------- Boss knockback resistance ----------
    [Header("Boss Knockback Resistance")]
    [Tooltip("Min horizontal slide enforced while stunned (higher = resists weak taps).")]
    [SerializeField] private float bossMinSpeed_0 = 11f;
    [SerializeField] private float bossMinSpeed_P = 9f;
    [SerializeField] private float bossMinSpeed_O = 8f;

    [Tooltip("How long the king is ‘soft‑stunned’ after being hit.")]
    [SerializeField] private float bossDisable_0 = 0.30f;   // shorter stun at 0 kills
    [SerializeField] private float bossDisable_P = 0.42f;
    [SerializeField] private float bossDisable_O = 0.48f;

    // ---------- Mass ----------
    [Header("Mass (momentum)")]
    [SerializeField] private float playerMass_0 = 1.6f;
    [SerializeField] private float playerMass_P = 1.6f;
    [SerializeField] private float playerMass_O = 1.6f;

    [SerializeField] private float bossMass_0 = 2.8f;       // heavier king at 0 kills
    [SerializeField] private float bossMass_P = 2.3f;
    [SerializeField] private float bossMass_O = 2.1f;

    [Header("Debug")]
    [SerializeField] private bool logAppliedValues = true;

    private void Reset() => Autofind();
    private void Awake() => Autofind();
    private void Start() => Apply();

#if UNITY_EDITOR
    [ContextMenu("Reapply Now")] private void ReapplyNow() => Apply();
#endif

    private void Autofind()
    {
        if (!playerShove) playerShove = FindObjectOfType<PlayerShove>(true);
        if (!playerRB)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
#else
            var pc = FindObjectOfType<PlayerController>(true);
#endif
            playerRB = playerShove ? playerShove.GetComponent<Rigidbody2D>() : pc ? pc.GetComponent<Rigidbody2D>() : null;
        }
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        if (!bossCtrl)  bossCtrl  = FindFirstObjectByType<BossKingController>(FindObjectsInactive.Include);
        if (!bossKnock) bossKnock = bossCtrl ? bossCtrl.GetComponent<BossKnockback>() : null;
        if (!bossRB)    bossRB    = bossCtrl ? bossCtrl.GetComponent<Rigidbody2D>() : null;
#else
        if (!bossCtrl)  bossCtrl  = FindObjectOfType<BossKingController>(true);
        if (!bossKnock) bossKnock = bossCtrl ? bossCtrl.GetComponent<BossKnockback>() : null;
        if (!bossRB)    bossRB    = bossCtrl ? bossCtrl.GetComponent<Rigidbody2D>() : null;
#endif
    }

    public void Apply()
    {
        Autofind();

        int kills = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;

        float tP = Mathf.InverseLerp(0, Mathf.Max(1, parityKills), kills);
        float tO = Mathf.InverseLerp(parityKills, Mathf.Max(parityKills + 1, overpowerKills), kills);
        float Blend(float a0, float aP, float aO) => Mathf.Lerp(Mathf.Lerp(a0, aP, tP), aO, tO);

        // compute targets
        float pΔV   = Blend(playerDeltaV_0, playerDeltaV_P, playerDeltaV_O);
        float pCD   = Blend(playerCD_0,     playerCD_P,     playerCD_O);
        float pAsst = Blend(pushAssist_0,   pushAssist_P,   pushAssist_O);
        float pMin  = Blend(minImpulse_0,   minImpulse_P,   minImpulse_O);

        float kShv  = Blend(kingShove_0,    kingShove_P,    kingShove_O);
        float kSpd  = Blend(kingSpeed_0,    kingSpeed_P,    kingSpeed_O);
        float kAcc  = Blend(kingAccel_0,    kingAccel_P,    kingAccel_O);
        float kMax  = Blend(kingMax_0,      kingMax_P,      kingMax_O);

        float bMinS = Blend(bossMinSpeed_0, bossMinSpeed_P, bossMinSpeed_O);
        float bDis  = Blend(bossDisable_0,  bossDisable_P,  bossDisable_O);

        float mP    = Blend(playerMass_0,    playerMass_P,   playerMass_O);
        float mK    = Blend(bossMass_0,      bossMass_P,     bossMass_O);

        // apply
        if (playerRB) playerRB.mass = mP;
        if (playerShove)
        {
            playerShove.SetDesign(pΔV, 0f, playerRelBoost, pCD);

            // set private/public fields for assist & minImpulse if they exist
            var t = typeof(PlayerShove);
            var fAssist = t.GetField("pushAssistForce", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fMinImp = t.GetField("minImpulse",      System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fAssist != null) fAssist.SetValue(playerShove, pAsst);
            if (fMinImp != null) fMinImp.SetValue(playerShove, pMin);
        }

        if (bossRB) bossRB.mass = mK;

        if (bossCtrl) bossCtrl.SetDesign(
            shoveImpulse: kShv,
            shoveCooldown: 0.35f,   // keep your current unless you expose it
            targetSpeed: kSpd,
            accelGain:  kAcc,
            maxSpeed:   kMax
        );

        if (bossKnock)
        {
            var t = typeof(BossKnockback);
            var fMin = t.GetField("minHorizontalSpeed",  System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fDis = t.GetField("defaultDisableSeconds", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fMin != null) fMin.SetValue(bossKnock, bMinS);
            if (fDis != null) fDis.SetValue(bossKnock, bDis);
        }

        if (logAppliedValues)
            Debug.Log($"[BossFightManager] Kills={kills} → PΔV={pΔV:0.00}, PCD={pCD:0.00}, Assist={pAsst}, MinImp={pMin} | KShove={kShv}, KSpd={kSpd}, KAcc={kAcc}, KMax={kMax} | BossMinSlide={bMinS}, Stun={bDis}, mP={mP}, mK={mK}");
    }
}