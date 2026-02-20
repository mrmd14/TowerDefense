using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TowerUpgradeButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI costLabel;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private CurrencyManager currencyManager;

    [Header("Placement")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private string costPrefix = "Upgrade: ";

    [Header("Show Animation")]
    [SerializeField, Min(0.01f)] private float showDuration = 0.16f;
    [SerializeField, Min(0f)] private float showStartScaleMultiplier = 0.65f;
    [SerializeField] private AnimationCurve showScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private TowerController2D targetTower;
    private Coroutine showRoutine;
    private CurrencyManager subscribedCurrencyManager;
    private Vector3 panelBaseScale = Vector3.one;





    private void Reset()
    {
        panelRoot = transform as RectTransform;
        upgradeButton = GetComponentInChildren<Button>(true);
        costLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        parentCanvas = GetComponentInParent<Canvas>();
        worldCamera = Camera.main;
        currencyManager = CurrencyManager.Instance ?? FindFirstObjectByType<CurrencyManager>();
    }

    private void Awake()
    {
        ResolveReferences();

        if (panelRoot != null)
        {
            panelBaseScale = panelRoot.localScale;
        }

        Hide();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToCurrency();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
            upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        }
    }

    private void OnDisable()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
        }

        UnsubscribeFromCurrency();

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (panelRoot != null)
        {
            panelRoot.localScale = panelBaseScale;
        }
    }

    private void Update()
    {
        if (targetTower == null)
        {
            return;
        }

        if (!targetTower.CanUpgrade)
        {
            Hide();
            return;
        }

        UpdatePanelPosition();
    }

    public void ShowForTower(TowerController2D tower)
    {
        if (tower == null || !tower.CanUpgrade)
        {
            Hide();
            return;
        }

        ResolveReferences();
        SubscribeToCurrency();

        bool shouldAnimate = targetTower != tower || !IsPanelVisible();
        targetTower = tower;

        UpdatePanelPosition();
        RefreshVisual();
        SetPanelVisible(true);

        if (shouldAnimate)
        {
            PlayShowAnimation();
        }
    }

    public void Hide()
    {
        targetTower = null;

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (panelRoot != null)
        {
            panelRoot.localScale = panelBaseScale;
        }

        SetPanelVisible(false);
    }

    private void HandleUpgradeClicked()
    {
        if (targetTower == null)
        {
            return;
        }

        int nextUpgradeCost = Mathf.Max(0, targetTower.NextUpgradeCost);
        if (nextUpgradeCost > 0 && currencyManager == null)
        {
            RefreshVisual();
            return;
        }

        bool upgraded = targetTower.TryUpgrade(currencyManager);
        if (!upgraded)
        {
            RefreshVisual();
            return;
        }

        if (!targetTower.CanUpgrade)
        {
            Hide();
            return;
        }

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (targetTower == null)
        {
            return;
        }

        int nextUpgradeCost = Mathf.Max(0, targetTower.NextUpgradeCost);
        bool canAfford = nextUpgradeCost <= 0 || (currencyManager != null && currencyManager.CanAfford(nextUpgradeCost));

        if (upgradeButton != null)
        {
            upgradeButton.interactable = canAfford;
        }

        if (costLabel != null)
        {
            costLabel.text = string.Concat(costPrefix, nextUpgradeCost.ToString());
        }
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = transform as RectTransform;
        }

        if (upgradeButton == null)
        {
            upgradeButton = GetComponentInChildren<Button>(true);
        }

        if (costLabel == null)
        {
            costLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (currencyManager == null)
        {
            currencyManager = CurrencyManager.Instance ?? FindFirstObjectByType<CurrencyManager>();
        }
    }

    private void SubscribeToCurrency()
    {
        if (currencyManager == null)
        {
            currencyManager = CurrencyManager.Instance ?? FindFirstObjectByType<CurrencyManager>();
        }

        if (currencyManager == null || subscribedCurrencyManager == currencyManager)
        {
            return;
        }

        UnsubscribeFromCurrency();
        subscribedCurrencyManager = currencyManager;
        subscribedCurrencyManager.OnMoneyChanged += HandleMoneyChanged;
    }

    private void UnsubscribeFromCurrency()
    {
        if (subscribedCurrencyManager == null)
        {
            return;
        }

        subscribedCurrencyManager.OnMoneyChanged -= HandleMoneyChanged;
        subscribedCurrencyManager = null;
    }

    private void HandleMoneyChanged(int _)
    {
        RefreshVisual();
    }

    private void UpdatePanelPosition()
    {
        if (panelRoot == null || targetTower == null)
        {
            return;
        }

        Vector3 desiredWorldPosition = targetTower.transform.position + worldOffset;
        Camera sceneCamera = worldCamera != null ? worldCamera : Camera.main;
        if (sceneCamera == null)
        {
            return;
        }

        RectTransform canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
        if (canvasRect == null)
        {
            panelRoot.position = desiredWorldPosition;
            return;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sceneCamera, desiredWorldPosition);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint))
        {
            panelRoot.anchoredPosition = localPoint;
        }
    }

    private void PlayShowAnimation()
    {
        if (panelRoot == null)
        {
            return;
        }

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(PlayShowAnimationRoutine());
    }

    private IEnumerator PlayShowAnimationRoutine()
    {
        if (panelRoot == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, showDuration);
        float scaleMultiplier = Mathf.Max(0f, showStartScaleMultiplier);
        Vector3 startScale = panelBaseScale * scaleMultiplier;
        Vector3 endScale = panelBaseScale;

        panelRoot.localScale = startScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = showScaleCurve == null ? t : showScaleCurve.Evaluate(t);
            panelRoot.localScale = Vector3.LerpUnclamped(startScale, endScale, curveT);
            yield return null;
        }

        panelRoot.localScale = endScale;
        showRoutine = null;
    }

    private bool IsPanelVisible()
    {
        return panelRoot != null && panelRoot.gameObject.activeSelf;
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot == null)
        {
            return;
        }

        if (panelRoot.gameObject.activeSelf != visible)
        {
            panelRoot.gameObject.SetActive(visible);
        }
    }
}
