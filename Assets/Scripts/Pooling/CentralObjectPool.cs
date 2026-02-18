using System;
using System.Collections.Generic;
using UnityEngine;

public class CentralObjectPool : MonoBehaviour
{
    public enum PoolCategory
    {
        Projectile,
        Enemy,
        Tower
    }

    [Serializable]
    private class PoolPreset
    {
        [SerializeField] private PoolCategory category = PoolCategory.Projectile;
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private int initialSize = 0;
        [SerializeField, Min(1)] private int maxSize = 128;

        public PoolCategory Category => category;
        public GameObject Prefab => prefab;
        public int InitialSize => Mathf.Max(0, initialSize);
        public int MaxSize => Mathf.Max(1, maxSize);
    }

    private sealed class PooledObject : MonoBehaviour
    {
        public GameObject SourcePrefab;
        public PoolCategory Category;
        public bool IsPooled;
    }

    private sealed class PoolState
    {
        public PoolState(GameObject prefab, PoolCategory category, int maxSize, Transform root)
        {
            Prefab = prefab;
            Category = category;
            MaxSize = Mathf.Max(1, maxSize);
            Available = new Queue<GameObject>();

            GameObject container = new GameObject($"{prefab.name}_{category}_Pool");
            Container = container.transform;
            Container.SetParent(root, false);
        }

        public GameObject Prefab { get; }
        public PoolCategory Category { get; }
        public int MaxSize { get; }
        public Queue<GameObject> Available { get; }
        public Transform Container { get; }
    }

    [Header("Pool Settings")]
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField, Min(1)] private int defaultMaxPoolSize = 128;
    [SerializeField] private List<PoolPreset> presets = new List<PoolPreset>();

    private static CentralObjectPool instance;
    private readonly Dictionary<int, PoolState> poolsByPrefabId = new Dictionary<int, PoolState>();

    public static CentralObjectPool Instance => EnsureInstance();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        InitializePresets();
    }

    public static Projectile2D SpawnProjectile(Projectile2D prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject projectileObject = Spawn(PoolCategory.Projectile, prefab.gameObject, position, rotation);
        return projectileObject != null ? projectileObject.GetComponent<Projectile2D>() : null;
    }

    public static GameObject SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return Spawn(PoolCategory.Enemy, prefab, position, rotation);
    }

    public static GameObject SpawnTower(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return Spawn(PoolCategory.Tower, prefab, position, rotation);
    }

    public static GameObject Spawn(PoolCategory category, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        return Instance.SpawnInternal(category, prefab, position, rotation);
    }

    public static void DespawnProjectile(Projectile2D projectile)
    {
        if (projectile != null)
        {
            Despawn(projectile.gameObject);
        }
    }

    public static void DespawnEnemy(EnemyMovement enemy)
    {
        if (enemy != null)
        {
            Despawn(enemy.gameObject);
        }
    }

    public static void DespawnTower(GameObject towerObject)
    {
        Despawn(towerObject);
    }

    public static void Despawn(GameObject instanceObject)
    {
        if (instanceObject == null)
        {
            return;
        }

        if (instance == null)
        {
            Destroy(instanceObject);
            return;
        }

        instance.DespawnInternal(instanceObject);
    }

    private static CentralObjectPool EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<CentralObjectPool>();
        if (instance != null)
        {
            return instance;
        }

        GameObject poolObject = new GameObject(nameof(CentralObjectPool));
        instance = poolObject.AddComponent<CentralObjectPool>();
        return instance;
    }

    private void InitializePresets()
    {
        for (int i = 0; i < presets.Count; i++)
        {
            PoolPreset preset = presets[i];
            if (preset == null || preset.Prefab == null)
            {
                continue;
            }

            PoolState state = GetOrCreateState(preset.Prefab, preset.Category, preset.MaxSize);
            Prewarm(state, preset.InitialSize);
        }
    }

    private GameObject SpawnInternal(PoolCategory category, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        PoolState state = GetOrCreateState(prefab, category, defaultMaxPoolSize);
        GameObject instanceObject = GetAvailableObject(state);

        instanceObject.transform.SetParent(null);
        instanceObject.transform.SetPositionAndRotation(position, rotation);

        PooledObject marker = GetOrAddMarker(instanceObject);
        marker.SourcePrefab = state.Prefab;
        marker.Category = state.Category;
        marker.IsPooled = false;

        instanceObject.SetActive(true);
        return instanceObject;
    }

    private void DespawnInternal(GameObject instanceObject)
    {
        PooledObject marker = instanceObject.GetComponent<PooledObject>();
        if (marker == null || marker.SourcePrefab == null)
        {
            Destroy(instanceObject);
            return;
        }

        if (marker.IsPooled)
        {
            return;
        }

        PoolState state = GetOrCreateState(marker.SourcePrefab, marker.Category, defaultMaxPoolSize);
        if (state.Available.Count >= state.MaxSize)
        {
            Destroy(instanceObject);
            return;
        }

        marker.IsPooled = true;
        instanceObject.SetActive(false);
        instanceObject.transform.SetParent(state.Container, false);
        state.Available.Enqueue(instanceObject);
    }

    private PoolState GetOrCreateState(GameObject prefab, PoolCategory category, int maxSize)
    {
        int prefabId = prefab.GetInstanceID();
        if (poolsByPrefabId.TryGetValue(prefabId, out PoolState existingState))
        {
            return existingState;
        }

        PoolState state = new PoolState(prefab, category, maxSize, transform);
        poolsByPrefabId.Add(prefabId, state);
        return state;
    }

    private GameObject GetAvailableObject(PoolState state)
    {
        while (state.Available.Count > 0)
        {
            GameObject pooled = state.Available.Dequeue();
            if (pooled != null)
            {
                return pooled;
            }
        }

        return CreateNewPooledObject(state);
    }

    private GameObject CreateNewPooledObject(PoolState state)
    {
        GameObject created = Instantiate(state.Prefab, state.Container);
        created.SetActive(false);

        PooledObject marker = GetOrAddMarker(created);
        marker.SourcePrefab = state.Prefab;
        marker.Category = state.Category;
        marker.IsPooled = true;

        return created;
    }

    private static PooledObject GetOrAddMarker(GameObject instanceObject)
    {
        PooledObject marker = instanceObject.GetComponent<PooledObject>();
        if (marker == null)
        {
            marker = instanceObject.AddComponent<PooledObject>();
        }

        return marker;
    }

    private void Prewarm(PoolState state, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (state.Available.Count >= state.MaxSize)
            {
                break;
            }

            GameObject instanceObject = CreateNewPooledObject(state);
            if (instanceObject == null)
            {
                continue;
            }

            instanceObject.SetActive(false);
            instanceObject.transform.SetParent(state.Container, false);
            state.Available.Enqueue(instanceObject);
        }
    }
}
