using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeStageSelectionController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject gameStartPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;

    [Header("Stage List")]
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject stageButtonPrefab;
    [SerializeField] private string stageCsvResourcePath = "Stage";
    [SerializeField] private string stageSpriteResourceFolder = "Sprites";
    [SerializeField, Min(0f)] private float stageButtonSpacing = 20f;

    [Header("Coin")]
    [SerializeField] private TMP_Text coinAmountText;

    [Header("Upgrade")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button upgradeCloseButton;
    [SerializeField] private RectTransform upgradeContent;
    [SerializeField] private GameObject upgradeCellPrefab;
    [SerializeField] private string upgradeCsvResourcePath = "UpgradeStatus";
    [SerializeField, Min(0f)] private float upgradeCellSpacing = 10f;

    [Header("Application")]
    [SerializeField] private Button exitButton;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    private readonly List<UpgradeCellView> upgradeCells = new List<UpgradeCellView>();

    private void Awake()
    {
        ResolveCoinAmountText();
        ResolveUpgradeReferences();
        RefreshTotalCoin();

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        BuildStageButtons();
        BuildUpgradeCells();
        gameStartPanel.SetActive(false);

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OpenGameStartPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGameStartPanel);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OpenUpgradePanel);
        }

        if (upgradeCloseButton != null)
        {
            upgradeCloseButton.onClick.AddListener(CloseUpgradePanel);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitApplication);
        }

        UpgradeProgressStorage.ProgressChanged += RefreshUpgradeUI;
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OpenGameStartPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseGameStartPanel);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OpenUpgradePanel);
        }

        if (upgradeCloseButton != null)
        {
            upgradeCloseButton.onClick.RemoveListener(CloseUpgradePanel);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitApplication);
        }

        UpgradeProgressStorage.ProgressChanged -= RefreshUpgradeUI;
    }

    public void ResetTotalCoin()
    {
        CoinStorage.ResetCoin();
        RefreshTotalCoin();
    }

    public void AddCheatCoin()
    {
        CoinStorage.AddCoin(100);
        RefreshTotalCoin();
    }

    public void RefreshTotalCoin()
    {
        int totalCoin = CoinStorage.LoadTotalCoin();
        if (coinAmountText != null)
        {
            coinAmountText.SetText("{0}", totalCoin);
        }

        for (int i = 0; i < upgradeCells.Count; i++)
        {
            upgradeCells[i].Refresh(totalCoin);
        }
    }

    public void ResetAllUpgradeLevels()
    {
        UpgradeProgressStorage.ResetAllLevels();
        RefreshUpgradeUI();
    }

    private void ResolveCoinAmountText()
    {
        if (coinAmountText != null)
        {
            return;
        }

        GameObject coinAmountObject = GameObject.Find("CoinAmount");
        if (coinAmountObject != null)
        {
            coinAmountText = coinAmountObject.GetComponent<TMP_Text>();
        }
    }

    private bool ValidateReferences()
    {
        if (gameStartPanel == null || startButton == null || closeButton == null || exitButton == null || content == null || stageButtonPrefab == null)
        {
            Debug.LogError("HomeStageSelectionController requires the panel, buttons, BTN_Exit, content, and BTN_Stage prefab references.");
            return false;
        }

        return true;
    }

    private void BuildStageButtons()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        List<StageData> stages = StageDataRepository.Load(stageCsvResourcePath);
        RectTransform prefabRect = stageButtonPrefab.GetComponent<RectTransform>();
        float buttonHeight = prefabRect != null ? prefabRect.rect.height : 100f;

        for (int i = 0; i < stages.Count; i++)
        {
            StageData stage = stages[i];
            GameObject buttonObject = Instantiate(stageButtonPrefab, content);
            buttonObject.name = $"BTN_Stage_{stage.StageId}";

            ConfigureButtonVisuals(buttonObject, stage);
            PositionButton(buttonObject.GetComponent<RectTransform>(), i, buttonHeight);

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => StartStage(stage));
            }
            else
            {
                Debug.LogError($"{buttonObject.name} does not contain a Button component.");
            }
        }

        float contentHeight = stages.Count > 0
            ? stages.Count * buttonHeight + (stages.Count - 1) * stageButtonSpacing
            : 0f;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
    }

    private void ConfigureButtonVisuals(GameObject buttonObject, StageData stage)
    {
        TMP_Text stageNameText = buttonObject.transform.Find("Text (TMP)")?.GetComponent<TMP_Text>();
        if (stageNameText != null)
        {
            stageNameText.text = stage.Name;
        }

        Image stageImage = buttonObject.transform.Find("StageImage")?.GetComponent<Image>();
        if (stageImage == null)
        {
            return;
        }

        string spritePath = string.IsNullOrWhiteSpace(stageSpriteResourceFolder)
            ? stage.Image
            : $"{stageSpriteResourceFolder.TrimEnd('/')}/{stage.Image}";
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"Could not load stage image at Resources/{spritePath} for StageId={stage.StageId}.");
            return;
        }

        stageImage.sprite = sprite;
        stageImage.preserveAspect = true;
    }

    private void PositionButton(RectTransform buttonRect, int index, float buttonHeight)
    {
        if (buttonRect == null)
        {
            return;
        }

        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.anchoredPosition = new Vector2(0f, -index * (buttonHeight + stageButtonSpacing));
    }


    private void ResolveUpgradeReferences()
    {
        Transform root = transform;
        upgradePanel ??= root.Find("Panel_Upgrade")?.gameObject;
        upgradeButton ??= root.Find("BTNS/BTN_Upgrade")?.GetComponent<Button>();
        upgradeCloseButton ??= root.Find("Panel_Upgrade/Popup/BTN_X")?.GetComponent<Button>();
        upgradeContent ??= root.Find("Panel_Upgrade/Popup/Scroll View/Viewport/Content") as RectTransform;

        if (upgradeCellPrefab == null && upgradeContent != null && upgradeContent.childCount > 0)
        {
            upgradeCellPrefab = upgradeContent.GetChild(0).gameObject;
        }
    }

    private void BuildUpgradeCells()
    {
        if (upgradeContent == null || upgradeCellPrefab == null)
        {
            Debug.LogError("Upgrade UI requires Panel_Upgrade content and an UpgradeCell template.", this);
            return;
        }

        IReadOnlyDictionary<UpgradeStat, List<UpgradeStatusData>> table = UpgradeStatusRepository.Load(upgradeCsvResourcePath);
        Sprite dotOff = null;
        Sprite dotOn = null;
        Sprite[] dotSprites = Resources.LoadAll<Sprite>("Sprites/dot");
        for (int i = 0; i < dotSprites.Length; i++)
        {
            if (dotSprites[i].name == "dot_0") dotOff = dotSprites[i];
            if (dotSprites[i].name == "dot_1") dotOn = dotSprites[i];
        }

        upgradeCellPrefab.SetActive(false);
        upgradeCells.Clear();
        float cellHeight = upgradeCellPrefab.GetComponent<RectTransform>()?.rect.height ?? 80f;
        int cellIndex = 0;

        foreach (UpgradeStat stat in System.Enum.GetValues(typeof(UpgradeStat)))
        {
            if (!table.TryGetValue(stat, out List<UpgradeStatusData> levels) || levels.Count == 0)
            {
                continue;
            }

            GameObject cellObject = Instantiate(upgradeCellPrefab, upgradeContent);
            cellObject.name = $"UpgradeCell_{stat}";
            cellObject.SetActive(true);
            PositionUpgradeCell(cellObject.GetComponent<RectTransform>(), cellIndex, cellHeight);

            UpgradeCellView cell = cellObject.AddComponent<UpgradeCellView>();
            cell.Bind(stat, levels, dotOff, dotOn, TryPurchaseUpgrade);
            upgradeCells.Add(cell);
            cellIndex++;
        }

        float contentHeight = cellIndex > 0
            ? cellIndex * cellHeight + (cellIndex - 1) * upgradeCellSpacing
            : 0f;
        upgradeContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
    }

    private void PositionUpgradeCell(RectTransform cellRect, int index, float cellHeight)
    {
        if (cellRect == null)
        {
            return;
        }

        cellRect.anchorMin = new Vector2(0f, 1f);
        cellRect.anchorMax = new Vector2(0f, 1f);
        cellRect.pivot = new Vector2(0f, 1f);
        cellRect.anchoredPosition = new Vector2(0f, -index * (cellHeight + upgradeCellSpacing));
    }

    private void TryPurchaseUpgrade(UpgradeStat stat)
    {
        IReadOnlyDictionary<UpgradeStat, List<UpgradeStatusData>> table = UpgradeStatusRepository.Load(upgradeCsvResourcePath);
        if (!table.TryGetValue(stat, out List<UpgradeStatusData> levels))
        {
            return;
        }

        int nextLevel = UpgradeProgressStorage.LoadLevel(stat) + 1;
        UpgradeStatusData next = levels.Find(data => data.Level == nextLevel);
        if (next == null || !CoinStorage.TrySpendCoin(next.CoinValue))
        {
            RefreshUpgradeUI();
            return;
        }

        UpgradeProgressStorage.SaveLevel(stat, nextLevel);
        RefreshUpgradeUI();
    }

    private void RefreshUpgradeUI()
    {
        RefreshTotalCoin();
    }

    private void OpenUpgradePanel()
    {
        RefreshUpgradeUI();
        upgradePanel?.SetActive(true);
    }

    private void CloseUpgradePanel()
    {
        upgradePanel?.SetActive(false);
    }

    private void OpenGameStartPanel()
    {
        gameStartPanel.SetActive(true);
    }

    private void CloseGameStartPanel()
    {
        gameStartPanel.SetActive(false);
    }

    private void ExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartStage(StageData stage)
    {
        StageSelectionState.Select(stage);
        SceneManager.LoadScene(gameSceneName);
    }
}
