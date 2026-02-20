using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Game Play Data", fileName = "GamePlayData")]
public class GamePlayData : ScriptableObject
{
    [Header("Starting Values")]
    [SerializeField, Min(1)] private int startingLives = 20;
    [SerializeField, Min(0)] private int startingMoney = 100;

    public int StartingLives => Mathf.Max(1, startingLives);
    public int StartingMoney => Mathf.Max(0, startingMoney);

    private void OnValidate()
    {
        startingLives = Mathf.Max(1, startingLives);
        startingMoney = Mathf.Max(0, startingMoney);
    }
}
