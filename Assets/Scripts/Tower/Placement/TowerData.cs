using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Tower Data", fileName = "TowerData")]
public class TowerData : ScriptableObject
{
    [Header("Core")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite icon;
    [SerializeField] private int cost = 10;

    [Header("Button Sprites")]
    [SerializeField] private Sprite affordableButtonSprite;
    [SerializeField] private Sprite unaffordableButtonSprite;

    [Header("Combat")]
    [SerializeField] private float range = 3.5f;

    [Header("Placement")]
    [SerializeField] private Vector2Int footprintSize = Vector2Int.one;

    public GameObject Prefab => prefab;
    public Sprite Icon => icon;
    public int Cost => cost;
    public Sprite AffordableButtonSprite => affordableButtonSprite;
    public Sprite UnaffordableButtonSprite => unaffordableButtonSprite;
    public float Range => range;
    public Vector2Int FootprintSize => footprintSize;

    private void OnValidate()
    {
        cost = Mathf.Max(0, cost);
        range = Mathf.Max(0f, range);
        footprintSize.x = Mathf.Max(1, footprintSize.x);
        footprintSize.y = Mathf.Max(1, footprintSize.y);
    }
}
