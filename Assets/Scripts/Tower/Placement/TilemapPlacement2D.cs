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

    [Header("Input Actions (New Input System)")]
    [SerializeField] private InputAction pointAction = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
    [SerializeField] private InputAction pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");

    [Header("Placement Rules")]
    [Tooltip("Include Tower + Path + Blocking layers.")]
    [SerializeField] private LayerMask placementBlockingLayers;
    [SerializeField, Range(0.1f, 1f)] private float overlapBoxSizeMultiplier = 0.95f;
    [SerializeField] private bool allowPcCancelInput = true;

    private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

    public IReadOnlyCollection<Vector3Int> OccupiedCells => occupiedCells;

    private void Reset()
    {
        placementCamera = Camera.main;
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
            return;
        }

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
            buildManager.CancelSelection();
            return;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
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
            return;
        }

        if (!TryGetPointerCell(out Vector3Int cell, out Vector3 cellCenter))
        {
            ghostPreview.Hide();
            return;
        }

        bool canPlace = CanPlaceAt(cell, cellCenter, selectedTower);
        bool canAfford = CanAfford(selectedTower.Cost);
        ghostPreview.Show();
        ghostPreview.SetPosition(cellCenter);
        ghostPreview.SetPlacementValidity(canPlace, canAfford);
    }

    private void OnPressPerformed(InputAction.CallbackContext context)
    {
        if (!context.ReadValueAsButton())
        {
            return;
        }

        if (buildManager == null || buildManager.SelectedTower == null)
        {
            return;
        }

        if (IsPointerOverUI(context))
        {
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

        Instantiate(selectedTower.Prefab, cellCenter, Quaternion.identity);
        occupiedCells.Add(cell);
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

        if (!buildableTilemap.cellBounds.Contains(cell))
        {
            return false;
        }

        if (!buildableTilemap.HasTile(cell))
        {
            return false;
        }

        if (blockedTilemap != null && blockedTilemap.HasTile(cell))
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

        return true;
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

    private bool TryGetPointerCell(out Vector3Int cell, out Vector3 cellCenter)
    {
        cell = Vector3Int.zero;
        cellCenter = Vector3.zero;

        if (placementCamera == null || buildableTilemap == null)
        {
            return false;
        }

        Vector2 screenPos = pointAction.ReadValue<Vector2>();
        Vector3 world = placementCamera.ScreenToWorldPoint(new Vector3(
            screenPos.x,
            screenPos.y,
            Mathf.Abs(placementCamera.transform.position.z))
        );

        cell = buildableTilemap.WorldToCell(world);
        cellCenter = buildableTilemap.GetCellCenterWorld(cell);
        cellCenter.z = 0f;
        return true;
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
