using UnityEngine;

public class PooledParticleAutoDespawn : MonoBehaviour
{
    [Header("Safety")]
    [SerializeField, Min(0.1f)] private float maxAliveTime = 6f;

    private ParticleSystem[] particleSystems;
    private float aliveTimer;

    private void Awake()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        aliveTimer = 0f;

        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem system = particleSystems[i];
            if (system == null)
            {
                continue;
            }

            system.Clear(true);
            system.Play(true);
        }
    }

    private void Update()
    {
        aliveTimer += Time.deltaTime;
        if (aliveTimer >= Mathf.Max(0.1f, maxAliveTime) || !AnyParticleSystemAlive())
        {
            CentralObjectPool.Despawn(gameObject);
        }
    }

    private bool AnyParticleSystemAlive()
    {
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem system = particleSystems[i];
            if (system != null && system.IsAlive(true))
            {
                return true;
            }
        }

        return false;
    }
}
