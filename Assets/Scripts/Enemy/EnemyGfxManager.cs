using System.Collections;
using UnityEngine;

public class EnemyGfxManager : MonoBehaviour
{
    private const string HitFlashPropertyName = "_HitFlash";

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Flip")]
    [SerializeField] private bool flipByDirection = true;

    [Header("Spawn Fade")]
    [SerializeField] private bool fadeInOnSpawn = true;
    [SerializeField] private float fadeInDuration = 0.35f;

    [Header("Hit Flash")]
    [SerializeField] private float hitFlashDuration = 0.08f;

    private Coroutine fadeCoroutine;
    private Coroutine hitFlashCoroutine;
    private Color visibleColor = Color.white;
    private bool hasVisibleColor;
    private MaterialPropertyBlock propertyBlock;
    private int hitFlashPropertyId;
    private bool materialSupportsHitFlash;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
        hitFlashPropertyId = Shader.PropertyToID(HitFlashPropertyName);
        materialSupportsHitFlash = spriteRenderer != null
            && spriteRenderer.sharedMaterial != null
            && spriteRenderer.sharedMaterial.HasProperty(hitFlashPropertyId);

        CacheVisibleColor();
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }

        if (spriteRenderer != null && hasVisibleColor)
        {
            spriteRenderer.color = visibleColor;
        }

        SetHitFlashAmount(0f);
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

    public void PlayHitFlash()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }

        hitFlashCoroutine = StartCoroutine(materialSupportsHitFlash
            ? HitFlashRoutine()
            : FallbackHitFlashRoutine());
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

    private IEnumerator HitFlashRoutine()
    {
        SetHitFlashAmount(1f);

        float duration = Mathf.Max(0.01f, hitFlashDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetHitFlashAmount(1f - t);
            yield return null;
        }

        SetHitFlashAmount(0f);
        hitFlashCoroutine = null;
    }

    private IEnumerator FallbackHitFlashRoutine()
    {
        CacheVisibleColor();

        Color original = spriteRenderer.color;
        Color flash = original;
        flash.r = 1f;
        flash.g = 1f;
        flash.b = 1f;
        spriteRenderer.color = flash;

        float duration = Mathf.Max(0.01f, hitFlashDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            spriteRenderer.color = Color.Lerp(flash, original, t);
            yield return null;
        }

        spriteRenderer.color = original;
        hitFlashCoroutine = null;
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

    private void SetHitFlashAmount(float value)
    {
        if (!materialSupportsHitFlash || spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(hitFlashPropertyId, Mathf.Clamp01(value));
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}
