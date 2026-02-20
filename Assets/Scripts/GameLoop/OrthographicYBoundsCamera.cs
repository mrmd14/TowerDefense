using System.Collections;
using UnityEngine;

public class OrthographicYBoundsCamera : MonoBehaviour
{
    public static OrthographicYBoundsCamera Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Y Bounds (World Space)")]
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;
    [SerializeField, Min(0f)] private float verticalPadding = 0f;
    [SerializeField] private bool lockCameraYToBoundsCenter = true;

    [Header("Startup Zoom Out")]
    [Tooltip("Extra orthographic size applied at start before settling to fit size.")]
    [SerializeField, Min(0f)] private float startupZoomOutAmount = 1f;
    [SerializeField, Min(0f)] private float startupZoomDuration = 0.6f;

    [Header("Runtime")]
    [Tooltip("Recompute every frame if bounds can change at runtime.")]
    [SerializeField] private bool updateContinuously = false;

    [Header("Impact Shake")]
    [SerializeField, Min(0f)] private float defaultShakeDuration = 0.16f;
    [SerializeField, Min(0f)] private float defaultShakeStrength = 0.18f;
    [SerializeField, Min(0f)] private float maxShakeOffset = 0.35f;

    private float targetOrthoSize;
    private Coroutine zoomRoutine;
    private float shakeTimeRemaining;
    private float shakeDuration;
    private float shakeStrength;
    private Vector3 currentShakeOffset;

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple {nameof(OrthographicYBoundsCamera)} instances found. Replacing previous instance.", this);
        }

        Instance = this;

        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            Debug.LogWarning($"{nameof(OrthographicYBoundsCamera)} requires a Camera reference.", this);
            return;
        }

        targetCamera.orthographic = true;
        RecalculateTarget();

        bool shouldAnimateStartupZoom = startupZoomDuration > 0f && startupZoomOutAmount > 0f;
        if (shouldAnimateStartupZoom)
        {
            float startSize = targetOrthoSize + startupZoomOutAmount;
            targetCamera.orthographicSize = startSize;
            zoomRoutine = StartCoroutine(AnimateStartupZoom(startSize, targetOrthoSize, startupZoomDuration));
        }
        else
        {
            targetCamera.orthographicSize = targetOrthoSize;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (updateContinuously)
        {
            RecalculateTarget();
            if (zoomRoutine == null)
            {
                targetCamera.orthographicSize = targetOrthoSize;
            }
        }

        ApplyShakeOffset();
    }

    [ContextMenu("Apply Bounds Now")]
    public void ApplyBoundsNow()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            return;
        }

        ClearShakeOffset();

        if (zoomRoutine != null)
        {
            StopCoroutine(zoomRoutine);
            zoomRoutine = null;
        }

        targetCamera.orthographic = true;
        RecalculateTarget();
        targetCamera.orthographicSize = targetOrthoSize;
    }

    public void SetYBounds(float newMinY, float newMaxY)
    {
        minY = newMinY;
        maxY = newMaxY;
        ApplyBoundsNow();
    }

    public void TriggerDefaultShake()
    {
        TriggerShake(defaultShakeDuration, defaultShakeStrength);
    }

    public void TriggerShake(float duration, float strength)
    {
        float requestedDuration = Mathf.Max(0f, duration);
        float requestedStrength = Mathf.Max(0f, strength);
        if (requestedDuration <= 0f || requestedStrength <= 0f)
        {
            return;
        }

        if (shakeTimeRemaining > 0f)
        {
            shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, requestedDuration);
            shakeDuration = Mathf.Max(shakeDuration, shakeTimeRemaining);
            shakeStrength = Mathf.Max(shakeStrength, requestedStrength);
            return;
        }

        shakeTimeRemaining = requestedDuration;
        shakeDuration = requestedDuration;
        shakeStrength = requestedStrength;
    }

    private void RecalculateTarget()
    {
        float lower = Mathf.Min(minY, maxY);
        float upper = Mathf.Max(minY, maxY);
        float centerY = (lower + upper) * 0.5f;
        float halfRange = (upper - lower) * 0.5f;

        targetOrthoSize = Mathf.Max(0.01f, halfRange + verticalPadding);

        if (!lockCameraYToBoundsCenter)
        {
            return;
        }

        Vector3 pos = targetCamera.transform.position;
        pos.y = centerY;
        targetCamera.transform.position = pos;
    }

    private void ApplyShakeOffset()
    {
        Transform cameraTransform = targetCamera.transform;
        Vector3 basePosition = cameraTransform.position - currentShakeOffset;
        currentShakeOffset = Vector3.zero;

        if (shakeTimeRemaining > 0f)
        {
            shakeTimeRemaining = Mathf.Max(0f, shakeTimeRemaining - Time.deltaTime);

            float normalizedTime = shakeDuration > 0f ? (shakeTimeRemaining / shakeDuration) : 0f;
            float currentStrength = shakeStrength * Mathf.Clamp01(normalizedTime);
            Vector2 randomOffset = Random.insideUnitCircle * currentStrength;

            if (maxShakeOffset > 0f)
            {
                randomOffset = Vector2.ClampMagnitude(randomOffset, maxShakeOffset);
            }

            currentShakeOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
        }

        cameraTransform.position = basePosition + currentShakeOffset;
    }

    private void ClearShakeOffset()
    {
        if (targetCamera == null)
        {
            currentShakeOffset = Vector3.zero;
            return;
        }

        if (currentShakeOffset == Vector3.zero)
        {
            return;
        }

        targetCamera.transform.position -= currentShakeOffset;
        currentShakeOffset = Vector3.zero;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private IEnumerator AnimateStartupZoom(float fromSize, float toSize, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - (1f - t) * (1f - t);
            targetCamera.orthographicSize = Mathf.Lerp(fromSize, toSize, t);
            yield return null;
        }

        targetCamera.orthographicSize = toSize;
        zoomRoutine = null;
    }
}
