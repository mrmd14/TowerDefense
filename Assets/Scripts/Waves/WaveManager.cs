using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private LivesManager livesManager;

    [Header("Waves")]
    [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();
    [SerializeField] private bool autoStartWaves = true;
    [SerializeField, Min(0f)] private float autoStartDelaySeconds = 0f;

    public event Action<int, int> OnWaveStarted;
    public event Action<int, int> OnWaveCompleted;

    public bool IsWaveActive { get; private set; }
    public bool IsGameOver => isGameOver;
    public int CurrentWaveIndex => currentWaveIndex;
    public int TotalWaves => waves?.Count ?? 0;
    public int CurrentWaveNumber => currentWaveIndex >= 0 ? currentWaveIndex + 1 : 0;
    public bool HasMoreWaves => currentWaveIndex + 1 < TotalWaves;

    private int currentWaveIndex = -1;
    private bool spawnFinished;
    private bool isGameOver;
    private Coroutine autoStartCoroutine;

    private void Awake()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (livesManager == null)
        {
            livesManager = FindFirstObjectByType<LivesManager>();
        }
    }

    private void OnEnable()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (livesManager == null)
        {
            livesManager = FindFirstObjectByType<LivesManager>();
        }

        if (enemySpawner != null)
        {
            enemySpawner.OnWaveSpawnCompleted += HandleWaveSpawnCompleted;
            enemySpawner.OnEnemyRemoved += HandleEnemyRemoved;
            enemySpawner.OnAliveEnemyCountChanged += HandleAliveEnemyCountChanged;
        }

        if (livesManager != null)
        {
            livesManager.OnGameOver += HandleGameOver;
        }

        QueueAutoStartNextWave();
    }

    private void OnDisable()
    {
        if (autoStartCoroutine != null)
        {
            StopCoroutine(autoStartCoroutine);
            autoStartCoroutine = null;
        }

        if (enemySpawner != null)
        {
            enemySpawner.OnWaveSpawnCompleted -= HandleWaveSpawnCompleted;
            enemySpawner.OnEnemyRemoved -= HandleEnemyRemoved;
            enemySpawner.OnAliveEnemyCountChanged -= HandleAliveEnemyCountChanged;
        }

        if (livesManager != null)
        {
            livesManager.OnGameOver -= HandleGameOver;
        }
    }

    public void StartWave()
    {
        if (autoStartCoroutine != null)
        {
            StopCoroutine(autoStartCoroutine);
            autoStartCoroutine = null;
        }

        if (isGameOver || IsWaveActive || !HasMoreWaves || enemySpawner == null)
        {
            return;
        }

        int nextWaveIndex = currentWaveIndex + 1;
        WaveConfig nextWave = waves[nextWaveIndex];

        currentWaveIndex = nextWaveIndex;
        IsWaveActive = true;
        spawnFinished = false;
        OnWaveStarted?.Invoke(CurrentWaveNumber, TotalWaves);

        if (nextWave == null)
        {
            Debug.LogWarning($"Wave {CurrentWaveNumber} is null and will complete immediately.", this);
            spawnFinished = true;
            TryCompleteWave();
            return;
        }

        enemySpawner.StartSpawningWave(nextWave);
    }

    private void HandleWaveSpawnCompleted()
    {
        spawnFinished = true;
        TryCompleteWave();
    }

    private void HandleEnemyRemoved(EnemyMovement enemy)
    {
        TryCompleteWave();
    }

    private void HandleAliveEnemyCountChanged(int aliveCount)
    {
        TryCompleteWave();
    }

    private void TryCompleteWave()
    {
        if (!IsWaveActive || !spawnFinished || enemySpawner == null)
        {
            return;
        }

        if (enemySpawner.AliveEnemiesCount > 0)
        {
            return;
        }

        IsWaveActive = false;
        OnWaveCompleted?.Invoke(CurrentWaveNumber, TotalWaves);
        QueueAutoStartNextWave();
    }

    private void HandleGameOver()
    {
        isGameOver = true;
        IsWaveActive = false;
        if (autoStartCoroutine != null)
        {
            StopCoroutine(autoStartCoroutine);
            autoStartCoroutine = null;
        }
        enemySpawner?.StopSpawning();
    }

    private void QueueAutoStartNextWave()
    {
        if (!autoStartWaves || isGameOver || IsWaveActive || !HasMoreWaves)
        {
            return;
        }

        if (autoStartCoroutine != null)
        {
            return;
        }

        autoStartCoroutine = StartCoroutine(AutoStartNextWaveRoutine(autoStartDelaySeconds));
    }

    private IEnumerator AutoStartNextWaveRoutine(float delaySeconds)
    {
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }
        else
        {
            yield return null;
        }

        autoStartCoroutine = null;
        StartWave();
    }
}
