using System.Collections.Generic;
using UnityEngine;

public class BombProjectile2D : Projectile2D
{
    [Header("Bomb Explosion")]
    [SerializeField, Min(0f)] private float explosionRadius = 1.25f;

    [Header("Bomb Impact VFX")]
    [SerializeField] private GameObject bombImpactVfxPrefab;
    [SerializeField] private Vector3 bombImpactVfxOffset = Vector3.zero;
    [SerializeField] private float bombImpactVfxRotationZ = 0f;
    [SerializeField, Min(0.1f)] private float bombImpactVfxDespawnDelay = 2f;

    [Header("Bomb Impact Camera Shake")]
    [SerializeField] private bool shakeCameraOnImpact = true;
    [SerializeField, Min(0f)] private float cameraShakeDuration = 0.18f;
    [SerializeField, Min(0f)] private float cameraShakeStrength = 0.18f;

    [Header("Bomb Rotation")]
    [SerializeField] private float zRotationSpeed = 360f;

    private bool exploded;

    protected override bool AllowProximityImpact => false;
    protected override bool AllowCollisionImpact => false;

    public override void Init(Transform targetTransform, int damageAmount)
    {
        exploded = false;
        base.Init(targetTransform, damageAmount);
    }

    private void LateUpdate()
    {
        transform.Rotate(0f, 0f, zRotationSpeed * Time.deltaTime);
    }

    protected override void OnGroundReached()
    {
        Explode();
    }

    protected override void OnLifetimeExpired()
    {
        Explode();
    }





    private void Explode()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;
        MarkAsHit();

        Vector3 explosionCenter = transform.position;
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(explosionCenter, Mathf.Max(0f, explosionRadius));
        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        bool damagedAtLeastOne = false;
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];
            if (overlap == null)
            {
                continue;
            }

            IDamageable damageable = overlap.GetComponentInParent<IDamageable>();
            if (damageable == null || !hitTargets.Add(damageable))
            {
                continue;
            }

            damageable.TakeDamage(DamageAmount);
            damagedAtLeastOne = true;
        }

        if (damagedAtLeastOne)
        {
            SpawnEnemyHitParticleAt(explosionCenter);
        }

        SpawnBombImpactVfx(explosionCenter);
        TriggerCameraShake();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, explosionRadius));
    }

    private void SpawnBombImpactVfx(Vector3 hitPoint)
    {
        if (bombImpactVfxPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = hitPoint + bombImpactVfxOffset;
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, bombImpactVfxRotationZ);
        GameObject impactVfx = CentralObjectPool.Spawn(bombImpactVfxPrefab, spawnPosition, spawnRotation);
        if (impactVfx == null)
        {
            return;
        }

        if (impactVfx.GetComponent<PooledParticleAutoDespawn>() != null)
        {
            return;
        }

        PooledDespawnAfterDelay despawnAfterDelay = impactVfx.GetComponent<PooledDespawnAfterDelay>();
        if (despawnAfterDelay == null)
        {
            despawnAfterDelay = impactVfx.AddComponent<PooledDespawnAfterDelay>();
        }

        despawnAfterDelay.SetDelay(bombImpactVfxDespawnDelay);
    }

    private void TriggerCameraShake()
    {
        if (!shakeCameraOnImpact)
        {
            return;
        }

        OrthographicYBoundsCamera boundsCamera = OrthographicYBoundsCamera.Instance;
        if (boundsCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                boundsCamera = mainCamera.GetComponent<OrthographicYBoundsCamera>();
            }
        }

        if (boundsCamera == null)
        {
            return;
        }

        if (cameraShakeDuration <= 0f || cameraShakeStrength <= 0f)
        {
            boundsCamera.TriggerDefaultShake();
            return;
        }

        boundsCamera.TriggerShake(cameraShakeDuration, cameraShakeStrength);
    }
}
