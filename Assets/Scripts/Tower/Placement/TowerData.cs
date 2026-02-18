using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Tower Data", fileName = "TowerData")]
public class TowerData : ScriptableObject
{
    [Header("Core")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite icon;
    [SerializeField] private int cost = 10;

    [Header("Placement")]
    [SerializeField] private Vector2Int footprintSize = Vector2Int.one;

    public GameObject Prefab => prefab;
    public Sprite Icon => icon;
    public int Cost => cost;
    public Vector2Int FootprintSize => footprintSize;

    private void OnValidate()
    {
        cost = Mathf.Max(0, cost);
        footprintSize.x = Mathf.Max(1, footprintSize.x);
        footprintSize.y = Mathf.Max(1, footprintSize.y);
    }
}
