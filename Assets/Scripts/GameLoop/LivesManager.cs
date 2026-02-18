using System;
using UnityEngine;

public class LivesManager : MonoBehaviour
{
    [SerializeField] private int startingLives = 20;
    [SerializeField] private EnemySpawner enemySpawner;

    public event Action<int> OnLivesChanged;
    public event Action OnGameOver;

    public int Lives { get; private set; }

    private bool gameOverRaised;

    private void Awake()
    {
        startingLives = Mathf.Max(1, startingLives);
        Lives = startingLives;

        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
    }

    private void OnEnable()
    {
        TrySubscribeSpawner();
    }

    private void Start()
    {
        OnLivesChanged?.Invoke(Lives);
    }

    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
        }
    }

    private void TrySubscribeSpawner()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (enemySpawner == null)
        {
            return;
        }

        enemySpawner.OnEnemyReachedGoal -= HandleEnemyReachedGoal;
        enemySpawner.OnEnemyReachedGoal += HandleEnemyReachedGoal;
    }

    private void HandleEnemyReachedGoal(EnemyMovement enemy)
    {
        if (gameOverRaised || Lives <= 0)
        {
            return;
        }

        Lives = Mathf.Max(0, Lives - 1);
        OnLivesChanged?.Invoke(Lives);

        if (enemy != null)
        {
            CentralObjectPool.DespawnEnemy(enemy);
        }

        if (Lives > 0)
        {
            return;
        }

        gameOverRaised = true;
        OnGameOver?.Invoke();
    }

    private void OnValidate()
    {
        startingLives = Mathf.Max(1, startingLives);
    }
}
