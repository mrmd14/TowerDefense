using UnityEngine;

public class TowerController2D : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float range = 3.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool rotateToTarget = false;

    [Header("Shooting")]
    [SerializeField] private float fireCooldown = 0.6f;
    [SerializeField] private int damage = 1;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Projectile2D projectilePrefab;

    [Header("Performance")]
    [SerializeField] private float targetScanInterval = 0.1f;

    private Transform currentTarget;
    private float scanTimer;
    private float fireTimer;

    private void Awake()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("TowerController2D requires a Projectile2D prefab. Disabling component.", this);
            enabled = false;
            return;
        }

        fireCooldown = Mathf.Max(0f, fireCooldown);
        targetScanInterval = Mathf.Max(0.01f, targetScanInterval);
        range = Mathf.Max(0f, range);
        damage = Mathf.Max(1, damage);
    }

    private void Update()
    {
        if (!enabled)
        {
            return;
        }

        scanTimer -= Time.deltaTime;
        fireTimer -= Time.deltaTime;

        if (scanTimer <= 0f)
        {
            scanTimer = targetScanInterval;
            currentTarget = FindNearestEnemyInRange();
        }

        if (currentTarget == null)
        {
            return;
        }

        if (!IsTargetStillInRange(currentTarget))
        {
            currentTarget = null;
            return;
        }

        if (rotateToTarget)
        {
            RotateTowards(currentTarget.position);
        }

        if (fireTimer <= 0f)
        {
            Shoot(currentTarget);
            fireTimer = fireCooldown;
        }
    }

    private Transform FindNearestEnemyInRange()
    {
        Vector2 origin = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, enemyLayer);

        Transform nearest = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            Vector2 toEnemy = (Vector2)hit.transform.position - origin;
            float sqrDistance = toEnemy.sqrMagnitude;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    private bool IsTargetStillInRange(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        float sqrDistance = (target.position - transform.position).sqrMagnitude;
        float sqrRange = range * range;
        return sqrDistance <= sqrRange;
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector2 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Shoot(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Projectile2D projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        projectile.Init(target, damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
