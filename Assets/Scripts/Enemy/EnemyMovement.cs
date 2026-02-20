using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private bool rotateToDirection = true;
    [SerializeField] private float reachDistance = 0.1f;
    [SerializeField] private bool destroyOnGoal = true;

    [Header("Momentum")]
    [SerializeField] private bool useMomentum = true;
    [SerializeField, Range(0f, 1f)] private float lastVelocityImpact = 0.65f;
    [SerializeField, Min(0f)] private float velocityAdjustSpeed = 12f;

    [Header("Visual")]
    [SerializeField] private EnemyGfxManager enemyGfxManager;

    public event Action<EnemyMovement> OnReachedGoal;

    private PathManager pathManager;
    private int currentWaypointIndex;
    private bool hasReachedGoal;
    private Vector3 currentVelocity;
    private float pathLateralOffset;
    private bool useCustomPathOffset;

    private void OnEnable()
    {
        pathManager = PathManager.Instance ?? FindFirstObjectByType<PathManager>();
        currentWaypointIndex = 0;
        hasReachedGoal = false;
        pathLateralOffset = 0f;
        useCustomPathOffset = false;

        // Fail fast when the path source is not available.
        if (pathManager == null)
        {
            Debug.LogError("EnemyMovement requires a PathManager in the scene. Disabling component.", this);
            enabled = false;
            return;
        }

        if (pathManager.WaypointCount == 0)
        {
            Debug.LogError("PathManager has no waypoints. EnemyMovement cannot move.", this);
            enabled = false;
            return;
        }

        Transform firstWaypoint = pathManager.GetWaypoint(0);
        if (firstWaypoint != null)
        {
            transform.position = firstWaypoint.position;
        }

        InitializeVelocity();

        if (enemyGfxManager == null)
        {
            enemyGfxManager = GetComponentInChildren<EnemyGfxManager>();
        }

        if (enemyGfxManager == null)
        {
            enemyGfxManager = gameObject.AddComponent<EnemyGfxManager>();
        }

        enemyGfxManager?.PlaySpawnFadeIn();
    }

    public void ConfigureSpawn(Vector3 spawnPosition, float lateralOffset)
    {
        transform.position = spawnPosition;
        currentWaypointIndex = 0;
        hasReachedGoal = false;
        pathLateralOffset = lateralOffset;
        useCustomPathOffset = Mathf.Abs(lateralOffset) > 0.0001f;
        InitializeVelocity();
    }

    private void Update()
    {
        if (!enabled || hasReachedGoal)
        {
            return;
        }

        // Read the active waypoint and move toward it each frame.
        if (!TryGetWaypointTargetPosition(currentWaypointIndex, out Vector3 targetPosition, out Vector3 segmentDirection))
        {
            Debug.LogError($"Waypoint {currentWaypointIndex} is null. Skipping to next waypoint.", this);
            AdvanceToNextWaypoint();
            return;
        }

        float waypointReachDistance = Mathf.Max(0f, reachDistance);

        // If we start exactly on a waypoint, advance first so we do not stall on spawn.
        if (ShouldAdvanceWaypoint(targetPosition, segmentDirection, waypointReachDistance))
        {
            AdvanceToNextWaypoint();
            if (hasReachedGoal)
            {
                return;
            }

            if (!TryGetWaypointTargetPosition(currentWaypointIndex, out targetPosition, out segmentDirection))
            {
                Debug.LogError($"Waypoint {currentWaypointIndex} is null. Skipping to next waypoint.", this);
                AdvanceToNextWaypoint();
                return;
            }
        }

        MoveWithMomentum(targetPosition);

        if (ShouldAdvanceWaypoint(targetPosition, segmentDirection, waypointReachDistance))
        {
            AdvanceToNextWaypoint();
        }
    }

    private void InitializeVelocity()
    {
        if (pathManager == null || pathManager.WaypointCount < 2)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        if (!TryGetWaypointTargetPosition(1, out Vector3 initialTargetPosition, out _))
        {
            currentVelocity = Vector3.zero;
            return;
        }

        Vector3 toInitialTarget = initialTargetPosition - transform.position;
        currentVelocity = toInitialTarget.sqrMagnitude > 0.0001f
            ? toInitialTarget.normalized * Mathf.Max(0f, moveSpeed)
            : Vector3.zero;
    }

    private void MoveWithMomentum(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - transform.position;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        float maxSpeed = Mathf.Max(0f, moveSpeed);
        Vector3 desiredVelocity = toTarget.normalized * maxSpeed;

        if (useMomentum)
        {
            float impact = Mathf.Clamp01(lastVelocityImpact);
            Vector3 carriedVelocity = currentVelocity * impact;
            Vector3 steeringVelocity = desiredVelocity * (1f - impact);
            Vector3 targetVelocity = carriedVelocity + steeringVelocity;

            if (targetVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                targetVelocity = targetVelocity.normalized * maxSpeed;
            }

            float adjustSpeed = Mathf.Max(0f, velocityAdjustSpeed);
            if (adjustSpeed <= 0f)
            {
                currentVelocity = targetVelocity;
            }
            else
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, adjustSpeed * Time.deltaTime);
            }
        }
        else
        {
            currentVelocity = desiredVelocity;
        }

        if (currentVelocity.sqrMagnitude <= 0.0001f)
        {
            currentVelocity = desiredVelocity;
        }

        if (rotateToDirection)
        {
            enemyGfxManager?.UpdateFlipX(currentVelocity);
        }

        transform.position += currentVelocity * Time.deltaTime;
    }

    private bool ShouldAdvanceWaypoint(Vector3 targetPosition, Vector3 segmentDirection, float waypointReachDistance)
    {
        if (Vector3.Distance(transform.position, targetPosition) <= waypointReachDistance)
        {
            return true;
        }

        if (segmentDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 waypointToPosition = transform.position - targetPosition;
        return Vector3.Dot(waypointToPosition, segmentDirection.normalized) > 0f;
    }

    private bool TryGetWaypointTargetPosition(int waypointIndex, out Vector3 targetPosition, out Vector3 segmentDirection)
    {
        targetPosition = Vector3.zero;
        segmentDirection = Vector3.zero;

        Transform waypoint = pathManager.GetWaypoint(waypointIndex);
        if (waypoint == null)
        {
            return false;
        }

        targetPosition = waypoint.position;
        segmentDirection = GetSegmentDirection(waypointIndex);

        if (!useCustomPathOffset || Mathf.Abs(pathLateralOffset) <= 0.0001f)
        {
            return true;
        }

        Vector3 lateralDirection = GetLateralDirection(segmentDirection, waypointIndex);
        targetPosition += lateralDirection * pathLateralOffset;
        return true;
    }

    private Vector3 GetSegmentDirection(int waypointIndex)
    {
        if (pathManager == null || pathManager.WaypointCount < 2)
        {
            return Vector3.zero;
        }

        Vector3 segment = Vector3.zero;
        if (waypointIndex <= 0)
        {
            Transform from = pathManager.GetWaypoint(0);
            Transform to = pathManager.GetWaypoint(1);
            if (from != null && to != null)
            {
                segment = to.position - from.position;
            }
        }
        else
        {
            Transform from = pathManager.GetWaypoint(waypointIndex - 1);
            Transform to = pathManager.GetWaypoint(waypointIndex);
            if (from != null && to != null)
            {
                segment = to.position - from.position;
            }
        }

        if (segment.sqrMagnitude <= 0.0001f && waypointIndex < pathManager.WaypointCount - 1)
        {
            Transform from = pathManager.GetWaypoint(waypointIndex);
            Transform to = pathManager.GetWaypoint(waypointIndex + 1);
            if (from != null && to != null)
            {
                segment = to.position - from.position;
            }
        }

        return segment.sqrMagnitude > 0.0001f ? segment.normalized : Vector3.zero;
    }

    private Vector3 GetLateralDirection(Vector3 segmentDirection, int waypointIndex)
    {
        Vector3 resolvedDirection = segmentDirection;
        if (resolvedDirection.sqrMagnitude <= 0.0001f && waypointIndex > 0)
        {
            resolvedDirection = GetSegmentDirection(waypointIndex - 1);
        }

        if (resolvedDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.right;
        }

        Vector3 lateral = new Vector3(-resolvedDirection.y, resolvedDirection.x, 0f);
        return lateral.sqrMagnitude > 0.0001f ? lateral.normalized : Vector3.right;
    }

    private void AdvanceToNextWaypoint()
    {
        // Once the final waypoint is reached, trigger goal handling.
        if (currentWaypointIndex >= pathManager.WaypointCount - 1)
        {
            ReachGoal();
            return;
        }

        currentWaypointIndex++;
    }

    private void ReachGoal()
    {
        if (hasReachedGoal)
        {
            return;
        }

        // Fire event first so listeners can react before optional destruction.
        hasReachedGoal = true;
        OnReachedGoal?.Invoke(this);

        if (destroyOnGoal)
        {
            CentralObjectPool.Despawn(gameObject);
        }
    }
}
