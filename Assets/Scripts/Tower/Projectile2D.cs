using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float hitDistance = 0.15f;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 4f;

    [Header("Hit")]
    [SerializeField] private bool useTriggerHit = true;

    private Transform target;
    private int damage;
    private float lifetimeTimer;
    private bool hasHit;

    public void Init(Transform targetTransform, int damageAmount)
    {
        target = targetTransform;
        damage = Mathf.Max(1, damageAmount);
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
            Destroy(gameObject);
            return;
        }

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = target.position;
        Vector3 toTarget = targetPosition - currentPosition;

        float hitDistanceSqr = hitDistance * hitDistance;
        if (toTarget.sqrMagnitude <= hitDistanceSqr)
        {
            Impact(target);
            return;
        }

        float moveStep = Mathf.Max(0f, speed) * Time.deltaTime;
        if (moveStep <= 0f)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(currentPosition, targetPosition, moveStep);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTriggerHit || hasHit || other == null)
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
        if (useTriggerHit || hasHit || collision == null)
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
        if (hasHit)
        {
            return;
        }

        hasHit = true;

        IDamageable damageable = hitTransform.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(damage);

        Destroy(gameObject);
    }
}
