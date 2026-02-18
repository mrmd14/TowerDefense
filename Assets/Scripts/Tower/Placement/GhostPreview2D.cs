using System.Collections.Generic;
using UnityEngine;

public class GhostPreview2D : MonoBehaviour
{
    [SerializeField] private Color validTint = new Color(0.3f, 1f, 0.3f, 0.55f);
    [SerializeField] private Color invalidTint = new Color(1f, 0.3f, 0.3f, 0.55f);
    [SerializeField] private string ghostLayerName = "Ghost";
    [SerializeField] private int sortingOrderOffset = 50;

    private TowerData currentTowerData;
    private GameObject ghostInstance;
    private readonly List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    private readonly List<Color> baseColors = new List<Color>();

    public void SetTower(TowerData towerData)
    {
        if (currentTowerData == towerData && ghostInstance != null)
        {
            return;
        }

        currentTowerData = towerData;
        DestroyGhost();

        if (towerData == null || towerData.Prefab == null)
        {
            return;
        }

        ghostInstance = Instantiate(towerData.Prefab, transform);
        ghostInstance.name = $"{towerData.Prefab.name}_Ghost";

        PrepareGhostInstance(ghostInstance);
        CacheRenderers(ghostInstance);
        SetValidity(true);
    }

    public void SetPosition(Vector3 worldPosition)
    {
        if (ghostInstance == null)
        {
            return;
        }

        ghostInstance.transform.position = worldPosition;
    }

    public void SetValidity(bool isValid)
    {
        ApplyTint(isValid ? validTint : invalidTint);
    }

    public void SetPlacementValidity(bool canPlace, bool canAfford)
    {
        SetValidity(canPlace && canAfford);
    }

    public void Show()
    {
        if (ghostInstance != null)
        {
            ghostInstance.SetActive(true);
        }
    }

    public void Hide()
    {
        if (ghostInstance != null)
        {
            ghostInstance.SetActive(false);
        }
    }

    public void Clear()
    {
        currentTowerData = null;
        DestroyGhost();
    }

    private void OnDestroy()
    {
        DestroyGhost();
    }

    private void DestroyGhost()
    {
        spriteRenderers.Clear();
        baseColors.Clear();

        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }
    }

    private void PrepareGhostInstance(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody2D[] bodies = root.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].simulated = false;
            bodies[i].linearVelocity = Vector2.zero;
            bodies[i].angularVelocity = 0f;
        }

        Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            behaviours[i].enabled = false;
        }

        int ghostLayer = LayerMask.NameToLayer(ghostLayerName);
        if (ghostLayer >= 0)
        {
            SetLayerRecursively(root.transform, ghostLayer);
        }
    }

    private void CacheRenderers(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = renderers[i];
            spriteRenderer.sortingOrder += sortingOrderOffset;
            spriteRenderers.Add(spriteRenderer);
            baseColors.Add(spriteRenderer.color);
        }
    }

    private void ApplyTint(Color tint)
    {
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            Color baseColor = baseColors[i];
            spriteRenderer.color = new Color(
                baseColor.r * tint.r,
                baseColor.g * tint.g,
                baseColor.b * tint.b,
                baseColor.a * tint.a
            );
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
