using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class WaveEnemyEntry
{
    [SerializeField] private EnemyType enemyType = EnemyType.Basic;
    [SerializeField, Min(0)] private int count = 1;

    public EnemyType EnemyType => enemyType;
    public int Count => Mathf.Max(0, count);

    public WaveEnemyEntry()
    {
    }

    public WaveEnemyEntry(EnemyType enemyType, int count)
    {
        this.enemyType = enemyType;
        this.count = Mathf.Max(0, count);
    }

    public void Clamp()
    {
        count = Mathf.Max(0, count);
    }
}

[CreateAssetMenu(menuName = "Tower Defense/Wave Config", fileName = "WaveConfig")]
public class WaveConfig : ScriptableObject
{
    [Header("Enemies")]
    [SerializeField] private List<WaveEnemyEntry> enemies = new List<WaveEnemyEntry>();

    [Header("Timing")]
    [SerializeField, Min(0f)] private float spawnInterval = 0.5f;
    [SerializeField, Min(0f)] private float startDelay = 0f;
    [SerializeField, Min(0f)] private float waveDuration = 10f;

    // Legacy single-enemy fields kept for backwards compatibility with existing assets.
    [FormerlySerializedAs("enemyPrefab")]
    [SerializeField, HideInInspector] private GameObject legacyEnemyPrefab;

    [FormerlySerializedAs("count")]
    [SerializeField, HideInInspector, Min(0)] private int legacyCount = 5;

    public IReadOnlyList<WaveEnemyEntry> Enemies => enemies;
    public int TotalEnemyCount
    {
        get
        {
            if (enemies == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                WaveEnemyEntry entry = enemies[i];
                if (entry == null)
                {
                    continue;
                }

                total += entry.Count;
            }

            return total;
        }
    }

    public float SpawnInterval => Mathf.Max(0f, spawnInterval);
    public float StartDelay => Mathf.Max(0f, startDelay);
    public float WaveDuration => Mathf.Max(0f, waveDuration);

    public bool TryGetLegacySpawnData(out GameObject enemyPrefab, out int count)
    {
        enemyPrefab = legacyEnemyPrefab;
        count = Mathf.Max(0, legacyCount);
        return enemyPrefab != null && count > 0;
    }

    private void OnEnable()
    {
        EnsureMigratedEnemyEntries();
    }

    private void OnValidate()
    {
        EnsureMigratedEnemyEntries();

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i]?.Clamp();
            }
        }

        legacyCount = Mathf.Max(0, legacyCount);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        startDelay = Mathf.Max(0f, startDelay);
        waveDuration = Mathf.Max(0f, waveDuration);
    }

    private void EnsureMigratedEnemyEntries()
    {
        enemies ??= new List<WaveEnemyEntry>();

        if (enemies.Count > 0 || legacyCount <= 0)
        {
            return;
        }

        enemies.Add(new WaveEnemyEntry(EnemyType.Basic, legacyCount));
    }
}
