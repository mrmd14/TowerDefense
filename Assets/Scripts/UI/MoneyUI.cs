using System.Collections;
using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    public static MoneyUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text moneyLabel;
    [SerializeField] private TMP_Text warningLabel;
    [SerializeField] private float warningDuration = 1.25f;
    [SerializeField] private string warningText = "Not enough money";

    [Header("Money Change Animation")]
    [SerializeField, Min(1f)] private float moneyPopScale = 1.2f;
    [SerializeField, Min(0.01f)] private float moneyScaleLerpDuration = 0.18f;
    [SerializeField] private AnimationCurve moneyScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private CurrencyManager subscribedCurrencyManager;
    private Coroutine warningRoutine;
    private Coroutine moneyChangeRoutine;
    private Vector3 moneyBaseScale = Vector3.one;
    private int displayedMoney;
    private bool hasDisplayedMoney;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (moneyLabel != null)
        {
            moneyBaseScale = moneyLabel.rectTransform.localScale;
        }

        if (warningLabel != null)
        {
            warningLabel.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        TrySubscribeToCurrency();
    }

    private void Start()
    {
        TrySubscribeToCurrency();
    }

    private void OnDisable()
    {
        UnsubscribeFromCurrency();

        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
            warningRoutine = null;
        }

        if (moneyChangeRoutine != null)
        {
            StopCoroutine(moneyChangeRoutine);
            moneyChangeRoutine = null;
        }

        if (moneyLabel != null)
        {
            moneyLabel.rectTransform.localScale = moneyBaseScale;
        }

        if (warningLabel != null)
        {
            warningLabel.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowNotEnoughMoney()
    {
        if (warningLabel == null)
        {
            Debug.Log("Not enough money");
            return;
        }

        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
        }

        warningRoutine = StartCoroutine(ShowWarningRoutine());
    }

    private void TrySubscribeToCurrency()
    {
        if (subscribedCurrencyManager != null)
        {
            return;
        }

        CurrencyManager currencyManager = CurrencyManager.Instance;
        if (currencyManager == null)
        {
            currencyManager = FindFirstObjectByType<CurrencyManager>();
        }

        if (currencyManager == null)
        {
            return;
        }

        subscribedCurrencyManager = currencyManager;
        subscribedCurrencyManager.OnMoneyChanged += HandleMoneyChanged;
        HandleMoneyChanged(subscribedCurrencyManager.Money);
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

    private void HandleMoneyChanged(int newMoney)
    {
        if (moneyLabel == null)
        {
            return;
        }

        if (!hasDisplayedMoney)
        {
            hasDisplayedMoney = true;
            displayedMoney = newMoney;
            SetMoneyLabel(newMoney);
            return;
        }

        if (newMoney == displayedMoney)
        {
            return;
        }

        SetMoneyLabel(newMoney);

        if (moneyChangeRoutine != null)
        {
            StopCoroutine(moneyChangeRoutine);
        }

        moneyChangeRoutine = StartCoroutine(AnimateMoneyScaleRoutine());
    }

    private IEnumerator ShowWarningRoutine()
    {
        warningLabel.text = warningText;
        warningLabel.gameObject.SetActive(true);

        yield return new WaitForSeconds(Mathf.Max(0.1f, warningDuration));

        warningLabel.gameObject.SetActive(false);
        warningRoutine = null;
    }

    private IEnumerator AnimateMoneyScaleRoutine()
    {
        RectTransform labelRect = moneyLabel.rectTransform;
        Vector3 targetScale = moneyBaseScale;
        Vector3 popScale = moneyBaseScale * Mathf.Max(1f, moneyPopScale);
        labelRect.localScale = popScale;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, moneyScaleLerpDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedT = Mathf.Clamp01(elapsed / duration);
            float curveT = moneyScaleCurve == null ? normalizedT : moneyScaleCurve.Evaluate(normalizedT);
            labelRect.localScale = Vector3.LerpUnclamped(popScale, targetScale, curveT);

            yield return null;
        }

        labelRect.localScale = targetScale;
        moneyChangeRoutine = null;
    }

    private void SetMoneyLabel(int value)
    {
        displayedMoney = value;
        moneyLabel.text = value.ToString();
    }
}
