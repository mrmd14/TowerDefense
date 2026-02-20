using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class TowerBuildButtonVisual : MonoBehaviour
{
    [SerializeField] private TowerData towerData;
    [SerializeField] private Image buttonImage;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private TMP_Text costLabel;


    private void Reset()
    {
        buttonImage = GetComponent<Image>();
        currencyManager = CurrencyManager.Instance ?? FindFirstObjectByType<CurrencyManager>();

        if (buttonImage != null)
        {
            fallbackSprite = buttonImage.sprite;
        }
    }

    private void Awake()
    {
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        if (fallbackSprite == null && buttonImage != null)
        {
            fallbackSprite = buttonImage.sprite;
        }

        ResolveCurrencyManager();

    }

    private void OnEnable()
    {
        ResolveCurrencyManager();

        if (currencyManager != null)
        {
            currencyManager.OnMoneyChanged -= HandleMoneyChanged;
            currencyManager.OnMoneyChanged += HandleMoneyChanged;
        }

        RefreshVisual();
    }

    private void OnDisable()
    {
        if (currencyManager != null)
        {
            currencyManager.OnMoneyChanged -= HandleMoneyChanged;
        }
    }

    public void RefreshVisual()
    {
        if (towerData == null)
        {
            return;
        }

        bool canAfford = currencyManager == null || currencyManager.CanAfford(towerData.Cost);

        if (buttonImage != null)
        {
            Sprite desiredSprite = canAfford
                ? towerData.AffordableButtonSprite
                : towerData.UnaffordableButtonSprite;



            if (desiredSprite != null && buttonImage.sprite != desiredSprite)
            {
                buttonImage.sprite = desiredSprite;
            }
        }

        UpdateCostLabel(towerData.Cost);
    }

    private void ResolveCurrencyManager()
    {
        if (currencyManager == null)
        {
            currencyManager = CurrencyManager.Instance ?? FindFirstObjectByType<CurrencyManager>();
        }
    }

    private void HandleMoneyChanged(int _)
    {
        RefreshVisual();
    }

    private void UpdateCostLabel(int cost)
    {

        if (costLabel == null)
        {
            return;
        }

        costLabel.text = cost.ToString();
    }


}
