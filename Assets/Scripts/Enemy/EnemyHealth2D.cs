using System;
using UnityEngine;

public class EnemyHealth2D : MonoBehaviour, IDamageable
{
    public static event Action<EnemyHealth2D> OnAnyEnemyDied;

    [Header("Health")]
    [SerializeField] private int maxHP = 3;
    [SerializeField] private int currentHP;

    public event Action<EnemyHealth2D> OnDied;

    public int CurrentHP => currentHP;

    private void Awake()
    {
        maxHP = Mathf.Max(1, maxHP);
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHP <= 0)
        {
            return;
        }

        currentHP -= amount;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        currentHP = 0;
        OnDied?.Invoke(this);
        OnAnyEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        maxHP = Mathf.Max(1, maxHP);

        if (!Application.isPlaying)
        {
            currentHP = maxHP;
        }
    }
}
