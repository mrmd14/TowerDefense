using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPrefabRegistry : MonoBehaviour
{
    [Serializable]
    private class EnemyPrefabEntry
    {
        [SerializeField] private EnemyType enemyType = EnemyType.Basic;
        [SerializeField] private GameObject prefab;

        public EnemyType EnemyType => enemyType;
        public GameObject Prefab => prefab;
    }

    private static EnemyPrefabRegistry instance;

    public static EnemyPrefabRegistry Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<EnemyPrefabRegistry>();
                instance?.RebuildCache();
            }

            return instance;
        }
        private set => instance = value;
    }

    [SerializeField] private List<EnemyPrefabEntry> enemyPrefabs = new List<EnemyPrefabEntry>();

    private readonly Dictionary<EnemyType, GameObject> prefabsByType = new Dictionary<EnemyType, GameObject>();
    private bool cacheBuilt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple EnemyPrefabRegistry instances found. Keeping the first one.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RebuildCache();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        RebuildCache();
    }

    public bool TryGetPrefab(EnemyType enemyType, out GameObject prefab)
    {
        if (!cacheBuilt)
        {
            RebuildCache();
        }

        if (prefabsByType.TryGetValue(enemyType, out prefab) && prefab != null)
        {
            return true;
        }

        prefab = null;
        return false;
    }

    private void RebuildCache()
    {
        prefabsByType.Clear();
        cacheBuilt = true;

        if (enemyPrefabs == null)
        {
            return;
        }

        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            EnemyPrefabEntry entry = enemyPrefabs[i];
            if (entry == null || entry.Prefab == null)
            {
                continue;
            }

            prefabsByType[entry.EnemyType] = entry.Prefab;
        }
    }
}
