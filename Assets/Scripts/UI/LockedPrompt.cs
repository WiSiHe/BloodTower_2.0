using System.Collections;
using UnityEngine;
using TMPro;

/// World-space "Needs a key" style prompt that fades in/out and faces the camera.
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasGroup))]
public class LockedPrompt : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text text;

    [Header("Timing")]
    [SerializeField] private float fadeIn  = 0.08f;
    [SerializeField] private float hold    = 0.8f;
    [SerializeField] private float fadeOut = 0.35f;

    [Header("Motion")]
    [SerializeField] private Vector3 localRise = new Vector3(0f, 0.25f, 0f);

    private Canvas canvas;
    private CanvasGroup group;
    private Vector3 baseLocalPos;
    private Coroutine co;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        group  = GetComponent<CanvasGroup>();
        if (canvas) canvas.renderMode = RenderMode.WorldSpace;
        baseLocalPos = transform.localPosition;
        group.alpha = 0f;
        gameObject.SetActive(false);
    }

    /// Show the prompt with optional custom text
    public void Show(string msg = "Needs a key")
    {
        if (text) text.text = msg;
        if (co != null) StopCoroutine(co);
        gameObject.SetActive(true);
        co = StartCoroutine(DoShow());
    }

    private IEnumerator DoShow()
    {
        // Reset
        transform.localPosition = baseLocalPos;
        group.alpha = 0f;

        // Fade in
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.SmoothStep(0f, 1f, t / fadeIn);
            transform.localPosition = Vector3.Lerp(baseLocalPos, baseLocalPos + localRise, t / (fadeIn + hold + fadeOut));
            FaceCamera();
            yield return null;
        }
        group.alpha = 1f;

        // Hold
        t = 0f;
        while (t < hold)
        {
            t += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(baseLocalPos, baseLocalPos + localRise, (fadeIn + t) / (fadeIn + hold + fadeOut));
            FaceCamera();
            yield return null;
        }

        // Fade out
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.SmoothStep(1f, 0f, t / fadeOut);
            transform.localPosition = Vector3.Lerp(baseLocalPos, baseLocalPos + localRise, (fadeIn + hold + t) / (fadeIn + hold + fadeOut));
            FaceCamera();
            yield return null;
        }

        group.alpha = 0f;
        gameObject.SetActive(false);
        co = null;
    }

    private void FaceCamera()
    {
        if (!Camera.main) return;
        // Keep rotation flat (2D) but face current camera forward
        transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up);
    }
}