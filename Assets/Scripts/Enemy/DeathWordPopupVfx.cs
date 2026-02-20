using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class DeathWordPopupVfx : MonoBehaviour
{
    private enum AnimationPhase
    {
        ScaleUp,
        Hold,
        ScaleDown
    }

    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private string defaultText = "BLOD";
    [SerializeField] private Color defaultTextColor = new Color(1f, 0.89f, 0.35f, 1f);
    [SerializeField] private int defaultSortingOrder = 25;

    [Header("Scale Animation")]
    [SerializeField, Min(0.01f)] private float startScaleMultiplier = 0.2f;
    [SerializeField, Min(0.01f)] private float normalScaleMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float endScaleMultiplier = 0.05f;
    [SerializeField, Min(0.01f)] private float scaleUpDuration = 0.12f;
    [SerializeField, Min(0f)] private float holdDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float scaleDownDuration = 0.18f;
    [SerializeField] private float riseSpeed = 0.3f;

    private AnimationPhase phase;
    private float phaseTimer;
    private Vector3 baseScale;
    private Renderer cachedRenderer;

    private void Awake()
    {
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        }


        ApplyDefaultVisuals();
    }

    private void OnEnable()
    {

        ApplyDefaultVisuals();
        RestartAnimation();
    }

    private void Update()
    {
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
        phaseTimer += Time.deltaTime;

        switch (phase)
        {
            case AnimationPhase.ScaleUp:
                {
                    float duration = Mathf.Max(0.01f, scaleUpDuration);
                    float t = Mathf.Clamp01(phaseTimer / duration);
                    ApplyScale(Mathf.Lerp(startScaleMultiplier, normalScaleMultiplier, t));
                    if (t >= 1f)
                    {
                        phase = AnimationPhase.Hold;
                        phaseTimer = 0f;
                    }

                    break;
                }

            case AnimationPhase.Hold:
                {
                    ApplyScale(normalScaleMultiplier);
                    if (phaseTimer >= Mathf.Max(0f, holdDuration))
                    {
                        phase = AnimationPhase.ScaleDown;
                        phaseTimer = 0f;
                    }

                    break;
                }

            case AnimationPhase.ScaleDown:
                {
                    float duration = Mathf.Max(0.01f, scaleDownDuration);
                    float t = Mathf.Clamp01(phaseTimer / duration);
                    ApplyScale(Mathf.Lerp(normalScaleMultiplier, endScaleMultiplier, t));
                    if (t >= 1f)
                    {
                        CentralObjectPool.Despawn(gameObject);
                    }

                    break;
                }
        }
    }

    public void Play(string word, int sortingOrderOverride)
    {


        if (textMesh != null)
        {
            textMesh.text = string.IsNullOrWhiteSpace(word) ? defaultText : word.Trim();
            textMesh.color = defaultTextColor;
        }

        SetSortingOrder(sortingOrderOverride);
        RestartAnimation();
    }

    private void RestartAnimation()
    {
        phase = AnimationPhase.ScaleUp;
        phaseTimer = 0f;
        ApplyScale(startScaleMultiplier);
    }

    private void ApplyScale(float scaleMultiplier)
    {
        transform.localScale = baseScale * Mathf.Max(0f, scaleMultiplier);
    }

    private void ApplyDefaultVisuals()
    {
        if (textMesh == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(textMesh.text))
        {
            textMesh.text = defaultText;
        }

        textMesh.color = defaultTextColor;
        SetSortingOrder(defaultSortingOrder);
    }

    private void SetSortingOrder(int sortingOrder)
    {
        int resolvedOrder = sortingOrder;
        if (resolvedOrder == int.MinValue)
        {
            resolvedOrder = defaultSortingOrder;
        }

        if (textMesh != null)
        {
            textMesh.sortingOrder = resolvedOrder;
        }

        if (cachedRenderer != null)
        {
            cachedRenderer.sortingOrder = resolvedOrder;
        }
    }


}
