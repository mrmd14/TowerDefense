using System.Collections;
using UnityEngine;

public class EnemyGfxManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Flip")]
    [SerializeField] private bool flipByDirection = true;

    [Header("Spawn Fade")]
    [SerializeField] private bool fadeInOnSpawn = true;
    [SerializeField] private float fadeInDuration = 0.35f;

    private Coroutine fadeCoroutine;
    private Color visibleColor = Color.white;
    private bool hasVisibleColor;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        CacheVisibleColor();
    }

    public void PlaySpawnFadeIn()
    {
        if (!fadeInOnSpawn || spriteRenderer == null)
        {
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeInRoutine());
    }

    public void UpdateFlipX(Vector3 toTarget)
    {
        if (!flipByDirection || spriteRenderer == null)
        {
            return;
        }

        if (Mathf.Abs(toTarget.x) <= 0.0001f)
        {
            return;
        }

        spriteRenderer.flipX = toTarget.x < 0f;
    }

    private IEnumerator FadeInRoutine()
    {
        CacheVisibleColor();

        float targetAlpha = visibleColor.a;
        if (targetAlpha <= 0f)
        {
            targetAlpha = 1f;
        }

        float duration = Mathf.Max(0.01f, fadeInDuration);
        float elapsed = 0f;

        SetAlpha(0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(0f, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
        fadeCoroutine = null;
    }

    private void CacheVisibleColor()
    {
        if (spriteRenderer == null || hasVisibleColor)
        {
            return;
        }

        visibleColor = spriteRenderer.color;
        hasVisibleColor = true;
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = visibleColor;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }
}
