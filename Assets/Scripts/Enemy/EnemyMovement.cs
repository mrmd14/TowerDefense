using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private bool rotateToDirection = true;
    [SerializeField] private float reachDistance = 0.1f;
    [SerializeField] private bool destroyOnGoal = true;

    [Header("Visual")]
    [SerializeField] private EnemyGfxManager enemyGfxManager;

    public event Action<EnemyMovement> OnReachedGoal;

    private PathManager pathManager;
    private int currentWaypointIndex;
    private bool hasReachedGoal;

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

        MoveTowards(targetWaypoint.position);

        if (Vector3.Distance(transform.position, targetWaypoint.position) <= Mathf.Max(0f, reachDistance))
        {
            AdvanceToNextWaypoint();
        }
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - transform.position;

        if (rotateToDirection && toTarget.sqrMagnitude > 0.0001f)
        {
            enemyGfxManager?.UpdateFlipX(toTarget);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            Mathf.Max(0f, moveSpeed) * Time.deltaTime
        );
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
            CentralObjectPool.DespawnEnemy(this);
        }
    }
}
