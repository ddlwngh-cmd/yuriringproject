using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerExperiences : MonoBehaviour
{
    private class LevelXpRow
    {
        public int Level;
        public float NeedXp;
    }

    public static PlayerExperiences Instance { get; private set; }

    [Header("Experience")]
    [SerializeField, Min(1)] private int currentLevel = 1;
    [SerializeField, Min(0f)] private float currentExp;
    [SerializeField] private string levelXpCsvResourcePath = "LevelXp";

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField, Min(0f)] private float levelUpFullGaugeHoldSeconds = 0.3f;

    public event Action<int> LevelChanged;
    public event Action<float, bool> ExpRatioChanged;

    private readonly Dictionary<int, float> needXpByLevel = new();
    private float queuedExp;
    private Coroutine addExpRoutine;

    public int CurrentLevel => currentLevel;
    public float CurrentExp => currentExp;
    public float NeedXp => GetNeedXp(currentLevel);
    public float ExpRatio => NeedXp <= 0f ? 0f : Mathf.Clamp01(currentExp / NeedXp);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentLevel = Mathf.Max(1, currentLevel);
        currentExp = Mathf.Max(0f, currentExp);
        LoadLevelXpTable();
        FindLevelTextIfNeeded();
        UpdateLevelText();
    }

    private void Start()
    {
        NotifyExpRatio(false);
        LevelChanged?.Invoke(currentLevel);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddExp(float amount)
    {
        if (GamePauseState.IsGameplayPaused || amount <= 0f)
        {
            return;
        }

        queuedExp += amount;
        if (addExpRoutine == null)
        {
            addExpRoutine = StartCoroutine(ProcessQueuedExp());
        }
    }

    private IEnumerator ProcessQueuedExp()
    {
        while (queuedExp > 0f)
        {
            float amount = queuedExp;
            queuedExp = 0f;
            yield return ApplyExpAmount(amount);
        }

        addExpRoutine = null;
    }

    private IEnumerator ApplyExpAmount(float amount)
    {
        while (amount > 0f)
        {
            float needXp = GetNeedXp(currentLevel);
            if (needXp <= 0f)
            {
                currentExp = 0f;
                NotifyExpRatio(false);
                yield break;
            }

            float expToLevelUp = Mathf.Max(0f, needXp - currentExp);
            if (amount < expToLevelUp)
            {
                currentExp += amount;
                amount = 0f;
                NotifyExpRatio(true);
                yield break;
            }

            currentExp = needXp;
            amount -= expToLevelUp;
            NotifyExpRatio(true);

            if (levelUpFullGaugeHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(levelUpFullGaugeHoldSeconds);
            }

            if (GamePauseState.IsForcedPause)
            {
                queuedExp = 0f;
                yield break;
            }

            currentLevel++;
            currentExp = 0f;
            UpdateLevelText();
            LevelChanged?.Invoke(currentLevel);
            NotifyExpRatio(false);

            if (amount > 0f)
            {
                float nextNeedXp = GetNeedXp(currentLevel);
                if (amount < nextNeedXp)
                {
                    currentExp = amount;
                    amount = 0f;
                    NotifyExpRatio(false);
                }
            }
        }
    }

    private void NotifyExpRatio(bool animate)
    {
        ExpRatioChanged?.Invoke(ExpRatio, animate);
    }

    private void LoadLevelXpTable()
    {
        needXpByLevel.Clear();

        TextAsset csv = Resources.Load<TextAsset>(levelXpCsvResourcePath);
        if (csv == null)
        {
            Debug.LogError($"PlayerExperiences could not load CSV at Resources/{levelXpCsvResourcePath}.csv");
            return;
        }

        foreach (LevelXpRow row in ParseCsv(csv.text))
        {
            if (row.Level <= 0 || row.NeedXp <= 0f)
            {
                continue;
            }

            needXpByLevel[row.Level] = row.NeedXp;
        }
    }

    private float GetNeedXp(int level)
    {
        if (needXpByLevel.TryGetValue(level, out float needXp))
        {
            return needXp;
        }

        int closestLevel = 0;
        float closestNeedXp = 0f;
        foreach (KeyValuePair<int, float> pair in needXpByLevel)
        {
            if (pair.Key > closestLevel)
            {
                closestLevel = pair.Key;
                closestNeedXp = pair.Value;
            }
        }

        return closestNeedXp;
    }

    private static List<LevelXpRow> ParseCsv(string csvText)
    {
        List<LevelXpRow> rows = new();
        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            return rows;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');
            if (columns.Length < 2)
            {
                continue;
            }

            if (!int.TryParse(columns[0], out int level) || !float.TryParse(columns[1], out float needXp))
            {
                continue;
            }

            rows.Add(new LevelXpRow
            {
                Level = level,
                NeedXp = needXp
            });
        }

        return rows;
    }

    private void FindLevelTextIfNeeded()
    {
        if (levelText != null)
        {
            return;
        }

        GameObject levelTextObject = GameObject.Find("TXT_Level");
        if (levelTextObject != null)
        {
            levelText = levelTextObject.GetComponent<TMP_Text>();
            return;
        }

        GameObject hudCanvas = GameObject.Find("HUD_Canvas");
        if (hudCanvas == null)
        {
            return;
        }

        GameObject createdLevelText = new("TXT_Level", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        createdLevelText.transform.SetParent(hudCanvas.transform, false);

        RectTransform rectTransform = createdLevelText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(40f, -40f);
        rectTransform.sizeDelta = new Vector2(300f, 60f);

        TextMeshProUGUI text = createdLevelText.GetComponent<TextMeshProUGUI>();
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        levelText = text;
    }

    private void UpdateLevelText()
    {
        if (levelText == null)
        {
            return;
        }

        levelText.text = $"Level : {currentLevel}";
    }
}
