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

    private void OnEnable()
    {
        pathManager = PathManager.Instance ?? FindFirstObjectByType<PathManager>();
        currentWaypointIndex = 0;
        hasReachedGoal = false;

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

    private void Update()
    {
        if (!enabled || hasReachedGoal)
        {
            return;
        }

        // Read the active waypoint and move toward it each frame.
        Transform targetWaypoint = pathManager.GetWaypoint(currentWaypointIndex);
        if (targetWaypoint == null)
        {
            Debug.LogError($"Waypoint {currentWaypointIndex} is null. Skipping to next waypoint.", this);
            AdvanceToNextWaypoint();
            return;
        }

        float waypointReachDistance = Mathf.Max(0f, reachDistance);

        // If we start exactly on a waypoint, advance first so we do not stall on spawn.
        if (ShouldAdvanceWaypoint(targetWaypoint, waypointReachDistance))
        {
            AdvanceToNextWaypoint();
            if (hasReachedGoal)
            {
                return;
            }

            targetWaypoint = pathManager.GetWaypoint(currentWaypointIndex);
            if (targetWaypoint == null)
            {
                Debug.LogError($"Waypoint {currentWaypointIndex} is null. Skipping to next waypoint.", this);
                AdvanceToNextWaypoint();
                return;
            }
        }

        MoveWithMomentum(targetWaypoint.position);

        if (ShouldAdvanceWaypoint(targetWaypoint, waypointReachDistance))
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

        Transform initialTarget = pathManager.GetWaypoint(1);
        if (initialTarget == null)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        Vector3 toInitialTarget = initialTarget.position - transform.position;
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

    private bool ShouldAdvanceWaypoint(Transform targetWaypoint, float waypointReachDistance)
    {
        if (targetWaypoint == null)
        {
            return true;
        }

        if (Vector3.Distance(transform.position, targetWaypoint.position) <= waypointReachDistance)
        {
            return true;
        }

        if (currentWaypointIndex <= 0)
        {
            return false;
        }

        Transform previousWaypoint = pathManager.GetWaypoint(currentWaypointIndex - 1);
        if (previousWaypoint == null)
        {
            return false;
        }

        Vector3 segmentDirection = targetWaypoint.position - previousWaypoint.position;
        if (segmentDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 waypointToPosition = transform.position - targetWaypoint.position;
        return Vector3.Dot(waypointToPosition, segmentDirection.normalized) > 0f;
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
