using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MashPoseAnimator : MonoBehaviour
{
    [Tooltip("Optional. If left empty, will search in scene.")]
    public BossMashDuel duel;

    [Tooltip("Animator float parameter that receives the 0..1 progress.")]
    public string parameter = "mashProgress";

    Animator anim;
    int paramHash;

    void Awake()
    {
        anim = GetComponent<Animator>();
        paramHash = Animator.StringToHash(parameter);

        if (!duel)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            duel = Object.FindFirstObjectByType<BossMashDuel>();
#else
            duel = Object.FindObjectOfType<BossMashDuel>();
#endif
        }
    }

    void Update()
    {
        if (!duel || !anim) return;
        anim.SetFloat(paramHash, duel.Progress01);   // 0..1
    }
}