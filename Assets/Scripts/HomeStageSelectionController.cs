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

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        BuildStageButtons();
        gameStartPanel.SetActive(false);
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
    }

    private bool ValidateReferences()
    {
        if (gameStartPanel == null || startButton == null || closeButton == null || content == null || stageButtonPrefab == null)
        {
            Debug.LogError("HomeStageSelectionController requires the panel, buttons, content, and BTN_Stage prefab references.");
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

    private void OpenGameStartPanel()
    {
        gameStartPanel.SetActive(true);
    }

    private void CloseGameStartPanel()
    {
        gameStartPanel.SetActive(false);
    }

    private void StartStage(StageData stage)
    {
        StageSelectionState.Select(stage);
        SceneManager.LoadScene(gameSceneName);
    }
}
