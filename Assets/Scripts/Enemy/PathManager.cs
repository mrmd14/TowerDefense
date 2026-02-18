using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance { get; private set; }

    [Header("Waypoints")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("Options")]

    [SerializeField] private bool drawPathGizmos = true;
    [SerializeField] private Color pathColor = Color.cyan;

    public int WaypointCount => waypoints?.Count ?? 0;

    private void Awake()
    {


        Instance = this;

        ValidateWaypoints();
    }

    public Transform GetWaypoint(int index)
    {
        if (index < 0 || index >= WaypointCount)
        {
            Debug.LogError($"Waypoint index {index} is out of range [0..{WaypointCount - 1}].", this);
            return null;
        }

        return waypoints[index];
    }

    public Vector3 GetWaypointPosition(int index)
    {
        Transform waypoint = GetWaypoint(index);
        if (waypoint == null)
        {
            Debug.LogError($"Waypoint at index {index} is null.", this);
            return Vector3.zero;
        }

        return waypoint.position;
    }

    private void ValidateWaypoints()
    {
        if (WaypointCount == 0)
        {
            Debug.LogError("PathManager has no waypoints assigned.", this);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawPathGizmos || waypoints == null || waypoints.Count == 0)
        {
            return;
        }

        Gizmos.color = pathColor;

        // Draw spheres and connecting line segments in waypoint order.
        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform current = waypoints[i];
            if (current == null)
            {
                continue;
            }

            Gizmos.DrawSphere(current.position, 0.15f);

            if (i < waypoints.Count - 1)
            {
                Transform next = waypoints[i + 1];
                if (next != null)
                {
                    Gizmos.DrawLine(current.position, next.position);
                }
            }
        }
    }
}
