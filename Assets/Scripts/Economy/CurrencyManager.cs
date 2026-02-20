using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Gameplay Data")]
    [SerializeField] private GamePlayData gamePlayData;


    private int startingMoney = 100;

    public event Action<int> OnMoneyChanged;

    public int Money { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        int configuredStartingMoney = gamePlayData != null ? gamePlayData.StartingMoney : startingMoney;
        startingMoney = Mathf.Max(0, configuredStartingMoney);
        Money = startingMoney;
    }

    private void OnEnable()
    {
        EnemyHealth2D.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void Start()
    {
        OnMoneyChanged?.Invoke(Money);
    }

    private void OnDisable()
    {
        EnemyHealth2D.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool CanAfford(int cost)
    {
        return cost <= 0 || Money >= cost;
    }

    public bool TrySpend(int cost)
    {
        if (cost <= 0)
        {
            return true;
        }

        if (!CanAfford(cost))
        {
            return false;
        }

        Money -= cost;
        OnMoneyChanged?.Invoke(Money);
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Money += amount;
        OnMoneyChanged?.Invoke(Money);
    }

    private void HandleEnemyDied(EnemyHealth2D enemy)
    {
        if (enemy == null)
        {
            return;
        }

        Add(enemy.KillReward);
    }
}
