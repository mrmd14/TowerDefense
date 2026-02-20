using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyDeathVfx : MonoBehaviour
{
    private static EnemyDeathVfx instance;

    private const string BloodStainResourcePath = "Prefabs/EnemyDeathBloodStain";
    private const string DeathWordResourcePath = "Prefabs/EnemyDeathWordText";

    private static readonly string[] DefaultWords =
    {
        "BLOD",
        "SMASH",
        "CRIT",
        "OUCH",
        "POW"
    };

    [Header("Prefabs")]
    [SerializeField] private GameObject bloodStainPrefab;
    [SerializeField] private GameObject deathWordPrefab;

    [Header("Spawn Offsets")]
    [SerializeField] private Vector3 bloodOffset = new Vector3(0f, -0.08f, 0f);
    [SerializeField, Min(0f)] private float wordVerticalOffset = 0.35f;

    [Header("Words")]
    [SerializeField] private List<string> deathWords = new List<string>(DefaultWords);

    [Header("Sorting")]
    [SerializeField] private int bloodSortingOrder = 2;
    [SerializeField] private int wordSortingOrder = 25;

    [Header("Lifecycle")]
    [SerializeField] private bool persistAcrossScenes = true;

    public static EnemyDeathVfx Instance => EnsureInstance();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        ResolvePrefabs();
        EnsureWords();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnValidate()
    {
        ResolvePrefabs();
        EnsureWords();
    }

    public static void PlayFor(Transform enemyTransform, Collider2D enemyCollider = null, Renderer enemyRenderer = null)
    {
        if (enemyTransform == null)
        {
            return;
        }

        EnsureInstance().PlayDeathVfxInternal(enemyTransform.position, enemyCollider, enemyRenderer);
    }

    private static EnemyDeathVfx EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<EnemyDeathVfx>();
        if (instance != null)
        {
            instance.ResolvePrefabs();
            instance.EnsureWords();
            return instance;
        }

        GameObject singletonObject = new GameObject(nameof(EnemyDeathVfx));
        instance = singletonObject.AddComponent<EnemyDeathVfx>();
        return instance;
    }

    private void PlayDeathVfxInternal(Vector3 deathPosition, Collider2D enemyCollider, Renderer enemyRenderer)
    {
        ResolvePrefabs();
        string selectedWord = ChooseWord();

        SpawnBlood(deathPosition + bloodOffset);
        SpawnWord(GetWordSpawnPosition(deathPosition, enemyCollider, enemyRenderer), selectedWord);
    }

    private void SpawnBlood(Vector3 worldPosition)
    {
        if (bloodStainPrefab == null)
        {
            return;
        }

        float randomRotation = Random.Range(0f, 360f);
        GameObject bloodObject = CentralObjectPool.Spawn(
            bloodStainPrefab,
            worldPosition,
            Quaternion.Euler(0f, 0f, randomRotation));
        if (bloodObject == null)
        {
            return;
        }

        BloodStainVfx bloodStainVfx = bloodObject.GetComponent<BloodStainVfx>();
        bloodStainVfx?.SetSortingOrder(bloodSortingOrder);
    }

    private void SpawnWord(Vector3 worldPosition, string selectedWord)
    {
        if (deathWordPrefab == null)
        {
            return;
        }

        GameObject wordObject = CentralObjectPool.Spawn(deathWordPrefab, worldPosition, Quaternion.identity);
        if (wordObject == null)
        {
            return;
        }

        DeathWordPopupVfx popupVfx = wordObject.GetComponent<DeathWordPopupVfx>();
        if (popupVfx != null)
        {
            popupVfx.Play(selectedWord, wordSortingOrder);
            return;
        }

        TextMeshPro textMesh = wordObject.GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = selectedWord;
            textMesh.sortingOrder = wordSortingOrder;
        }
    }

    private Vector3 GetWordSpawnPosition(Vector3 deathPosition, Collider2D enemyCollider, Renderer enemyRenderer)
    {
        float highestY = deathPosition.y;

        if (enemyCollider != null)
        {
            highestY = Mathf.Max(highestY, enemyCollider.bounds.max.y);
        }
        else if (enemyRenderer != null)
        {
            highestY = Mathf.Max(highestY, enemyRenderer.bounds.max.y);
        }

        return new Vector3(deathPosition.x, highestY + Mathf.Max(0f, wordVerticalOffset), deathPosition.z);
    }

    private string ChooseWord()
    {
        if (deathWords == null || deathWords.Count == 0)
        {
            return DefaultWords[0];
        }

        int startIndex = Random.Range(0, deathWords.Count);
        for (int i = 0; i < deathWords.Count; i++)
        {
            int index = (startIndex + i) % deathWords.Count;
            string candidate = deathWords[index];
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        return DefaultWords[0];
    }

    private void ResolvePrefabs()
    {
        if (bloodStainPrefab == null)
        {
            bloodStainPrefab = Resources.Load<GameObject>(BloodStainResourcePath);
        }

        if (deathWordPrefab == null)
        {
            deathWordPrefab = Resources.Load<GameObject>(DeathWordResourcePath);
        }
    }

    private void EnsureWords()
    {
        if (deathWords == null || deathWords.Count == 0)
        {
            deathWords = new List<string>(DefaultWords);
        }
    }
}
