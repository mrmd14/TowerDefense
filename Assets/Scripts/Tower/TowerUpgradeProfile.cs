using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Tower Upgrade Profile", fileName = "TowerUpgradeProfile")]
public class TowerUpgradeProfile : ScriptableObject
{
    [Serializable]
    public struct Stage
    {
        [Min(0)] public int cost;
        [Min(0f)] public float range;
        [Min(1)] public int power;
        [Min(0f)] public float cooldown;
    }

    [SerializeField] private List<Stage> stages = new List<Stage>();

    public int StageCount => stages != null ? stages.Count : 0;
    public bool HasStages => StageCount > 0;

    public Stage GetStage(int index)
    {
        if (!TryGetStage(index, out Stage stage))
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Stage index must be in [0..{Mathf.Max(0, StageCount - 1)}].");
        }

        return stage;
    }

    public bool TryGetStage(int index, out Stage stage)
    {
        if (stages == null || index < 0 || index >= stages.Count)
        {
            stage = default;
            return false;
        }

        stage = stages[index];
        return true;
    }

    private void OnValidate()
    {
        if (stages == null)
        {
            stages = new List<Stage>();
            return;
        }

        for (int i = 0; i < stages.Count; i++)
        {
            Stage stage = stages[i];
            stage.cost = Mathf.Max(0, stage.cost);
            stage.range = Mathf.Max(0f, stage.range);
            stage.power = Mathf.Max(1, stage.power);
            stage.cooldown = Mathf.Max(0f, stage.cooldown);
            stages[i] = stage;
        }
    }
}
