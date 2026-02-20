using System.Collections;
using UnityEngine;

public class BombTowerController2D : TowerController2D
{
    [Header("Shoot Fade Visual")]
    [SerializeField] private SpriteRenderer fadeSpriteRenderer;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;
    [SerializeField, Min(0f)] private float fadeRiseDistance = 0.35f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Coroutine fadeCoroutine;

    private void Reset()
    {
        if (fadeSpriteRenderer == null)
        {
            fadeSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetAlpha(0f);
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    protected override void Shoot(Transform target)
    {
        TriggerFade();
        base.Shoot(target);
    }

    private void TriggerFade()
    {
        if (fadeSpriteRenderer == null)
        {
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeSpriteRenderer.transform.position = FirePointPosition;
        SetAlpha(1f);
        fadeCoroutine = StartCoroutine(FadeToZeroRoutine(fadeSpriteRenderer.transform.position));
    }

    private IEnumerator FadeToZeroRoutine(Vector3 startPosition)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Clamp01(fadeCurve.Evaluate(t));
            fadeSpriteRenderer.transform.position = startPosition + (Vector3.up * (fadeRiseDistance * t));
            SetAlpha(alpha);
            yield return null;
        }

        fadeSpriteRenderer.transform.position = startPosition + (Vector3.up * fadeRiseDistance);
        SetAlpha(0f);
        fadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (fadeSpriteRenderer == null)
        {
            return;
        }

        Color color = fadeSpriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        fadeSpriteRenderer.color = color;
    }
}
