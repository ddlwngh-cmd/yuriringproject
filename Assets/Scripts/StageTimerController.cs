using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageTimerController : MonoBehaviour
{
    public enum StageResult
    {
        InProgress,
        Success,
        Failure
    }

    private sealed class StageRow
    {
        public int StageId;
        public float Time;
    }

    [Header("Stage")]
    [SerializeField, Min(1)] private int stageId = 1;
    [SerializeField] private string stageCsvResourcePath = "Stage";

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TMP_Text timeText;

    [Header("Result UI")]
    [SerializeField] private GameObject resultHud;
    [SerializeField] private GameObject successImage;
    [SerializeField] private GameObject failImage;
    [SerializeField] private Button homeButton;
    [SerializeField] private string homeSceneName = "HomeScene";

    [Header("Events")]
    [SerializeField] private UnityEvent onStageSuccess;
    [SerializeField] private UnityEvent onStageFailure;

    private float remainingTime;
    private bool isInitialized;
    private StageResult result = StageResult.InProgress;
    private bool isResultPauseApplied;

    public float RemainingTime => remainingTime;
    public StageResult Result => result;

    private void Awake()
    {
        GamePauseState.SetForcedPause(false);
        Time.timeScale = 1f;
        ResolveReferences();
        HideResultHud();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += OnPlayerDied;
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(LoadHomeScene);
        }
    }

    private void Start()
    {
        if (!TryLoadStageTime())
        {
            enabled = false;
            return;
        }

        isInitialized = true;
        UpdateTimerText();

        if (playerHealth != null && playerHealth.CurrentHP <= 0f)
        {
            TriggerFailure();
        }
        else if (remainingTime <= 0f)
        {
            TriggerSuccess();
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (result != StageResult.InProgress || GamePauseState.IsGameplayPaused)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateTimerText();

        if (remainingTime <= 0f)
        {
            TriggerSuccess();
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(LoadHomeScene);
        }
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (timeText == null)
        {
            GameObject timeTextObject = GameObject.Find("TXT_Time");
            if (timeTextObject != null)
            {
                timeText = timeTextObject.GetComponent<TMP_Text>();
            }
        }
    }

    private bool TryLoadStageTime()
    {
        TextAsset csv = Resources.Load<TextAsset>(stageCsvResourcePath);
        if (csv == null)
        {
            Debug.LogError($"StageTimerController could not load CSV at Resources/{stageCsvResourcePath}.csv");
            return false;
        }

        List<StageRow> rows = ParseCsv(csv.text);
        StageRow currentStage = rows.Find(row => row.StageId == stageId);
        if (currentStage == null)
        {
            Debug.LogError($"StageTimerController could not find StageId={stageId} in Resources/{stageCsvResourcePath}.csv");
            return false;
        }

        remainingTime = Mathf.Max(0f, currentStage.Time);
        return true;
    }

    private static List<StageRow> ParseCsv(string csvText)
    {
        List<StageRow> rows = new();
        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return rows;
        }

        string[] headers = lines[0].Split(',');
        int stageIdIndex = FindColumnIndex(headers, "StageId");
        int timeIndex = FindColumnIndex(headers, "Time");
        if (stageIdIndex < 0 || timeIndex < 0)
        {
            Debug.LogError("Stage.csv must contain StageId and Time columns.");
            return rows;
        }

        int requiredColumnCount = Mathf.Max(stageIdIndex, timeIndex) + 1;
        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');
            if (columns.Length < requiredColumnCount
                || !int.TryParse(columns[stageIdIndex].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedStageId)
                || !float.TryParse(columns[timeIndex].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedTime))
            {
                Debug.LogWarning($"StageTimerController skipped invalid Stage.csv row {i + 1}: {lines[i]}");
                continue;
            }

            rows.Add(new StageRow
            {
                StageId = parsedStageId,
                Time = Mathf.Max(0f, parsedTime)
            });
        }

        return rows;
    }

    private static int FindColumnIndex(string[] headers, string columnName)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            if (string.Equals(headers[i].Trim(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void OnPlayerDied()
    {
        if (isInitialized && result == StageResult.InProgress && remainingTime > 0f)
        {
            TriggerFailure();
        }
    }

    private void TriggerSuccess()
    {
        if (result != StageResult.InProgress)
        {
            return;
        }

        remainingTime = 0f;
        result = StageResult.Success;
        UpdateTimerText();
        CompleteStage(true);
        onStageSuccess?.Invoke();
    }

    private void TriggerFailure()
    {
        if (result != StageResult.InProgress || remainingTime <= 0f)
        {
            return;
        }

        result = StageResult.Failure;
        CompleteStage(false);
        onStageFailure?.Invoke();
    }

    private void CompleteStage(bool isSuccess)
    {
        GamePauseState.SetForcedPause(true);
        Time.timeScale = 0f;
        isResultPauseApplied = true;

        if (successImage != null)
        {
            successImage.SetActive(isSuccess);
        }

        if (failImage != null)
        {
            failImage.SetActive(!isSuccess);
        }

        if (homeButton != null)
        {
            homeButton.gameObject.SetActive(true);
            homeButton.interactable = true;
        }

        if (resultHud != null)
        {
            resultHud.SetActive(true);
        }
    }

    private void HideResultHud()
    {
        if (resultHud != null)
        {
            resultHud.SetActive(false);
        }
    }

    private void LoadHomeScene()
    {
        ReleaseResultPause();
        SceneManager.LoadScene(homeSceneName);
    }

    private void OnDestroy()
    {
        ReleaseResultPause();
    }

    private void ReleaseResultPause()
    {
        if (!isResultPauseApplied)
        {
            return;
        }

        isResultPauseApplied = false;
        GamePauseState.SetForcedPause(false);
        Time.timeScale = 1f;
    }

    private void UpdateTimerText()
    {
        if (timeText == null)
        {
            return;
        }

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timeText.SetText("{0:00}:{1:00}", minutes, seconds);
    }
}
