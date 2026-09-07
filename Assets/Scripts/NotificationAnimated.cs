using System.Collections;
using UnityEngine;

// NotificationAnimated: animates a notification panel with a pop-then-settle scale on enable,
// then continuous bob and rotation. Uses unscaled time so it works during pauses.
// Hide() collapses the panel with a quick scale-to-zero and disables the GameObject.
public class NotificationAnimated : MonoBehaviour
{
    [Header("Bobbing")]
    [SerializeField] private float bobAmplitude = 1f;
    [SerializeField] private float bobSpeed = 1f;

    [Header("Rotation")]
    [SerializeField] private float rotateAngle = 5f;
    [SerializeField] private float rotateSpeed = 1f;

    [Header("Pop Animation")]
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popOvershoot = 1.2f;
    [SerializeField] private float settleDuration = 0.08f;

    [Header("Hide Animation")]
    [SerializeField] private float hideDuration = 0.12f;

    private RectTransform rectTransform;
    private Vector3 startPos;
    private float phaseOffset;

    private Vector3 originalScale;
    private bool isAnimating = false;
    private Coroutine animRoutine;

    private float bobRotateStartTime;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;

        phaseOffset = (Random.value < 0.5f) ? 0f : Mathf.PI;
    }

    private void OnEnable()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);

        // Hard reset visuals for a clean “straight” pop
        rectTransform.localScale = Vector3.zero;
        rectTransform.anchoredPosition = startPos;
        rectTransform.localRotation = Quaternion.identity;

        // ensure Update doesn’t move it during pop
        isAnimating = true;

        animRoutine = StartCoroutine(PopThenSettle());
    }

    private void Update()
    {
        if (isAnimating) return;

        float localT = Time.unscaledTime - bobRotateStartTime;

        float yOffset = Mathf.Sin(localT * bobSpeed + phaseOffset) * bobAmplitude;
        rectTransform.anchoredPosition = startPos + new Vector3(0f, yOffset, 0f);

        float zRotation = Mathf.Sin(localT * rotateSpeed + phaseOffset) * rotateAngle;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy) return;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(ScaleTo(Vector3.zero, hideDuration, disableAtEnd: true));
    }

    private IEnumerator PopThenSettle()
    {
        // POP: 0 -> overshoot
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / popDuration);

            float s = EaseOutBack(normalized) * popOvershoot;
            rectTransform.localScale = originalScale * s;

            yield return null;
        }

        Vector3 from = rectTransform.localScale;
        Vector3 to = originalScale;

        float sT = 0f;
        float dur = Mathf.Max(0.0001f, settleDuration);
        while (sT < dur)
        {
            sT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(sT / dur);

            rectTransform.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }

        rectTransform.localScale = originalScale;

        // Reset to a clean baseline so bob/rotate begins in phase after the pop settles.
        rectTransform.anchoredPosition = startPos;
        rectTransform.localRotation = Quaternion.identity;
        bobRotateStartTime = Time.unscaledTime;

        isAnimating = false;
        animRoutine = null;
    }

    private IEnumerator ScaleTo(Vector3 target, float duration, bool disableAtEnd)
    {
        isAnimating = true;

        Vector3 from = rectTransform.localScale;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            rectTransform.localScale = Vector3.Lerp(from, target, k);
            yield return null;
        }

        rectTransform.localScale = target;

        isAnimating = false;
        animRoutine = null;

        if (disableAtEnd)
            gameObject.SetActive(false);
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
    }
}