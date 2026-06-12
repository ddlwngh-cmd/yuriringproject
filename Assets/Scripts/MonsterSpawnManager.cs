using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class MonsterSpawnManager : MonoBehaviour
{
    [Serializable]
    private class StageMonsterRow
    {
        public int StageId;
        public string MonsterId;
        public float SpawnStartSec;
        public float WaveIntervalSec;
        public int WaveSizeStart;
        public int WaveSizeGrowth;
        public int WaveSizeMax;
        public int TotalBudget;
        public int MaxAliveCap;
        public float SpawnEndSec;
    }

    private class SpawnRuleState
    {
        public StageMonsterRow Rule;
        public GameObject Prefab;
        public int SpawnedTotal;
        public int AliveCount;
        public int WaveIndex;
        public float NextSpawnTime;
    }

    private class SpawnedMonsterTracker : MonoBehaviour
    {
        private Action onDestroyed;

        public void Init(Action onDestroyedCallback)
        {
            onDestroyed = onDestroyedCallback;
        }

        private void OnDestroy()
        {
            onDestroyed?.Invoke();
        }
    }

    [SerializeField] private int stageId = 1;
    [SerializeField] private string stageMonsterCsvResourcePath = "StageMonster";
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0.1f)] private float outsideViewSpawnPadding = 2f;

    private readonly List<SpawnRuleState> spawnStates = new();

    private void Start()
    {
        if (StageSelectionState.HasSelection)
        {
            stageId = StageSelectionState.SelectedStage.StageId;
        }

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (playerTarget == null || targetCamera == null)
        {
            Debug.LogError("MonsterSpawnManager requires both player target and camera.");
            enabled = false;
            return;
        }

        BuildSpawnSchedule();
    }

    private void Update()
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        float elapsed = Time.timeSinceLevelLoad;

        foreach (SpawnRuleState state in spawnStates)
        {
            if (elapsed < state.NextSpawnTime || elapsed >= state.Rule.SpawnEndSec)
            {
                continue;
            }

            if (state.SpawnedTotal >= state.Rule.TotalBudget)
            {
                continue;
            }

            int waveSize = state.Rule.WaveSizeStart + (state.WaveIndex * state.Rule.WaveSizeGrowth);
            waveSize = Mathf.Min(waveSize, state.Rule.WaveSizeMax);

            int remainingBudget = state.Rule.TotalBudget - state.SpawnedTotal;
            int remainingAliveSlots = state.Rule.MaxAliveCap - state.AliveCount;
            int spawnCount = Mathf.Min(waveSize, remainingBudget, remainingAliveSlots);

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnMonster(state);
            }

            state.WaveIndex++;
            state.NextSpawnTime += state.Rule.WaveIntervalSec;
        }
    }

    private void BuildSpawnSchedule()
    {
        TextAsset csv = Resources.Load<TextAsset>(stageMonsterCsvResourcePath);
        if (csv == null)
        {
            Debug.LogError($"MonsterSpawnManager could not load CSV at Resources/{stageMonsterCsvResourcePath}.csv");
            enabled = false;
            return;
        }

        List<StageMonsterRow> allRows = ParseCsv(csv.text);
        foreach (StageMonsterRow row in allRows)
        {
            if (row.StageId != stageId)
            {
                continue;
            }

            GameObject prefab = Resources.Load<GameObject>(row.MonsterId);
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>($"Prefabs/{row.MonsterId}");
            }

            if (prefab == null)
            {
                Debug.LogWarning($"Monster prefab not found for MonsterId={row.MonsterId}. Expected Resources/{row.MonsterId} or Resources/Prefabs/{row.MonsterId}.");
                continue;
            }

            spawnStates.Add(new SpawnRuleState
            {
                Rule = row,
                Prefab = prefab,
                SpawnedTotal = 0,
                AliveCount = 0,
                WaveIndex = 0,
                NextSpawnTime = row.SpawnStartSec
            });
        }

        if (spawnStates.Count == 0)
        {
            Debug.LogWarning($"MonsterSpawnManager found no valid spawn rows for StageId={stageId}.");
        }
    }

    private void SpawnMonster(SpawnRuleState state)
    {
        Vector3 spawnPos = GetSpawnPositionOutsideCamera();
        GameObject monster = Instantiate(state.Prefab, spawnPos, Quaternion.identity);

        MonsterController monsterController = monster.GetComponent<MonsterController>();
        if (monsterController != null)
        {
            monsterController.SetTarget(playerTarget);
        }

        SpawnedMonsterTracker tracker = monster.AddComponent<SpawnedMonsterTracker>();
        state.AliveCount++;
        state.SpawnedTotal++;
        tracker.Init(() => { state.AliveCount = Mathf.Max(0, state.AliveCount - 1); });
    }

    private Vector3 GetSpawnPositionOutsideCamera()
    {
        Vector3 center = playerTarget.position;
        float halfHeight = targetCamera.orthographic ? targetCamera.orthographicSize : 5f;
        float halfWidth = halfHeight * targetCamera.aspect;
        float minRadius = Mathf.Sqrt((halfWidth * halfWidth) + (halfHeight * halfHeight)) + outsideViewSpawnPadding;

        Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
        if (randomDir.sqrMagnitude < 0.0001f)
        {
            randomDir = Vector2.right;
        }

        Vector2 pos2D = (Vector2)center + (randomDir * minRadius);
        return new Vector3(pos2D.x, pos2D.y, 0f);
    }

    private static List<StageMonsterRow> ParseCsv(string csvText)
    {
        List<StageMonsterRow> rows = new();
        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            return rows;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] c = lines[i].Split(',');
            if (c.Length < 9)
            {
                continue;
            }

            float spawnEndSec = float.PositiveInfinity;
            if (c.Length >= 10 && !string.IsNullOrWhiteSpace(c[9]))
            {
                spawnEndSec = ParseFloat(c[9]);
            }

            StageMonsterRow row = new()
            {
                StageId = int.Parse(c[0], NumberStyles.Integer, CultureInfo.InvariantCulture),
                MonsterId = c[1],
                SpawnStartSec = ParseFloat(c[2]),
                WaveIntervalSec = Mathf.Max(0.01f, ParseFloat(c[3])),
                WaveSizeStart = Mathf.Max(0, int.Parse(c[4], NumberStyles.Integer, CultureInfo.InvariantCulture)),
                WaveSizeGrowth = Mathf.Max(0, int.Parse(c[5], NumberStyles.Integer, CultureInfo.InvariantCulture)),
                WaveSizeMax = Mathf.Max(0, int.Parse(c[6], NumberStyles.Integer, CultureInfo.InvariantCulture)),
                TotalBudget = Mathf.Max(0, int.Parse(c[7], NumberStyles.Integer, CultureInfo.InvariantCulture)),
                MaxAliveCap = Mathf.Max(0, int.Parse(c[8], NumberStyles.Integer, CultureInfo.InvariantCulture)),
                SpawnEndSec = spawnEndSec
            };
            rows.Add(row);
        }

        return rows;
    }

    private static float ParseFloat(string value)
    {
        return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
