using UnityEngine;
using UnityEngine.UI;

public class MashVFXController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MashInput mash;          // your MashInput on the “BossMash” GO
    [SerializeField] private BossMashDuel duel;       // the duel driver (for meter)
    [SerializeField] private Image meterImage;        // drag the meter Image (fillAmount 0..1)

    [SerializeField] private Transform playerSocket;  // where to spawn sparks on the player
    [SerializeField] private Transform kingSocket;    // where to spawn sparks on the king
    [SerializeField] private ParticleSystem playerSparksPrefab;
    [SerializeField] private ParticleSystem kingSparksPrefab;

    [Header("Tuning")]
    [SerializeField] private float lightRate = 4f;    // taps/sec
    [SerializeField] private float heavyRate = 7.5f;  // taps/sec
    [SerializeField] private float minSpawnGap = 0.12f;

    private float lastSpawn;
    private float smoothedRate;

    void Reset()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
        mash = FindFirstObjectByType<MashInput>(FindObjectsInactive.Include);
        duel = FindFirstObjectByType<BossMashDuel>(FindObjectsInactive.Include);
#else
        mash = FindObjectOfType<MashInput>(true);
        duel = FindObjectOfType<BossMashDuel>(true);
#endif
        // If your duel exposes the meter Image, auto-pull it:
        if (!meterImage && duel != null)
        {
            var meterField = typeof(BossMashDuel).GetField("meterFill",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (meterField != null) meterImage = meterField.GetValue(duel) as Image;
        }
    }

    void Update()
    {
        // 1) Mash rate from tap delta this frame
        int taps = mash ? mash.ConsumeTapCount() : 0;                // see tiny addition to MashInput below
        float instRate = (Time.deltaTime > 0f) ? taps / Time.deltaTime : 0f;
        smoothedRate = Mathf.Lerp(smoothedRate, instRate, 0.25f);

        // 2) Advantage from meter (0..1 → -1..+1)
        float adv01 = 0.5f;
        if (meterImage) adv01 = Mathf.Clamp01(meterImage.fillAmount);
        float adv = adv01 * 2f - 1f;

        if (Time.time - lastSpawn < minSpawnGap) return;

        bool playerWinning = adv > 0.05f;
        if (smoothedRate >= heavyRate)      Burst(playerWinning, true);
        else if (smoothedRate >= lightRate) Burst(playerWinning, false);
    }

    void Burst(bool playerSide, bool strong)
    {
        lastSpawn = Time.time;
        var socket = playerSide ? playerSocket : kingSocket;
        var prefab = playerSide ? playerSparksPrefab : kingSparksPrefab;
        if (socket && prefab)
        {
            var fx = Instantiate(prefab, socket.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 1.5f);
        }
    }
}