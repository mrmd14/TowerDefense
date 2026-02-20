using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    private const float BaseGravity = 9.81f;

    [Header("Arc Movement")]
    [SerializeField] private float arcHeight = 1.5f;
    [SerializeField] private float arcHeightPerUnitDistance = 0.35f;
    [SerializeField] private float gravityScale = 1f;
    [SerializeField] private float hitDistance = 0.15f;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 4f;

    [Header("Hit")]
    [SerializeField] private bool useTriggerHit = true;

    [Header("Impact VFX")]
    [SerializeField] private GameObject enemyHitParticlePrefab;
    [SerializeField] private Vector3 enemyHitParticleOffset = Vector3.zero;
    [SerializeField] private float enemyHitParticleRotationZ = 0f;

    private Transform target;
    private int damage;
    private float lifetimeTimer;
    private float flightTimer;
    private bool hasHit;
    private bool trajectoryReady;

    private float effectiveGravity;
    private float ascentDuration;
    private float descentDuration;
    private float launchVerticalVelocity;

    private Vector3 launchPosition;
    private Vector3 destinationPosition;
    private Vector3 apexPosition;

    protected int DamageAmount => damage;
    protected virtual bool AllowProximityImpact => true;
    protected virtual bool AllowCollisionImpact => true;

    public virtual void Init(Transform targetTransform, int damageAmount)
    {
        target = targetTransform;
        damage = Mathf.Max(1, damageAmount);
        hasHit = false;
        lifetimeTimer = 0f;
        SetupTrajectory();
    }

    private void Update()
    {
        if (hasHit)
        {
            return;
        }

        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= Mathf.Max(0.1f, maxLifetime))
        {
            OnLifetimeExpired();
            DespawnSelf();
            return;
        }

        if (!trajectoryReady)
        {
            SetupTrajectory();
        }

        flightTimer += Time.deltaTime;

        float totalFlightDuration = ascentDuration + descentDuration;
        if (flightTimer >= totalFlightDuration)
        {
            transform.position = destinationPosition;
            OnGroundReached();
            DespawnSelf();
            return;
        }

        transform.position = EvaluateArcPosition(flightTimer);

        if (AllowProximityImpact && target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            float hitDistanceSqr = hitDistance * hitDistance;
            if (toTarget.sqrMagnitude <= hitDistanceSqr)
            {
                Impact(target);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTriggerHit || !AllowCollisionImpact || hasHit || other == null)
        {
            return;
        }

        if (other.GetComponentInParent<IDamageable>() != null)
        {
            Impact(other.transform);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (useTriggerHit || !AllowCollisionImpact || hasHit || collision == null)
        {
            return;
        }

        Collider2D other = collision.collider;
        if (other != null && other.GetComponentInParent<IDamageable>() != null)
        {
            Impact(other.transform);
        }
    }

    private void Impact(Transform hitTransform)
    {
        if (hasHit || !CanHitTarget())
        {
            return;
        }

        hasHit = true;

        IDamageable damageable = hitTransform.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(damage);

        if (damageable != null)
        {
            SpawnEnemyHitParticle();
        }

        DespawnSelf();
    }

    private void SpawnEnemyHitParticle()
    {
        SpawnEnemyHitParticleAt(transform.position);
    }

    protected void SpawnEnemyHitParticleAt(Vector3 spawnPosition)
    {
        if (enemyHitParticlePrefab == null)
        {
            return;
        }

        spawnPosition += enemyHitParticleOffset;
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, enemyHitParticleRotationZ);
        GameObject impactVfx = CentralObjectPool.Spawn(enemyHitParticlePrefab, spawnPosition, spawnRotation);
        if (impactVfx == null)
        {
            return;
        }

        if (impactVfx.GetComponent<PooledParticleAutoDespawn>() == null)
        {
            impactVfx.AddComponent<PooledParticleAutoDespawn>();
        }
    }

    private bool CanHitTarget()
    {
        if (!trajectoryReady)
        {
            return false;
        }

        return flightTimer > ascentDuration;
    }

    private void SetupTrajectory()
    {
        launchPosition = transform.position;
        destinationPosition = target != null ? target.position : launchPosition;

        float minArcHeight = Mathf.Max(0f, arcHeight);
        float arcScaleByDistance = Mathf.Max(0f, arcHeightPerUnitDistance);
        float launchToDestinationDistance = Vector2.Distance(launchPosition, destinationPosition);
        float scaledArcHeight = launchToDestinationDistance * arcScaleByDistance;
        float finalArcHeight = Mathf.Max(minArcHeight, scaledArcHeight);

        float clampedGravityScale = Mathf.Max(0.01f, gravityScale);
        effectiveGravity = BaseGravity * clampedGravityScale;

        float apexY = Mathf.Max(launchPosition.y, destinationPosition.y) + finalArcHeight;
        float minimumYOffset = 0.01f;
        if (apexY < launchPosition.y + minimumYOffset)
        {
            apexY = launchPosition.y + minimumYOffset;
        }

        if (apexY < destinationPosition.y + minimumYOffset)
        {
            apexY = destinationPosition.y + minimumYOffset;
        }

        apexPosition = Vector3.Lerp(launchPosition, destinationPosition, 0.5f);
        apexPosition.y = apexY;

        float ascentHeight = apexY - launchPosition.y;
        float descentHeight = apexY - destinationPosition.y;

        ascentDuration = Mathf.Sqrt((2f * ascentHeight) / effectiveGravity);
        descentDuration = Mathf.Sqrt((2f * descentHeight) / effectiveGravity);
        launchVerticalVelocity = effectiveGravity * ascentDuration;

        flightTimer = 0f;
        trajectoryReady = true;
    }

    private Vector3 EvaluateArcPosition(float elapsedTime)
    {
        if (elapsedTime <= ascentDuration)
        {
            float ascentProgress = ascentDuration > 0f ? elapsedTime / ascentDuration : 1f;
            float horizontalLerp = 0.5f * ascentProgress;
            Vector3 horizontal = Vector3.Lerp(launchPosition, destinationPosition, horizontalLerp);
            float y = launchPosition.y
                + (launchVerticalVelocity * elapsedTime)
                - (0.5f * effectiveGravity * elapsedTime * elapsedTime);
            horizontal.y = y;
            return horizontal;
        }

        float descentTime = elapsedTime - ascentDuration;
        float descentProgress = descentDuration > 0f ? descentTime / descentDuration : 1f;
        float horizontalLerpDown = 0.5f + (0.5f * descentProgress);
        Vector3 horizontalDown = Vector3.Lerp(launchPosition, destinationPosition, horizontalLerpDown);
        float yDown = apexPosition.y - (0.5f * effectiveGravity * descentTime * descentTime);
        horizontalDown.y = yDown;
        return horizontalDown;
    }

    protected virtual void OnGroundReached()
    {
    }

    protected virtual void OnLifetimeExpired()
    {
    }

    protected void MarkAsHit()
    {
        hasHit = true;
    }

    protected virtual void DespawnSelf()
    {
        CentralObjectPool.Despawn(gameObject);
    }
}
