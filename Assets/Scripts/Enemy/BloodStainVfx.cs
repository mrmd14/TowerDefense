using UnityEngine;

[DisallowMultipleComponent]
public class BloodStainVfx : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Min(0f)] private float visibleDelay = 0.25f;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.55f;
    [SerializeField, Min(0.01f)] private float minScale = 0.9f;
    [SerializeField, Min(0.01f)] private float maxScale = 1.2f;

    private float timer;
    private bool isFading;
    private Vector3 baseScale;
    private Color baseColor;

    private void Awake()
    {
        ResolveRenderer();
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        ResolveRenderer();

        if (baseScale == Vector3.zero)
        {
            baseScale = Vector3.one;
        }

        timer = 0f;
        isFading = false;

        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
            baseColor.a = 1f;
            spriteRenderer.color = baseColor;
        }

        float randomScale = Random.Range(Mathf.Min(minScale, maxScale), Mathf.Max(minScale, maxScale));
        transform.localScale = baseScale * randomScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (!isFading)
        {
            if (timer < Mathf.Max(0f, visibleDelay))
            {
                return;
            }

            isFading = true;
            timer = 0f;
        }

        float duration = Mathf.Max(0.05f, fadeDuration);
        float progress = Mathf.Clamp01(timer / duration);
        float alpha = 1f - progress;
        SetAlpha(alpha);

        if (progress >= 1f)
        {
            CentralObjectPool.Despawn(gameObject);
        }
    }

    public void SetSortingOrder(int sortingOrder)
    {
        ResolveRenderer();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    private void ResolveRenderer()
    {
        if (spriteRenderer != null)
        {
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }
}
