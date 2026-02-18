using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Wave Config", fileName = "WaveConfig")]
public class WaveConfig : ScriptableObject
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField, Min(0)] private int count = 5;
    [SerializeField, Min(0f)] private float spawnInterval = 0.5f;
    [SerializeField, Min(0f)] private float startDelay = 0f;
    [SerializeField, Min(0f)] private float waveDuration = 10f;

    public GameObject EnemyPrefab => enemyPrefab;
    public int Count => Mathf.Max(0, count);
    public float SpawnInterval => Mathf.Max(0f, spawnInterval);
    public float StartDelay => Mathf.Max(0f, startDelay);
    public float WaveDuration => Mathf.Max(0f, waveDuration);

    private void OnValidate()
    {
        count = Mathf.Max(0, count);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        startDelay = Mathf.Max(0f, startDelay);
        waveDuration = Mathf.Max(0f, waveDuration);
    }
}
