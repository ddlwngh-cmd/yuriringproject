using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("Events")]
    [SerializeField] private UnityEvent onStageSuccess;
    [SerializeField] private UnityEvent onStageFailure;

    private float remainingTime;
    private bool isInitialized;
    private StageResult result = StageResult.InProgress;

    public float RemainingTime => remainingTime;
    public StageResult Result => result;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += OnPlayerDied;
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

        if (result == StageResult.InProgress && !GamePauseState.IsGameplayPaused)
        {
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            if (remainingTime <= 0f)
            {
                TriggerSuccess();
            }
        }

        UpdateTimerText();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
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
        onStageSuccess?.Invoke();
    }

    private void TriggerFailure()
    {
        if (result != StageResult.InProgress || remainingTime <= 0f)
        {
            return;
        }

        result = StageResult.Failure;
        onStageFailure?.Invoke();
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
