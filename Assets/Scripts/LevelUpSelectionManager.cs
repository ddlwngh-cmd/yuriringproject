using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelUpSelectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerExperiences playerExperiences;
    [SerializeField] private GameObject cardContainer;
    [SerializeField] private Button[] cardButtons;

    [Header("Pause")]
    [SerializeField] private bool pauseWithTimeScale = true;

    private readonly Queue<int> pendingLevelUps = new();
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
        observedLevel = playerExperiences != null ? playerExperiences.CurrentLevel : 1;
        ConfigureCardButtons();
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

        if (playerExperiences == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerExperiences = player.GetComponent<PlayerExperiences>();
            }
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
        isPanelOpen = true;
        PauseGame();
        ShowPanelVisuals();
    }

    private void SelectCard(int cardIndex)
    {
        if (!isPanelOpen)
        {
            return;
        }

        ApplySelectedCard(cardIndex);
        ClosePanel();
        TryOpenNextPanel();
    }

    private void ApplySelectedCard(int cardIndex)
    {
        Debug.Log($"Level {activeLevel} upgrade card {cardIndex + 1} selected. Upgrade effect is not implemented yet.");
    }

    private void ClosePanel()
    {
        HidePanelVisuals();
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
}
