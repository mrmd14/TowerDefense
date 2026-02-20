using UnityEngine;

public class PooledDespawnAfterDelay : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float despawnDelay = 2f;

    private float aliveTimer;

    public void SetDelay(float delay)
    {
        despawnDelay = Mathf.Max(0.1f, delay);
    }

    private void OnEnable()
    {
        aliveTimer = 0f;
    }

    private void Update()
    {
        aliveTimer += Time.deltaTime;
        if (aliveTimer >= Mathf.Max(0.1f, despawnDelay))
        {
            CentralObjectPool.Despawn(gameObject);
        }
    }
}
