using UnityEngine;

public class ArrowTowerController2D : TowerController2D
{
    [Header("Archer Visuals")]
    [SerializeField] private Animator archerAnimator;
    [SerializeField] private SpriteRenderer archerRenderer;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string shootStateName = "Shoot";

    private void Reset()
    {
        if (archerAnimator == null)
        {
            archerAnimator = GetComponentInChildren<Animator>();
        }

        if (archerRenderer == null)
        {
            archerRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        PlayIdleAnimation();
    }

    protected override void Shoot(Transform target)
    {
        UpdateFacing(target);
        PlayShootAnimation();
        base.Shoot(target);
    }

    private void UpdateFacing(Transform target)
    {
        if (archerRenderer == null || target == null)
        {
            return;
        }

        archerRenderer.flipX = target.position.x < transform.position.x;
    }

    private void PlayShootAnimation()
    {
        if (archerAnimator == null || string.IsNullOrEmpty(shootStateName))
        {
            return;
        }

        archerAnimator.Play(shootStateName, 0, 0f);
    }

    private void PlayIdleAnimation()
    {
        if (archerAnimator == null || string.IsNullOrEmpty(idleStateName))
        {
            return;
        }

        archerAnimator.Play(idleStateName, 0, 0f);
    }
}
