using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Tower Data", fileName = "TowerData")]
public class TowerData : ScriptableObject
{
    [Header("Core")]
    [SerializeField] private GameObject prefab;

    [Tooltip("Fallback build cost when the tower prefab has no upgrade stage 0.")]
    [SerializeField] private int cost = 10;

    [Header("Button Sprites")]
    [SerializeField] private Sprite affordableButtonSprite;
    [SerializeField] private Sprite unaffordableButtonSprite;

    [Header("Combat")]
    [Tooltip("Fallback range when the tower prefab has no upgrade stage 0.")]
    [SerializeField] private float range = 3.5f;

    [Header("Placement")]
    [SerializeField] private Vector2Int footprintSize = Vector2Int.one;

    public GameObject Prefab => prefab;

    public int Cost => TryGetInitialUpgradeCost(out int stageCost) ? stageCost : cost;
    public Sprite AffordableButtonSprite => affordableButtonSprite;
    public Sprite UnaffordableButtonSprite => unaffordableButtonSprite;
    public float Range => TryGetInitialUpgradeRange(out float stageRange) ? stageRange : range;
    public Vector2Int FootprintSize => footprintSize;

    private void OnValidate()
    {
        cost = Mathf.Max(0, cost);
        range = Mathf.Max(0f, range);
        footprintSize.x = Mathf.Max(1, footprintSize.x);
        footprintSize.y = Mathf.Max(1, footprintSize.y);
    }

    private bool TryGetTowerController(out TowerController2D towerController)
    {
        towerController = null;
        if (prefab == null)
        {
            return false;
        }

        towerController = prefab.GetComponent<TowerController2D>();
        return towerController != null;
    }

    private bool TryGetInitialUpgradeCost(out int stageCost)
    {
        stageCost = 0;
        if (!TryGetTowerController(out TowerController2D towerController))
        {
            return false;
        }

        return towerController.TryGetInitialUpgradeCost(out stageCost);
    }

    private bool TryGetInitialUpgradeRange(out float stageRange)
    {
        stageRange = 0f;
        if (!TryGetTowerController(out TowerController2D towerController))
        {
            return false;
        }

        return towerController.TryGetInitialUpgradeRange(out stageRange);
    }
}
