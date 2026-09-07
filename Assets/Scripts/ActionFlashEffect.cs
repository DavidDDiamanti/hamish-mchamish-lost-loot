using UnityEngine;
using System.Collections;

// ActionFlashEffect: brief sprite flash played on the PlayerActionArea when the player clicks.
// Play() fades the SpriteRenderer in then back out over totalDuration, splitting time equally.
// Used as a visual click-feedback indicator; excluded from Player's invulnerability flash by
// filtering it out in Player.Awake().
public class ActionFlashEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Coroutine routine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        SetAlpha(0f);
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        SetAlpha(0f);
    }

    public void Play(float totalDuration)
    {
        if (!isActiveAndEnabled) return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        Debug.Log($"Playing flash effect with duration {totalDuration}");

        float phaseDuration = totalDuration / 2f;
        routine = StartCoroutine(PlayRoutine(phaseDuration, 0f, phaseDuration));
    }

    private IEnumerator PlayRoutine(float fadeInTime, float holdTime, float fadeOutTime)
    {
        yield return Fade(0f, 1f, fadeInTime);
        yield return new WaitForSeconds(holdTime);
        yield return Fade(1f, 0f, fadeOutTime);

        SetAlpha(0f);
        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float t = 0f;

        while (t < duration)
        {
            if (!isActiveAndEnabled)
            {
                SetAlpha(0f);
                routine = null;
                yield break;
            }

            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (spriteRenderer == null) return;

        Color c = spriteRenderer.color;
        c.a = Mathf.Clamp01(a);
        spriteRenderer.color = c;
    }
}
