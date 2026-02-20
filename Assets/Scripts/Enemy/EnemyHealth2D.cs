using System;
using UnityEngine;

public class EnemyHealth2D : MonoBehaviour, IDamageable
{
    public static event Action<EnemyHealth2D> OnAnyEnemyDied;

    [Header("Health")]
    [SerializeField] private int maxHP = 3;
    [SerializeField] private int currentHP;
    [SerializeField] private EnemyGfxManager enemyGfxManager;

    [Header("Reward")]
    [SerializeField, Min(0)] private int killReward = 5;

    [Header("Health Scale")]
    [SerializeField] private Transform healthScaleTarget;

    public event Action<EnemyHealth2D> OnDied;

    public int CurrentHP => currentHP;
    public int KillReward => Mathf.Max(0, killReward);

    private void Awake()
    {
        maxHP = Mathf.Max(1, maxHP);
        currentHP = maxHP;
        ResolveGfxManager();
        UpdateHealthScaleVisual();
    }

    private void OnEnable()
    {
        currentHP = Mathf.Max(1, maxHP);
        ResolveGfxManager();
        UpdateHealthScaleVisual();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHP <= 0)
        {
            return;
        }

        currentHP -= amount;
        enemyGfxManager?.PlayHitFlash();
        UpdateHealthScaleVisual();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        currentHP = 0;
        UpdateHealthScaleVisual();
        OnDied?.Invoke(this);
        OnAnyEnemyDied?.Invoke(this);
        CentralObjectPool.Despawn(gameObject);
    }

    private void OnValidate()
    {
        maxHP = Mathf.Max(1, maxHP);
        killReward = Mathf.Max(0, killReward);

        if (!Application.isPlaying)
        {
            currentHP = maxHP;
        }

        UpdateHealthScaleVisual();
    }

    private void ResolveGfxManager()
    {
        if (enemyGfxManager != null)
        {
            return;
        }

        enemyGfxManager = GetComponentInChildren<EnemyGfxManager>();
    }

    private void UpdateHealthScaleVisual()
    {
        if (healthScaleTarget == null)
        {
            return;
        }

        float healthPercent = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;
        Vector3 localScale = healthScaleTarget.localScale;
        localScale.x = healthPercent;
        healthScaleTarget.localScale = localScale;
    }
}
