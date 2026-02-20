using TMPro;
using UnityEngine;

public class LivesUI : MonoBehaviour
{
    [SerializeField] private TMP_Text livesLabel;
    [SerializeField] private LivesManager livesManager;

    private void Awake()
    {
        if (livesManager == null)
        {
            livesManager = FindFirstObjectByType<LivesManager>();
        }
    }

    private void OnEnable()
    {
        TrySubscribeLives();
        RefreshLabel();
    }

    private void OnDisable()
    {
        if (livesManager != null)
        {
            livesManager.OnLivesChanged -= HandleLivesChanged;
        }
    }

    private void TrySubscribeLives()
    {
        if (livesManager == null)
        {
            livesManager = FindFirstObjectByType<LivesManager>();
        }

        if (livesManager == null)
        {
            return;
        }

        livesManager.OnLivesChanged -= HandleLivesChanged;
        livesManager.OnLivesChanged += HandleLivesChanged;
    }

    private void HandleLivesChanged(int lives)
    {
        if (livesLabel == null)
        {
            return;
        }

        livesLabel.text = $"{lives}";
    }

    private void RefreshLabel()
    {
        if (livesManager == null)
        {
            return;
        }

        HandleLivesChanged(livesManager.Lives);
    }
}
