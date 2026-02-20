using System.Collections;
using TMPro;
using UnityEngine;

public class WaveStartFeedbackUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private TMP_Text waveStartLabel;
    [SerializeField] private GameObject feedbackVisual;
    [SerializeField] private RectTransform feedbackRect;

    [Header("Text")]
    [SerializeField] private string waveLabelPrefix = "Wave ";
    [SerializeField] private bool showTotalWaves;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float scaleUpDuration = 0.2f;
    [SerializeField] private Vector3 scaleUpFromScale = Vector3.one * 0.8f;
    [SerializeField] private AnimationCurve scaleUpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float holdBeforeScaleDown = 0.25f;
    [SerializeField, Min(0.01f)] private float animationDuration = 0.75f;
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 endScale = Vector3.zero;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }

        if (waveStartLabel == null)
        {
            waveStartLabel = GetComponentInChildren<TMP_Text>(true);
        }

        if (feedbackVisual == null && waveStartLabel != null)
        {
            feedbackVisual = waveStartLabel.gameObject;
        }

        if (feedbackRect == null && feedbackVisual != null)
        {
            feedbackRect = feedbackVisual.GetComponent<RectTransform>();
        }

        HideFeedback();
    }

    private void OnEnable()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }

        if (waveManager != null)
        {
            waveManager.OnWaveStarted -= HandleWaveStarted;
            waveManager.OnWaveStarted += HandleWaveStarted;
        }

        HideFeedback();
    }

    private void OnDisable()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveStarted -= HandleWaveStarted;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }
    }

    private void HandleWaveStarted(int currentWave, int totalWaves)
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(PlayFeedbackRoutine(currentWave, totalWaves));
    }

    private IEnumerator PlayFeedbackRoutine(int currentWave, int totalWaves)
    {
        if (waveStartLabel != null)
        {
            if (showTotalWaves)
            {
                waveStartLabel.text = $"{waveLabelPrefix}{currentWave}/{totalWaves}";
            }
            else
            {
                waveStartLabel.text = $"{waveLabelPrefix}{currentWave}";
            }
        }

        if (feedbackVisual != null)
        {
            feedbackVisual.SetActive(true);
        }

        if (feedbackRect != null)
        {
            feedbackRect.localScale = scaleUpDuration > 0f ? scaleUpFromScale : startScale;
        }

        if (scaleUpDuration > 0f)
        {
            float scaleUpElapsed = 0f;

            while (scaleUpElapsed < scaleUpDuration)
            {
                scaleUpElapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(scaleUpElapsed / scaleUpDuration);
                float curveValue = Mathf.Clamp01(scaleUpCurve.Evaluate(normalizedTime));

                if (feedbackRect != null)
                {
                    feedbackRect.localScale = Vector3.LerpUnclamped(scaleUpFromScale, startScale, curveValue);
                }

                yield return null;
            }

            if (feedbackRect != null)
            {
                feedbackRect.localScale = startScale;
            }
        }

        if (holdBeforeScaleDown > 0f)
        {
            yield return new WaitForSeconds(holdBeforeScaleDown);
        }

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / animationDuration);
            float curveValue = Mathf.Clamp01(scaleCurve.Evaluate(normalizedTime));

            if (feedbackRect != null)
            {
                feedbackRect.localScale = Vector3.LerpUnclamped(startScale, endScale, curveValue);
            }

            yield return null;
        }

        if (feedbackRect != null)
        {
            feedbackRect.localScale = endScale;
        }

        HideFeedback();
        feedbackRoutine = null;
    }

    private void HideFeedback()
    {
        if (feedbackVisual != null)
        {
            feedbackVisual.SetActive(false);
        }
    }
}
