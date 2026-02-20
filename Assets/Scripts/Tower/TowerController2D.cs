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

    [Header("Interaction")]
    [SerializeField] private bool ensureClickCollider = true;

    [Header("Upgrade")]
    [SerializeField] private TowerUpgradeProfile upgradeProfile;
    [SerializeField] private bool resetUpgradeOnEnable = true;
    [SerializeField, HideInInspector] private int currentUpgradeStage = -1;

    [Header("Rendering")]
    [SerializeField] private SpriteRenderer[] sortableSpriteRenderers;
    [SerializeField, Min(1)] private int sortingOrderPerGridRow = 10;
    [SerializeField] private int sortingOrderOffset;

    private Transform currentTarget;
    private float scanTimer;
    private float fireTimer;
    private int[] sortableBaseOrders = new int[0];
    private int sortableBaseOrderAnchor;

    public float Range => range;
    public int Damage => damage;
    public float FireCooldown => fireCooldown;
    public int CurrentUpgradeStage => currentUpgradeStage;
    public bool HasUpgradeStages => upgradeProfile != null && upgradeProfile.HasStages;
    public bool CanUpgrade => TryGetNextUpgradeStage(out _, out _);
    public int NextUpgradeCost => TryGetNextUpgradeStage(out TowerUpgradeProfile.Stage stage, out _) ? stage.cost : -1;
    protected Vector3 FirePointPosition => firePoint != null ? firePoint.position : transform.position;
    public TowerUpgradeProfile UpgradeProfile => upgradeProfile;

    public void SetRange(float value)
    {
        range = Mathf.Max(0f, value);
    }

    public void SetDamage(int value)
    {
        damage = Mathf.Max(1, value);
    }

    public void SetFireCooldown(float value)
    {
        fireCooldown = Mathf.Max(0f, value);
    }

    public void SetCombatStats(float rangeValue, int damageValue, float cooldownValue)
    {
        SetRange(rangeValue);
        SetDamage(damageValue);
        SetFireCooldown(cooldownValue);
    }

    public void SetSortingOrderFromGridY(int gridY)
    {
        CacheSortableRenderers();
        if (sortableSpriteRenderers == null || sortableSpriteRenderers.Length == 0)
        {
            return;
        }

        int targetBaseOrder = sortingOrderOffset + (-gridY * Mathf.Max(1, sortingOrderPerGridRow));
        int delta = targetBaseOrder - sortableBaseOrderAnchor;

        for (int i = 0; i < sortableSpriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = sortableSpriteRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            spriteRenderer.sortingOrder = sortableBaseOrders[i] + delta;
        }
    }

    private void Reset()
    {
        sortableSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

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

        EnsureClickCollider();
        CacheSortableRenderers();

        if (HasUpgradeStages)
        {
            ApplyUpgradeStage(0);
        }
    }

    protected virtual void OnEnable()
    {
        currentTarget = null;
        scanTimer = 0f;
        fireTimer = 0f;

        if (resetUpgradeOnEnable)
        {
            ResetUpgradeProgress();
        }
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
            currentTarget = FindLowestHpEnemyInRange();
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

    public bool TryGetInitialUpgradeStage(out TowerUpgradeProfile.Stage stage)
    {
        if (upgradeProfile == null || !upgradeProfile.TryGetStage(0, out stage))
        {
            stage = default;
            return false;
        }

        return true;
    }

    public bool TryGetInitialUpgradeRange(out float stageRange)
    {
        stageRange = 0f;
        if (!TryGetInitialUpgradeStage(out TowerUpgradeProfile.Stage stage))
        {
            return false;
        }

        stageRange = stage.range;
        return true;
    }

    public bool TryGetInitialUpgradeCost(out int stageCost)
    {
        stageCost = 0;
        if (!TryGetInitialUpgradeStage(out TowerUpgradeProfile.Stage stage))
        {
            return false;
        }

        stageCost = stage.cost;
        return true;
    }

    public bool ResetUpgradeProgress()
    {
        if (!HasUpgradeStages)
        {
            currentUpgradeStage = -1;
            return false;
        }

        ApplyUpgradeStage(0);
        return true;
    }

    public bool TryUpgrade()
    {
        CurrencyManager currencyManager = CurrencyManager.Instance ?? FindFirstObjectByType<CurrencyManager>();
        return TryUpgrade(currencyManager);
    }

    public bool TryUpgrade(CurrencyManager currencyManager)
    {
        if (!TryGetNextUpgradeStage(out TowerUpgradeProfile.Stage nextStage, out int nextStageIndex))
        {
            return false;
        }

        if (nextStage.cost > 0)
        {
            if (currencyManager == null)
            {
                return false;
            }

            if (!currencyManager.TrySpend(nextStage.cost))
            {
                return false;
            }
        }

        ApplyUpgradeStage(nextStageIndex);
        return true;
    }

    private Transform FindLowestHpEnemyInRange()
    {
        Vector2 origin = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, enemyLayer);

        Transform lowestHpTarget = null;
        int lowestHp = int.MaxValue;
        float lowestHpSqrDistance = float.MaxValue;

        Transform nearestFallbackTarget = null;
        float nearestFallbackSqrDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            Vector2 toEnemy = (Vector2)hit.transform.position - origin;
            float sqrDistance = toEnemy.sqrMagnitude;

            EnemyHealth2D enemyHealth = hit.GetComponentInParent<EnemyHealth2D>();
            if (enemyHealth == null || enemyHealth.CurrentHP <= 0)
            {
                if (sqrDistance < nearestFallbackSqrDistance)
                {
                    nearestFallbackSqrDistance = sqrDistance;
                    nearestFallbackTarget = hit.transform;
                }

                continue;
            }

            int currentHp = enemyHealth.CurrentHP;
            if (currentHp < lowestHp || (currentHp == lowestHp && sqrDistance < lowestHpSqrDistance))
            {
                lowestHp = currentHp;
                lowestHpSqrDistance = sqrDistance;
                lowestHpTarget = enemyHealth.transform;
            }
        }

        return lowestHpTarget != null ? lowestHpTarget : nearestFallbackTarget;
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

    protected virtual void Shoot(Transform target)
    {
        if (target == null)
        {
            return;
        }

        GameObject projectileObject = CentralObjectPool.Spawn(projectilePrefab.gameObject, FirePointPosition, Quaternion.identity);
        Projectile2D projectile = projectileObject != null ? projectileObject.GetComponent<Projectile2D>() : null;
        if (projectile != null)
        {
            projectile.Init(target, damage);
        }
    }

    private void EnsureClickCollider()
    {
        if (!ensureClickCollider || TryGetComponent<Collider2D>(out _))
        {
            return;
        }

        BoxCollider2D clickCollider = gameObject.AddComponent<BoxCollider2D>();
        clickCollider.isTrigger = true;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            clickCollider.offset = spriteBounds.center;
            clickCollider.size = spriteBounds.size;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    private bool TryGetNextUpgradeStage(out TowerUpgradeProfile.Stage stage, out int stageIndex)
    {
        stage = default;
        stageIndex = -1;

        if (!HasUpgradeStages)
        {
            return false;
        }

        stageIndex = currentUpgradeStage + 1;
        if (!upgradeProfile.TryGetStage(stageIndex, out stage))
        {
            stageIndex = -1;
            return false;
        }

        return true;
    }

    private void ApplyUpgradeStage(int stageIndex)
    {
        if (upgradeProfile == null || !upgradeProfile.TryGetStage(stageIndex, out TowerUpgradeProfile.Stage stage))
        {
            return;
        }

        currentUpgradeStage = stageIndex;
        SetCombatStats(stage.range, stage.power, stage.cooldown);
        currentTarget = null;
        scanTimer = 0f;
        fireTimer = Mathf.Min(fireTimer, fireCooldown);
    }

    private void CacheSortableRenderers()
    {
        if (sortableSpriteRenderers == null || sortableSpriteRenderers.Length == 0)
        {
            sortableSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (sortableSpriteRenderers == null || sortableSpriteRenderers.Length == 0)
        {
            sortableBaseOrders = new int[0];
            sortableBaseOrderAnchor = 0;
            return;
        }

        sortableBaseOrders = new int[sortableSpriteRenderers.Length];
        int anchor = int.MaxValue;

        for (int i = 0; i < sortableSpriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = sortableSpriteRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            sortableBaseOrders[i] = spriteRenderer.sortingOrder;
            if (spriteRenderer.sortingOrder < anchor)
            {
                anchor = spriteRenderer.sortingOrder;
            }
        }

        sortableBaseOrderAnchor = anchor == int.MaxValue ? 0 : anchor;
    }
}
