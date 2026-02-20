using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Tilemaps;

public class TilemapPlacement2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera placementCamera;
    [SerializeField] private Tilemap buildableTilemap;
    [SerializeField] private Tilemap blockedTilemap;
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private GhostPreview2D ghostPreview;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private MoneyUI moneyUI;
    [SerializeField] private Transform towerRangeVisual;

    [Header("Input Actions (New Input System)")]
    [SerializeField] private InputAction pointAction = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
    [SerializeField] private InputAction pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");

    [Header("Placement Rules")]
    [Tooltip("Include Tower + Path + Blocking layers.")]
    [SerializeField] private LayerMask placementBlockingLayers;
    [SerializeField, Range(0.1f, 1f)] private float overlapBoxSizeMultiplier = 0.95f;
    [Tooltip("Allows slight overlap near the bottom edge of blocked tiles.")]
    [SerializeField, Min(0f)] private float blockedTileYMargin = 0.1f;
    [SerializeField] private bool allowPcCancelInput = true;
    [SerializeField] private LayerMask towerSelectionLayers;

    [Header("Range Visual")]
    [SerializeField] private string towerRangeObjectName = "towerRange";
    [SerializeField] private float rangeVisualUnitRadius = 1f;

    private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    private TowerController2D selectedPlacedTower;
    private Vector3 rangeVisualBaseScale = Vector3.one;
    private bool hasRangeVisualBaseScale;

    public IReadOnlyCollection<Vector3Int> OccupiedCells => occupiedCells;

    private void Reset()
    {
        placementCamera = Camera.main;
        towerSelectionLayers = LayerMask.GetMask("Tower");
    }

    private void Awake()
    {
        if (placementCamera == null)
        {
            placementCamera = Camera.main;
        }

        if (buildManager == null)
        {
            buildManager = FindFirstObjectByType<BuildManager>();
        }

        if (ghostPreview == null)
        {
            ghostPreview = FindFirstObjectByType<GhostPreview2D>();
        }

        if (currencyManager == null)
        {
            currencyManager = CurrencyManager.Instance ?? FindFirstObjectByType<CurrencyManager>();
        }

        if (moneyUI == null)
        {
            moneyUI = MoneyUI.Instance ?? FindFirstObjectByType<MoneyUI>();
        }

        if (towerSelectionLayers == 0)
        {
            int towerLayer = LayerMask.NameToLayer("Tower");
            towerSelectionLayers = towerLayer >= 0 ? 1 << towerLayer : Physics2D.AllLayers;
        }

        EnsureRangeVisualReference();
        CacheRangeVisualBaseScale();
        HideRangeVisual();
    }

    private void OnEnable()
    {
        pressAction.performed += OnPressPerformed;
        pointAction.Enable();
        pressAction.Enable();

        if (buildManager != null)
        {
            buildManager.OnSelectedTowerChanged += OnSelectedTowerChanged;
        }

        SyncGhostWithSelection();
        HideRangeVisual();
    }

    private void OnDisable()
    {
        pressAction.performed -= OnPressPerformed;
        pointAction.Disable();
        pressAction.Disable();

        if (buildManager != null)
        {
            buildManager.OnSelectedTowerChanged -= OnSelectedTowerChanged;
        }

        HideRangeVisual();
    }

    private void Update()
    {
        if (allowPcCancelInput)
        {
            HandleCancelInput();
        }

        UpdateGhostPreview();
    }

    public bool IsCellOccupied(Vector3Int cell)
    {
        return occupiedCells.Contains(cell);
    }

    public void RegisterOccupiedCell(Vector3Int cell)
    {
        occupiedCells.Add(cell);
    }

    public void UnregisterOccupiedCell(Vector3Int cell)
    {
        occupiedCells.Remove(cell);
    }

    private void OnSelectedTowerChanged(TowerData selectedTower)
    {
        if (ghostPreview == null)
        {
            return;
        }

        if (selectedTower == null)
        {
            ghostPreview.Clear();
            if (selectedPlacedTower == null)
            {
                HideRangeVisual();
            }
            return;
        }

        selectedPlacedTower = null;
        ghostPreview.SetTower(selectedTower);
    }

    private void SyncGhostWithSelection()
    {
        if (buildManager == null)
        {
            return;
        }

        OnSelectedTowerChanged(buildManager.SelectedTower);
    }

    private void HandleCancelInput()
    {
        if (buildManager == null || !buildManager.HasSelection)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            selectedPlacedTower = null;
            HideRangeVisual();
            buildManager.CancelSelection();
            return;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            selectedPlacedTower = null;
            HideRangeVisual();
            buildManager.CancelSelection();
        }
    }

    private void UpdateGhostPreview()
    {
        if (ghostPreview == null || buildManager == null)
        {
            return;
        }

        TowerData selectedTower = buildManager.SelectedTower;
        if (selectedTower == null)
        {
            ghostPreview.Hide();
            if (selectedPlacedTower == null)
            {
                HideRangeVisual();
            }
            else
            {
                ShowRangeVisual(selectedPlacedTower.transform.position, selectedPlacedTower.Range);
            }
            return;
        }

        selectedPlacedTower = null;

        if (!TryGetPointerCell(out Vector3Int cell, out Vector3 cellCenter))
        {
            ghostPreview.Hide();
            HideRangeVisual();
            return;
        }

        bool canPlace = CanPlaceAt(cell, cellCenter, selectedTower);
        bool canAfford = CanAfford(selectedTower.Cost);
        ghostPreview.Show();
        ghostPreview.SetPosition(cellCenter);
        ghostPreview.SetPlacementValidity(canPlace, canAfford);
        ShowRangeVisual(cellCenter, GetTowerRange(selectedTower));
    }

    private void OnPressPerformed(InputAction.CallbackContext context)
    {
        if (!context.ReadValueAsButton())
        {
            return;
        }

        if (buildManager == null)
        {
            return;
        }

        if (IsPointerOverUI(context))
        {
            return;
        }

        if (buildManager.SelectedTower == null)
        {
            if (TrySelectPlacedTowerAtPointer())
            {
                return;
            }

            selectedPlacedTower = null;
            HideRangeVisual();
            return;
        }

        if (!TryGetPointerCell(out Vector3Int cell, out Vector3 cellCenter))
        {
            return;
        }

        TryPlaceAt(cell, cellCenter);
    }

    private bool TryPlaceAt(Vector3Int cell, Vector3 cellCenter)
    {
        TowerData selectedTower = buildManager.SelectedTower;
        if (!CanPlaceAt(cell, cellCenter, selectedTower))
        {
            return false;
        }

        if (!CanAfford(selectedTower.Cost))
        {
            ShowNotEnoughMoneyFeedback();
            return false;
        }

        if (!currencyManager.TrySpend(selectedTower.Cost))
        {
            ShowNotEnoughMoneyFeedback();
            return false;
        }

        GameObject spawnedTowerObject = CentralObjectPool.Spawn(selectedTower.Prefab, cellCenter, Quaternion.identity);
        if (spawnedTowerObject != null)
        {
            TowerController2D towerController = spawnedTowerObject.GetComponent<TowerController2D>();
            if (towerController != null)
            {
                towerController.SetRange(selectedTower.Range);
            }
        }

        occupiedCells.Add(cell);
        selectedPlacedTower = null;
        HideRangeVisual();
        buildManager.CancelSelection();
        return true;
    }

    private bool CanAfford(int cost)
    {
        if (currencyManager == null)
        {
            currencyManager = CurrencyManager.Instance ?? FindFirstObjectByType<CurrencyManager>();
        }

        return currencyManager != null && currencyManager.CanAfford(cost);
    }

    private void ShowNotEnoughMoneyFeedback()
    {
        if (moneyUI == null)
        {
            moneyUI = MoneyUI.Instance ?? FindFirstObjectByType<MoneyUI>();
        }

        if (moneyUI != null)
        {
            moneyUI.ShowNotEnoughMoney();
            return;
        }

        Debug.Log("Not enough money");
    }

    private bool CanPlaceAt(Vector3Int cell, Vector3 cellCenter, TowerData towerData)
    {
        if (towerData == null || towerData.Prefab == null)
        {

            return false;
        }

        if (buildableTilemap == null)
        {

            return false;
        }






        if (blockedTilemap != null && IsBlockedByTilemap(cellCenter))
        {
            return false;
        }

        if (occupiedCells.Contains(cell))
        {

            return false;
        }

        if (HasPhysicsOverlap(cellCenter, towerData))
        {

            return false;
        }
        print(6);
        return true;
    }

    private bool IsBlockedByTilemap(Vector3 worldPosition)
    {
        Vector3Int blockedCell = blockedTilemap.WorldToCell(worldPosition);
        if (!blockedTilemap.HasTile(blockedCell))
        {
            return false;
        }

        if (blockedTileYMargin <= 0f)
        {
            return true;
        }

        Vector3 blockedCellCenter = blockedTilemap.GetCellCenterWorld(blockedCell);
        float halfCellHeight = Mathf.Abs(blockedTilemap.layoutGrid.cellSize.y) * 0.5f;
        float effectiveMargin = Mathf.Min(blockedTileYMargin, halfCellHeight);
        float bottomEdgeY = blockedCellCenter.y - halfCellHeight;
        float distanceFromBottom = worldPosition.y - bottomEdgeY;

        return distanceFromBottom > effectiveMargin;
    }

    private bool HasPhysicsOverlap(Vector3 cellCenter, TowerData towerData)
    {
        Vector2 overlapSize = GetFootprintWorldSize(towerData) * Mathf.Clamp01(overlapBoxSizeMultiplier);
        Collider2D hit = Physics2D.OverlapBox(cellCenter, overlapSize, 0f, placementBlockingLayers);
        return hit != null;
    }

    private Vector2 GetFootprintWorldSize(TowerData towerData)
    {
        Vector3 cellSize = buildableTilemap.layoutGrid.cellSize;
        Vector2Int footprint = towerData.FootprintSize;

        return new Vector2(
            Mathf.Abs(cellSize.x) * Mathf.Max(1, footprint.x),
            Mathf.Abs(cellSize.y) * Mathf.Max(1, footprint.y)
        );
    }

    private float GetTowerRange(TowerData towerData)
    {
        if (towerData == null)
        {
            return 0f;
        }

        return towerData.Range;
    }

    private bool TrySelectPlacedTowerAtPointer()
    {
        if (!TryGetPointerWorld(out Vector3 pointerWorld))
        {
            return false;
        }

        Collider2D towerHit = Physics2D.OverlapPoint(pointerWorld, towerSelectionLayers);
        if (towerHit == null)
        {
            return false;
        }

        TowerController2D tower = towerHit.GetComponentInParent<TowerController2D>();
        if (tower == null)
        {
            return false;
        }

        selectedPlacedTower = tower;
        ShowRangeVisual(tower.transform.position, tower.Range);
        return true;
    }

    private bool TryGetPointerCell(out Vector3Int cell, out Vector3 cellCenter)
    {
        cell = Vector3Int.zero;
        cellCenter = Vector3.zero;

        if (buildableTilemap == null || !TryGetPointerWorld(out Vector3 pointerWorld))
        {
            return false;
        }

        cell = buildableTilemap.WorldToCell(pointerWorld);
        cellCenter = buildableTilemap.GetCellCenterWorld(cell);
        cellCenter.z = 0f;
        return true;
    }

    private bool TryGetPointerWorld(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (placementCamera == null)
        {
            return false;
        }

        Vector2 screenPos = pointAction.ReadValue<Vector2>();
        worldPosition = placementCamera.ScreenToWorldPoint(new Vector3(
            screenPos.x,
            screenPos.y,
            Mathf.Abs(placementCamera.transform.position.z))
        );
        worldPosition.z = 0f;
        return true;
    }

    private void EnsureRangeVisualReference()
    {
        if (towerRangeVisual != null || string.IsNullOrWhiteSpace(towerRangeObjectName))
        {
            return;
        }

        Transform fallback = null;
        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform sceneTransform = sceneTransforms[i];
            if (sceneTransform == null || sceneTransform.name != towerRangeObjectName)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = sceneTransform;
            }

            if (sceneTransform.parent == null)
            {
                towerRangeVisual = sceneTransform;
                return;
            }
        }

        towerRangeVisual = fallback;
    }

    private void CacheRangeVisualBaseScale()
    {
        if (towerRangeVisual == null)
        {
            return;
        }

        rangeVisualBaseScale = towerRangeVisual.localScale;
        hasRangeVisualBaseScale = true;
    }

    private void ShowRangeVisual(Vector3 worldPosition, float range)
    {
        if (towerRangeVisual == null)
        {
            return;
        }

        if (!hasRangeVisualBaseScale)
        {
            CacheRangeVisualBaseScale();
        }

        float safeUnitRadius = Mathf.Max(0.0001f, rangeVisualUnitRadius);
        float scaleMultiplier = Mathf.Max(0f, range) / safeUnitRadius;

        towerRangeVisual.position = new Vector3(worldPosition.x, worldPosition.y, towerRangeVisual.position.z);
        towerRangeVisual.localScale = new Vector3(
            rangeVisualBaseScale.x * scaleMultiplier,
            rangeVisualBaseScale.y * scaleMultiplier,
            rangeVisualBaseScale.z
        );

        SetRangeVisualActive(true);
    }

    private void HideRangeVisual()
    {
        SetRangeVisualActive(false);
    }

    private void SetRangeVisualActive(bool isActive)
    {
        if (towerRangeVisual == null)
        {
            return;
        }

        if (towerRangeVisual.gameObject.activeSelf != isActive)
        {
            towerRangeVisual.gameObject.SetActive(isActive);
        }
    }

    private static bool IsPointerOverUI(InputAction.CallbackContext context)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        if (context.control?.device is Touchscreen touchscreen)
        {
            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                TouchControl touch = touchscreen.touches[i];
                if (!touch.press.isPressed)
                {
                    continue;
                }

                int touchId = touch.touchId.ReadValue();
                if (eventSystem.IsPointerOverGameObject(touchId))
                {
                    return true;
                }
            }
        }

        if (eventSystem.IsPointerOverGameObject())
        {
            return true;
        }

        if (Pointer.current != null && eventSystem.IsPointerOverGameObject(Pointer.current.deviceId))
        {
            return true;
        }

        return false;
    }
}
