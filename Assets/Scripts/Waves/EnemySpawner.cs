using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private struct SpawnInstruction
    {
        public SpawnInstruction(EnemyType enemyType, GameObject prefab, int count)
        {
            EnemyType = enemyType;
            Prefab = prefab;
            Count = Mathf.Max(0, count);
        }

        public EnemyType EnemyType { get; }
        public GameObject Prefab { get; }
        public int Count { get; }
    }

    [Header("References")]
    [SerializeField] private PathManager pathManager;

    public event Action<EnemyMovement> OnEnemySpawned;
    public event Action<EnemyMovement> OnEnemyReachedGoal;
    public event Action<EnemyMovement> OnEnemyRemoved;
    public event Action OnWaveSpawnCompleted;
    public event Action<int> OnAliveEnemyCountChanged;

    public int SpawnedEnemiesCount { get; private set; }
    public bool IsSpawning => spawnRoutine != null;

    public int AliveEnemiesCount
    {
        get
        {
            PruneDestroyedEnemies();
            return trackedEnemies.Count;
        }
    }

    private readonly HashSet<EnemyMovement> trackedEnemies = new HashSet<EnemyMovement>();
    private Coroutine spawnRoutine;

    private void Awake()
    {
        if (pathManager == null)
        {
            pathManager = PathManager.Instance ?? FindFirstObjectByType<PathManager>();
        }
    }

    private void OnDisable()
    {
        StopSpawning();
        ClearTrackedEnemies();
    }

    public void StartSpawningWave(WaveConfig wave)
    {
        StartSpawningWave(wave, null);
    }

    public void StartSpawningWave(WaveConfig wave, GameObject enemyPrefabOverride)
    {
        if (wave == null)
        {
            Debug.LogError("EnemySpawner cannot spawn a null wave.", this);
            OnWaveSpawnCompleted?.Invoke();
            return;
        }

        if (pathManager == null)
        {
            pathManager = PathManager.Instance ?? FindFirstObjectByType<PathManager>();
        }

        if (pathManager == null || pathManager.WaypointCount == 0)
        {
            Debug.LogError("EnemySpawner requires PathManager with at least one waypoint.", this);
            OnWaveSpawnCompleted?.Invoke();
            return;
        }

        StopSpawning();
        SpawnedEnemiesCount = 0;

        List<SpawnInstruction> spawnInstructions = BuildSpawnInstructions(wave, enemyPrefabOverride);
        if (spawnInstructions.Count == 0)
        {
            OnWaveSpawnCompleted?.Invoke();
            return;
        }

        spawnRoutine = StartCoroutine(SpawnRoutine(wave, spawnInstructions));
    }

    public void StopSpawning()
    {
        if (spawnRoutine == null)
        {
            return;
        }

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private IEnumerator SpawnRoutine(WaveConfig wave, List<SpawnInstruction> spawnInstructions)
    {
        if (wave.StartDelay > 0f)
        {
            yield return new WaitForSeconds(wave.StartDelay);
        }

        Vector3 spawnPosition = pathManager.GetWaypointPosition(0);
        float duration = wave.WaveDuration;
        bool useDurationLoop = duration > 0f;
        float endTime = Time.time + duration;

        do
        {
            for (int instructionIndex = 0; instructionIndex < spawnInstructions.Count; instructionIndex++)
            {
                SpawnInstruction instruction = spawnInstructions[instructionIndex];
                if (instruction.Prefab == null || instruction.Count <= 0)
                {
                    continue;
                }

                for (int countIndex = 0; countIndex < instruction.Count; countIndex++)
                {
                    if (useDurationLoop && Time.time >= endTime)
                    {
                        break;
                    }

                    SpawnEnemy(instruction.Prefab, spawnPosition);

                    bool isLastInstruction = instructionIndex >= spawnInstructions.Count - 1;
                    bool isLastCount = countIndex >= instruction.Count - 1;
                    bool shouldWait = !isLastInstruction || !isLastCount || useDurationLoop;
                    if (!shouldWait)
                    {
                        continue;
                    }

                    if (wave.SpawnInterval > 0f)
                    {
                        float waitDuration = wave.SpawnInterval;
                        if (useDurationLoop)
                        {
                            float remainingTime = endTime - Time.time;
                            if (remainingTime <= 0f)
                            {
                                break;
                            }

                            waitDuration = Mathf.Min(waitDuration, remainingTime);
                        }

                        yield return new WaitForSeconds(waitDuration);
                    }
                    else if (useDurationLoop)
                    {
                        // Prevent a tight loop when duration mode uses zero interval.
                        yield return null;
                    }
                }
            }
        }
        while (useDurationLoop && Time.time < endTime);

        spawnRoutine = null;
        OnWaveSpawnCompleted?.Invoke();
    }

    private void SpawnEnemy(GameObject enemyPrefab, Vector3 spawnPosition)
    {
        GameObject enemyObject = CentralObjectPool.Spawn(enemyPrefab, spawnPosition, Quaternion.identity);
        if (enemyObject == null)
        {
            return;
        }

        EnemyMovement movement = enemyObject.GetComponent<EnemyMovement>();

        if (movement != null)
        {
            TrackEnemy(movement);
            OnEnemySpawned?.Invoke(movement);
        }
        else
        {
            Debug.LogWarning("Spawned enemy is missing EnemyMovement. It will not be tracked.", enemyObject);
        }

        SpawnedEnemiesCount++;
    }

    private List<SpawnInstruction> BuildSpawnInstructions(WaveConfig wave, GameObject enemyPrefabOverride)
    {
        List<SpawnInstruction> instructions = new List<SpawnInstruction>();
        IReadOnlyList<WaveEnemyEntry> waveEnemies = wave.Enemies;

        if (waveEnemies != null)
        {
            for (int i = 0; i < waveEnemies.Count; i++)
            {
                WaveEnemyEntry entry = waveEnemies[i];
                if (entry == null || entry.Count <= 0)
                {
                    continue;
                }

                GameObject resolvedPrefab = ResolvePrefab(entry.EnemyType, enemyPrefabOverride);
                if (resolvedPrefab == null)
                {
                    Debug.LogWarning(
                        $"Wave '{wave.name}' skipped enemy type '{entry.EnemyType}' because no prefab is mapped.",
                        this);
                    continue;
                }

                instructions.Add(new SpawnInstruction(entry.EnemyType, resolvedPrefab, entry.Count));
            }
        }

        if (instructions.Count > 0)
        {
            return instructions;
        }

        if (enemyPrefabOverride != null && wave.TotalEnemyCount > 0)
        {
            instructions.Add(new SpawnInstruction(EnemyType.Basic, enemyPrefabOverride, wave.TotalEnemyCount));
            return instructions;
        }

        if (wave.TryGetLegacySpawnData(out GameObject legacyEnemyPrefab, out int legacyCount))
        {
            instructions.Add(new SpawnInstruction(EnemyType.Basic, legacyEnemyPrefab, legacyCount));
        }

        return instructions;
    }

    private GameObject ResolvePrefab(EnemyType enemyType, GameObject enemyPrefabOverride)
    {
        if (enemyPrefabOverride != null)
        {
            return enemyPrefabOverride;
        }

        EnemyPrefabRegistry registry = EnemyPrefabRegistry.Instance;
        if (registry == null)
        {
            return null;
        }

        registry.TryGetPrefab(enemyType, out GameObject prefab);
        return prefab;
    }

    private void TrackEnemy(EnemyMovement enemy)
    {
        if (enemy == null || !trackedEnemies.Add(enemy))
        {
            return;
        }

        enemy.OnReachedGoal += HandleEnemyReachedGoal;

        EnemyHealth2D health = enemy.GetComponent<EnemyHealth2D>();
        if (health != null)
        {
            health.OnDied += HandleEnemyDied;
        }

        OnAliveEnemyCountChanged?.Invoke(trackedEnemies.Count);
    }

    private void HandleEnemyReachedGoal(EnemyMovement enemy)
    {
        OnEnemyReachedGoal?.Invoke(enemy);
        UntrackEnemy(enemy);
    }

    private void HandleEnemyDied(EnemyHealth2D enemyHealth)
    {
        if (enemyHealth == null)
        {
            return;
        }

        EnemyMovement enemy = enemyHealth.GetComponent<EnemyMovement>();
        if (enemy == null)
        {
            enemy = enemyHealth.GetComponentInParent<EnemyMovement>();
        }

        UntrackEnemy(enemy);
    }

    private void UntrackEnemy(EnemyMovement enemy)
    {
        if (enemy == null || !trackedEnemies.Remove(enemy))
        {
            return;
        }

        enemy.OnReachedGoal -= HandleEnemyReachedGoal;

        EnemyHealth2D health = enemy.GetComponent<EnemyHealth2D>();
        if (health != null)
        {
            health.OnDied -= HandleEnemyDied;
        }

        OnEnemyRemoved?.Invoke(enemy);
        OnAliveEnemyCountChanged?.Invoke(trackedEnemies.Count);
    }

    private void ClearTrackedEnemies()
    {
        if (trackedEnemies.Count == 0)
        {
            return;
        }

        EnemyMovement[] snapshot = new EnemyMovement[trackedEnemies.Count];
        trackedEnemies.CopyTo(snapshot);

        for (int i = 0; i < snapshot.Length; i++)
        {
            EnemyMovement enemy = snapshot[i];
            if (enemy == null)
            {
                continue;
            }

            enemy.OnReachedGoal -= HandleEnemyReachedGoal;

            EnemyHealth2D health = enemy.GetComponent<EnemyHealth2D>();
            if (health != null)
            {
                health.OnDied -= HandleEnemyDied;
            }
        }

        trackedEnemies.Clear();
        OnAliveEnemyCountChanged?.Invoke(0);
    }

    private void PruneDestroyedEnemies()
    {
        if (trackedEnemies.Count == 0)
        {
            return;
        }

        List<EnemyMovement> stale = null;

        foreach (EnemyMovement enemy in trackedEnemies)
        {
            if (enemy != null)
            {
                continue;
            }

            stale ??= new List<EnemyMovement>();
            stale.Add(enemy);
        }

        if (stale == null)
        {
            return;
        }

        for (int i = 0; i < stale.Count; i++)
        {
            trackedEnemies.Remove(stale[i]);
        }

        OnAliveEnemyCountChanged?.Invoke(trackedEnemies.Count);
    }
}
