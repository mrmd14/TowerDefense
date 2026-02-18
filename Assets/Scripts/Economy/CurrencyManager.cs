using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startingMoney = 100;
    [SerializeField] private int killReward = 5;

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
        startingMoney = Mathf.Max(0, startingMoney);
        killReward = Mathf.Max(0, killReward);
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

        Add(killReward);
    }
}
