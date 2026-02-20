using System;
using System.Collections.Generic;
using UnityEngine;

public class CentralObjectPool : MonoBehaviour
{
    [Serializable]
    private class PoolPreset
    {
        [SerializeField] private string id = "";
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private int initialSize = 0;
        [SerializeField, Min(1)] private int maxSize = 128;

        public string Id => id != null ? id.Trim() : string.Empty;
        public GameObject Prefab => prefab;
        public int InitialSize => Mathf.Max(0, initialSize);
        public int MaxSize => Mathf.Max(1, maxSize);
    }

    private sealed class PooledObject : MonoBehaviour
    {
        public GameObject SourcePrefab;
        public string PoolId;
        public bool IsPooled;
    }

    private sealed class PoolState
    {
        public PoolState(string id, GameObject prefab, int maxSize, Transform root)
        {
            Id = id;
            Prefab = prefab;
            MaxSize = Mathf.Max(1, maxSize);
            Available = new Queue<GameObject>();

            GameObject container = new GameObject($"{Id}_Pool");
            Container = container.transform;
            Container.SetParent(root, false);
        }

        public string Id { get; }
        public GameObject Prefab { get; }
        public int MaxSize { get; }
        public Queue<GameObject> Available { get; }
        public Transform Container { get; }
    }

    [Header("Pool Settings")]
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField, Min(1)] private int defaultMaxPoolSize = 128;
    [SerializeField] private List<PoolPreset> presets = new List<PoolPreset>();

    private static CentralObjectPool instance;
    private readonly Dictionary<string, PoolState> poolsById = new Dictionary<string, PoolState>(StringComparer.Ordinal);
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

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        return Instance.SpawnInternal(prefab, null, position, rotation);
    }

    public static GameObject Spawn(string presetId, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            Debug.LogWarning("Cannot spawn from an empty pool preset id.");
            return null;
        }

        return Instance.SpawnInternal(null, presetId.Trim(), position, rotation);
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

            string poolId = string.IsNullOrWhiteSpace(preset.Id) ? preset.Prefab.name : preset.Id;

            PoolState state = CreateState(poolId, preset.Prefab, preset.MaxSize);
            if (state == null)
            {
                continue;
            }

            Prewarm(state, preset.InitialSize);
        }
    }

    private GameObject SpawnInternal(GameObject prefab, string presetId, Vector3 position, Quaternion rotation)
    {
        PoolState state = GetOrCreateState(prefab, presetId, defaultMaxPoolSize);
        if (state == null)
        {
            return null;
        }

        GameObject instanceObject = GetAvailableObject(state);

        instanceObject.transform.SetParent(null);
        instanceObject.transform.SetPositionAndRotation(position, rotation);

        PooledObject marker = GetOrAddMarker(instanceObject);
        marker.SourcePrefab = state.Prefab;
        marker.PoolId = state.Id;
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

        PoolState state = GetOrCreateState(marker.SourcePrefab, marker.PoolId, defaultMaxPoolSize);
        if (state == null)
        {
            Destroy(instanceObject);
            return;
        }

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

    private PoolState GetOrCreateState(GameObject prefab, string presetId, int maxSize)
    {
        if (!string.IsNullOrWhiteSpace(presetId) && poolsById.TryGetValue(presetId, out PoolState byId))
        {
            return byId;
        }

        if (prefab == null)
        {
            if (!string.IsNullOrWhiteSpace(presetId))
            {
                Debug.LogWarning($"Pool preset '{presetId}' was not found.", this);
            }

            return null;
        }

        int prefabId = prefab.GetInstanceID();
        if (poolsByPrefabId.TryGetValue(prefabId, out PoolState existingState))
        {
            return existingState;
        }

        string resolvedId = ResolvePoolId(presetId, prefabId, prefab);
        return CreateState(resolvedId, prefab, maxSize);
    }

    private PoolState CreateState(string poolId, GameObject prefab, int maxSize)
    {
        if (prefab == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(poolId))
        {
            poolId = ResolvePoolId(null, prefab.GetInstanceID(), prefab);
        }

        if (poolsById.TryGetValue(poolId, out PoolState existingById))
        {
            if (existingById.Prefab == prefab)
            {
                poolsByPrefabId[prefab.GetInstanceID()] = existingById;
                return existingById;
            }

            string originalPoolId = poolId;
            int prefabId = prefab.GetInstanceID();
            int suffix = 0;

            do
            {
                poolId = suffix == 0
                    ? $"{originalPoolId}_{prefabId}"
                    : $"{originalPoolId}_{prefabId}_{suffix}";
                suffix++;
            }
            while (poolsById.ContainsKey(poolId));

            Debug.LogWarning(
                $"Pool id '{originalPoolId}' is already assigned to another prefab. Using '{poolId}' instead.",
                this
            );
        }

        PoolState state = new PoolState(poolId, prefab, maxSize, transform);
        poolsById.Add(poolId, state);
        poolsByPrefabId[prefab.GetInstanceID()] = state;
        return state;
    }

    private static string ResolvePoolId(string presetId, int prefabId, GameObject prefab)
    {
        if (!string.IsNullOrWhiteSpace(presetId))
        {
            return presetId;
        }

        return $"{prefab.name}_{prefabId}";
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
        marker.PoolId = state.Id;
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
