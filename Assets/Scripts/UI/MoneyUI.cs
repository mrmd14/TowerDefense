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

    private CurrencyManager subscribedCurrencyManager;
    private Coroutine warningRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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

        moneyLabel.text = $"Money: {newMoney}";
    }

    private IEnumerator ShowWarningRoutine()
    {
        warningLabel.text = warningText;
        warningLabel.gameObject.SetActive(true);

        yield return new WaitForSeconds(Mathf.Max(0.1f, warningDuration));

        warningLabel.gameObject.SetActive(false);
        warningRoutine = null;
    }
}
