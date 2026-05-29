using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LevelUpCardEffect
{
    None,
    DamageAdd,
    DamagePercent,
    FireIntervalAdd,
    FireIntervalPercent,
    ProjectileSpeedAdd,
    ProjectileSpeedPercent,
    ProjectileLifetimeAdd,
    ProjectileScaleAdd,
    MoveSpeedAdd,
    MoveSpeedPercent,
    MaxHpAdd,
    Heal
}

[DisallowMultipleComponent]
public class LevelUpSelectionManager : MonoBehaviour
{
    private class LevelUpCardRow
    {
        public int ID;
        public float Ratio;
        public int? Required;
        public string Icon;
        public string Desc;
        public LevelUpCardEffect Effect;
        public float? Value;
    }

    private class LevelUpCardView
    {
        public Button Button;
        public Image IconImage;
        public TMP_Text DescriptionText;
    }

    [Header("References")]
    [SerializeField] private PlayerExperiences playerExperiences;
    [SerializeField] private GameObject cardContainer;
    [SerializeField] private Button[] cardButtons;
    [SerializeField] private AutoShooter autoShooter;
    [SerializeField] private TopDownPlayerMovement playerMovement;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Data")]
    [SerializeField] private string levelUpCardCsvResourcePath = "LevelUpCard";
    [SerializeField] private string iconResourceFolder = "Sprites";

    [Header("Pause")]
    [SerializeField] private bool pauseWithTimeScale = true;

    private readonly Queue<int> pendingLevelUps = new();
    private readonly List<LevelUpCardRow> cardTable = new();
    private readonly List<LevelUpCardRow> activeCards = new();
    private readonly HashSet<int> selectedCardIds = new();
    private readonly Dictionary<string, Sprite> iconCache = new();
    private LevelUpCardView[] cardViews;
    private CanvasGroup cardContainerCanvasGroup;
    private int observedLevel;
    private int activeLevel;
    private bool isPanelOpen;
    private bool buttonsConfigured;
    private bool hasPausedTimeScale;
    private float timeScaleBeforePause = 1f;

    private void Awake()
    {
        ResolveReferences();
        LoadLevelUpCardTable();
        observedLevel = playerExperiences != null ? playerExperiences.CurrentLevel : 1;
        ConfigureCardButtons();
        BuildCardViews();
        HidePanelVisuals();
        GamePauseState.SetLevelUpSelectionOpen(false);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        ResolveReferences();
        ConfigureCardButtons();
        BuildCardViews();
        observedLevel = playerExperiences != null ? playerExperiences.CurrentLevel : observedLevel;
        Subscribe();
    }

    private void OnDisable()
    {
        if (playerExperiences != null)
        {
            playerExperiences.LevelChanged -= OnPlayerLevelChanged;
        }
    }

    private void OnDestroy()
    {
        if (isPanelOpen)
        {
            ResumeGame();
        }
    }

    private void ResolveReferences()
    {
        if (playerExperiences == null)
        {
            playerExperiences = PlayerExperiences.Instance;
        }

        GameObject player = null;
        if (playerExperiences == null || autoShooter == null || playerMovement == null || playerHealth == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerExperiences == null && player != null)
        {
            playerExperiences = player.GetComponent<PlayerExperiences>();
        }

        if (autoShooter == null && player != null)
        {
            autoShooter = player.GetComponent<AutoShooter>();
        }

        if (playerMovement == null && player != null)
        {
            playerMovement = player.GetComponent<TopDownPlayerMovement>();
        }

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (cardContainer == null)
        {
            cardContainer = gameObject.name == "CardContainer" ? gameObject : GameObject.Find("CardContainer");
        }

        if (cardContainer != null)
        {
            cardContainerCanvasGroup = cardContainer.GetComponent<CanvasGroup>();
            if (cardContainerCanvasGroup == null)
            {
                cardContainerCanvasGroup = cardContainer.AddComponent<CanvasGroup>();
            }

            if (cardButtons == null || cardButtons.Length == 0)
            {
                cardButtons = cardContainer.GetComponentsInChildren<Button>(true);
            }
        }
    }

    private void Subscribe()
    {
        if (playerExperiences == null)
        {
            ResolveReferences();
        }

        if (playerExperiences == null)
        {
            return;
        }

        playerExperiences.LevelChanged -= OnPlayerLevelChanged;
        playerExperiences.LevelChanged += OnPlayerLevelChanged;
    }

    private void ConfigureCardButtons()
    {
        if (buttonsConfigured || cardButtons == null)
        {
            return;
        }

        buttonsConfigured = true;

        for (int i = 0; i < cardButtons.Length; i++)
        {
            Button button = cardButtons[i];
            if (button == null)
            {
                continue;
            }

            int cardIndex = i;
            button.onClick.AddListener(() => SelectCard(cardIndex));
        }
    }

    private void BuildCardViews()
    {
        if (cardButtons == null)
        {
            cardViews = Array.Empty<LevelUpCardView>();
            return;
        }

        cardViews = new LevelUpCardView[cardButtons.Length];
        for (int i = 0; i < cardButtons.Length; i++)
        {
            Button button = cardButtons[i];
            cardViews[i] = new LevelUpCardView
            {
                Button = button,
                IconImage = FindNamedComponentInChildren<Image>(button != null ? button.transform : null, "Image_Icon"),
                DescriptionText = FindNamedComponentInChildren<TMP_Text>(button != null ? button.transform : null, "Text_Description")
            };
        }
    }

    private void OnPlayerLevelChanged(int newLevel)
    {
        if (newLevel <= observedLevel)
        {
            observedLevel = Mathf.Max(observedLevel, newLevel);
            return;
        }

        for (int level = observedLevel + 1; level <= newLevel; level++)
        {
            pendingLevelUps.Enqueue(level);
        }

        observedLevel = newLevel;
        TryOpenNextPanel();
    }

    private void TryOpenNextPanel()
    {
        if (isPanelOpen || pendingLevelUps.Count == 0)
        {
            return;
        }

        activeLevel = pendingLevelUps.Dequeue();
        OpenPanel();
    }

    private void OpenPanel()
    {
        activeCards.Clear();
        activeCards.AddRange(DrawCards(3));

        if (activeCards.Count == 0)
        {
            Debug.LogWarning($"No level-up card candidates are available for level {activeLevel}.");
            TryOpenNextPanel();
            return;
        }

        isPanelOpen = true;
        PauseGame();
        FillCardViews();
        ShowPanelVisuals();
    }

    private void SelectCard(int cardIndex)
    {
        if (!isPanelOpen || cardIndex < 0 || cardIndex >= activeCards.Count)
        {
            return;
        }

        ApplySelectedCard(activeCards[cardIndex]);
        ClosePanel();
        TryOpenNextPanel();
    }

    private void ApplySelectedCard(LevelUpCardRow card)
    {
        selectedCardIds.Add(card.ID);
        float value = card.Value ?? 0f;

        switch (card.Effect)
        {
            case LevelUpCardEffect.DamageAdd:
                autoShooter?.AddDamage(value);
                break;
            case LevelUpCardEffect.DamagePercent:
                autoShooter?.AddDamagePercent(value);
                break;
            case LevelUpCardEffect.FireIntervalAdd:
                autoShooter?.AddFireInterval(-value);
                break;
            case LevelUpCardEffect.FireIntervalPercent:
                autoShooter?.ReduceFireIntervalPercent(value);
                break;
            case LevelUpCardEffect.ProjectileSpeedAdd:
                autoShooter?.AddProjectileSpeed(value);
                break;
            case LevelUpCardEffect.ProjectileSpeedPercent:
                autoShooter?.AddProjectileSpeedPercent(value);
                break;
            case LevelUpCardEffect.ProjectileLifetimeAdd:
                autoShooter?.AddProjectileLifetime(value);
                break;
            case LevelUpCardEffect.ProjectileScaleAdd:
                autoShooter?.AddProjectileScale(value);
                break;
            case LevelUpCardEffect.MoveSpeedAdd:
                playerMovement?.AddMoveSpeed(value);
                break;
            case LevelUpCardEffect.MoveSpeedPercent:
                playerMovement?.AddMoveSpeedPercent(value);
                break;
            case LevelUpCardEffect.MaxHpAdd:
                playerHealth?.IncreaseMaxHP(value);
                break;
            case LevelUpCardEffect.Heal:
                playerHealth?.Heal(value);
                break;
            case LevelUpCardEffect.None:
                break;
            default:
                Debug.LogWarning($"Unhandled level-up card effect: {card.Effect}");
                break;
        }

        Debug.Log($"Level {activeLevel} selected card {card.ID}: {card.Effect} {card.Value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}");
    }

    private List<LevelUpCardRow> DrawCards(int count)
    {
        List<LevelUpCardRow> pool = BuildCandidateCards();
        List<LevelUpCardRow> result = new();

        while (result.Count < count && pool.Count > 0)
        {
            LevelUpCardRow selected = DrawWeightedCard(pool);
            if (selected == null)
            {
                break;
            }

            result.Add(selected);
            pool.Remove(selected);
        }

        return result;
    }

    private List<LevelUpCardRow> BuildCandidateCards()
    {
        List<LevelUpCardRow> candidates = new();
        for (int i = 0; i < cardTable.Count; i++)
        {
            LevelUpCardRow row = cardTable[i];
            if (row.Ratio <= 0f || selectedCardIds.Contains(row.ID))
            {
                continue;
            }

            if (row.Required.HasValue && !selectedCardIds.Contains(row.Required.Value))
            {
                continue;
            }

            candidates.Add(row);
        }

        return candidates;
    }

    private static LevelUpCardRow DrawWeightedCard(List<LevelUpCardRow> pool)
    {
        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            totalWeight += Mathf.Max(0f, pool[i].Ratio);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= Mathf.Max(0f, pool[i].Ratio);
            if (roll <= 0f)
            {
                return pool[i];
            }
        }

        return pool[pool.Count - 1];
    }

    private void FillCardViews()
    {
        if (cardViews == null)
        {
            BuildCardViews();
        }

        for (int i = 0; i < cardViews.Length; i++)
        {
            LevelUpCardView view = cardViews[i];
            bool hasCard = i < activeCards.Count;
            if (view?.Button != null)
            {
                view.Button.gameObject.SetActive(hasCard);
                view.Button.interactable = hasCard;
            }

            if (!hasCard)
            {
                continue;
            }

            LevelUpCardRow card = activeCards[i];
            if (view.IconImage != null)
            {
                view.IconImage.sprite = LoadIcon(card.Icon);
                view.IconImage.enabled = view.IconImage.sprite != null;
            }

            if (view.DescriptionText != null)
            {
                view.DescriptionText.text = card.Desc;
            }
        }
    }

    private Sprite LoadIcon(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        if (iconCache.TryGetValue(iconName, out Sprite cachedIcon))
        {
            return cachedIcon;
        }

        Sprite icon = Resources.Load<Sprite>($"{iconResourceFolder}/{iconName}");
        if (icon == null)
        {
            icon = Resources.Load<Sprite>(iconName);
        }

        if (icon == null)
        {
            Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < loadedSprites.Length; i++)
            {
                if (loadedSprites[i].name == iconName)
                {
                    icon = loadedSprites[i];
                    break;
                }
            }
        }

        if (icon == null)
        {
            Debug.LogWarning($"Level-up card icon '{iconName}' was not found. Put it under a Resources folder or reference it in the scene.");
        }

        iconCache[iconName] = icon;
        return icon;
    }

    private void ClosePanel()
    {
        HidePanelVisuals();
        activeCards.Clear();
        isPanelOpen = false;

        if (pendingLevelUps.Count == 0)
        {
            ResumeGame();
        }
    }

    private void ShowPanelVisuals()
    {
        if (cardContainer != null)
        {
            cardContainer.transform.SetAsLastSibling();
        }

        if (cardContainerCanvasGroup == null)
        {
            return;
        }

        cardContainerCanvasGroup.alpha = 1f;
        cardContainerCanvasGroup.interactable = true;
        cardContainerCanvasGroup.blocksRaycasts = true;
    }

    private void HidePanelVisuals()
    {
        if (cardContainerCanvasGroup == null)
        {
            return;
        }

        cardContainerCanvasGroup.alpha = 0f;
        cardContainerCanvasGroup.interactable = false;
        cardContainerCanvasGroup.blocksRaycasts = false;
    }

    private void PauseGame()
    {
        GamePauseState.SetLevelUpSelectionOpen(true);

        if (!pauseWithTimeScale)
        {
            return;
        }

        if (hasPausedTimeScale)
        {
            return;
        }

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        hasPausedTimeScale = true;
    }

    private void ResumeGame()
    {
        if (pauseWithTimeScale && hasPausedTimeScale)
        {
            Time.timeScale = timeScaleBeforePause;
            hasPausedTimeScale = false;
        }

        GamePauseState.SetLevelUpSelectionOpen(false);
        activeLevel = 0;
    }

    private void LoadLevelUpCardTable()
    {
        cardTable.Clear();

        TextAsset csv = Resources.Load<TextAsset>(levelUpCardCsvResourcePath);
        if (csv == null)
        {
            Debug.LogError($"LevelUpSelectionManager could not load CSV at Resources/{levelUpCardCsvResourcePath}.csv");
            return;
        }

        HashSet<int> ids = new();
        foreach (LevelUpCardRow row in ParseCsv(csv.text))
        {
            if (row.ID <= 0 || row.Ratio <= 0f || !ids.Add(row.ID))
            {
                continue;
            }

            cardTable.Add(row);
        }
    }

    private static List<LevelUpCardRow> ParseCsv(string csvText)
    {
        List<LevelUpCardRow> rows = new();
        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            return rows;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> columns = SplitCsvLine(lines[i]);
            if (columns.Count < 7)
            {
                continue;
            }

            if (!int.TryParse(columns[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                || !float.TryParse(columns[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float ratio)
                || !Enum.TryParse(columns[5], true, out LevelUpCardEffect effect))
            {
                continue;
            }

            int? required = null;
            if (!string.IsNullOrWhiteSpace(columns[2])
                && int.TryParse(columns[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int requiredId))
            {
                required = requiredId;
            }

            float? value = null;
            if (!string.IsNullOrWhiteSpace(columns[6])
                && float.TryParse(columns[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
            {
                value = parsedValue;
            }

            rows.Add(new LevelUpCardRow
            {
                ID = id,
                Ratio = ratio,
                Required = required,
                Icon = columns[3].Trim(),
                Desc = columns[4],
                Effect = effect,
                Value = value
            });
        }

        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        List<string> columns = new();
        bool inQuotes = false;
        string current = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                columns.Add(current);
                current = string.Empty;
                continue;
            }

            current += character;
        }

        columns.Add(current);
        return columns;
    }

    private static T FindNamedComponentInChildren<T>(Transform root, string objectName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].gameObject.name == objectName)
            {
                return components[i];
            }
        }

        return null;
    }
}
