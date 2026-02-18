using UnityEngine;

public class ArrowProjectile2D : Projectile2D
{
    [Header("Arrow Rotation")]
    [SerializeField] private float zRotationOffset = 0f;
    [SerializeField] private float minDirectionSqrMagnitude = 0.000001f;

    private Vector3 previousPosition;

    private void OnEnable()
    {
        previousPosition = transform.position;
    }

    private void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        Vector2 movementDirection = currentPosition - previousPosition;

        if (movementDirection.sqrMagnitude > Mathf.Max(0f, minDirectionSqrMagnitude))
        {
            float zAngle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, zAngle + zRotationOffset);
        }

        previousPosition = currentPosition;
    }
}
