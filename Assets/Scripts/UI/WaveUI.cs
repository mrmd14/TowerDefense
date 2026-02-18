using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text waveLabel;
    [SerializeField] private Button startNextWaveButton;

    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private LivesManager livesManager;

    private void Awake()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }

        if (livesManager == null)
        {
            livesManager = FindFirstObjectByType<LivesManager>();
        }

        if (startNextWaveButton != null)
        {
            startNextWaveButton.onClick.AddListener(HandleStartWaveClicked);
        }
    }

    private void OnEnable()
    {
        TrySubscribeEvents();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveStarted -= HandleWaveChanged;
            waveManager.OnWaveCompleted -= HandleWaveChanged;
        }

        if (livesManager != null)
        {
            livesManager.OnGameOver -= HandleGameOver;
        }
    }

    private void OnDestroy()
    {
        if (startNextWaveButton != null)
        {
            startNextWaveButton.onClick.RemoveListener(HandleStartWaveClicked);
        }
    }

    private void TrySubscribeEvents()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }

        if (livesManager == null)
        {
            livesManager = FindFirstObjectByType<LivesManager>();
        }

        if (waveManager != null)
        {
            waveManager.OnWaveStarted -= HandleWaveChanged;
            waveManager.OnWaveCompleted -= HandleWaveChanged;
            waveManager.OnWaveStarted += HandleWaveChanged;
            waveManager.OnWaveCompleted += HandleWaveChanged;
        }

        if (livesManager != null)
        {
            livesManager.OnGameOver -= HandleGameOver;
            livesManager.OnGameOver += HandleGameOver;
        }
    }

    private void HandleStartWaveClicked()
    {
        waveManager?.StartWave();
        RefreshUI();
    }

    private void HandleWaveChanged(int currentWave, int totalWaves)
    {
        RefreshUI();
    }

    private void HandleGameOver()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (waveManager == null)
        {
            return;
        }

        int totalWaves = waveManager.TotalWaves;
        int displayedWave = 0;

        if (waveManager.IsWaveActive)
        {
            displayedWave = waveManager.CurrentWaveNumber;
        }
        else if (waveManager.HasMoreWaves)
        {
            displayedWave = waveManager.CurrentWaveNumber + 1;
        }
        else
        {
            displayedWave = totalWaves;
        }

        if (waveLabel != null)
        {
            waveLabel.text = $"Wave: {displayedWave}/{totalWaves}";
        }

        if (startNextWaveButton != null)
        {
            startNextWaveButton.interactable =
                !waveManager.IsWaveActive &&
                waveManager.HasMoreWaves &&
                !waveManager.IsGameOver;
        }
    }
}
