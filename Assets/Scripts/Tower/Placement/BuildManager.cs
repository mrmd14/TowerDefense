using System;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    [SerializeField] private TowerData selectedTower;

    public event Action<TowerData> OnSelectedTowerChanged;

    public TowerData SelectedTower => selectedTower;
    public bool HasSelection => selectedTower != null;

    public void SelectTower(TowerData towerData)
    {
        if (selectedTower == towerData)
        {
            return;
        }

        selectedTower = towerData;
        OnSelectedTowerChanged?.Invoke(selectedTower);
    }

    public void CancelSelection()
    {
        if (selectedTower == null)
        {
            return;
        }

        selectedTower = null;
        OnSelectedTowerChanged?.Invoke(null);
    }
}
