#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class MonsterSpawnBalancingWindow : EditorWindow
{
    [Serializable]
    private sealed class MonsterEntry
    {
        public GameObject Prefab;
        public float SpawnStartSec;
        public float Weight = 1f;
    }

    private const string StageCsvPath = "Assets/Resources/Stage.csv";
    private const string StageMonsterCsvPath = "Assets/Resources/StageMonster.csv";

    [SerializeField] private int stageId = 1;
    [SerializeField] private float stageDurationSec = 180f;
    [SerializeField, Range(0.01f, 0.99f)] private float targetClearProbability = 0.7f;
    [SerializeField] private AnimationCurve difficultyCurve = new(
        new Keyframe(0f, 0.55f),
        new Keyframe(0.35f, 0.8f),
        new Keyframe(0.7f, 1.15f),
        new Keyframe(1f, 1.65f));
    [SerializeField] private float playerAttack = 10f;
    [SerializeField] private float playerAttackInterval = 0.35f;
    [SerializeField, Range(0f, 1f)] private float criticalChance = 0.05f;
    [SerializeField] private float criticalDamageMultiplier = 2f;
    [SerializeField, Range(0.05f, 1f)] private float attackEfficiency = 0.8f;
    [SerializeField] private float playerMaxHp = 100f;
    [SerializeField] private float waveIntervalSec = 2f;
    [SerializeField, Range(4, 60)] private int segmentCount = MonsterSpawnBalanceCalculator.DefaultSegmentCount;
    [SerializeField] private List<MonsterEntry> monsters = new();

    private Vector2 scrollPosition;
    private MonsterSpawnBalanceCalculator.Result preview;
    private string validationMessage;

    [MenuItem("Tools/밸런싱/몬스터 스폰 밸런서")]
    public static void Open()
    {
        MonsterSpawnBalancingWindow window = GetWindow<MonsterSpawnBalancingWindow>("몬스터 스폰 밸런서");
        window.minSize = new Vector2(680f, 720f);
    }

    private void OnEnable()
    {
        if (monsters.Count == 0)
        {
            monsters.Add(new MonsterEntry());
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawHeader();
        DrawStageSettings();
        DrawPlayerSettings();
        DrawMonsterSettings();
        DrawPreview();
        DrawApplyArea();
        EditorGUILayout.EndScrollView();
    }

    private static void DrawHeader()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("몬스터 스폰 밸런싱 및 데이터 적용", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "플레이어의 기대 DPS, 몬스터 체력/공격력, 목표 클리어 확률과 난이도 곡선을 이용해 " +
            "시간 구간별 StageMonster 스폰 규칙을 생성합니다.", MessageType.Info);
    }

    private void DrawStageSettings()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("1. 스테이지 목표", EditorStyles.boldLabel);
        stageId = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Stage ID", "Stage.StageId / StageMonster.StageId"), stageId));
        stageDurationSec = Mathf.Max(1f, EditorGUILayout.FloatField(new GUIContent("버티기 시간 (초)", "Stage.Time"), stageDurationSec));
        targetClearProbability = EditorGUILayout.Slider(new GUIContent("목표 클리어 성공 확률", "낮을수록 플레이어 처리량을 초과하는 스폰 압박을 생성합니다."), targetClearProbability, 0.01f, 0.99f);
        difficultyCurve = EditorGUILayout.CurveField(
            new GUIContent("난이도 증가 곡선", "X: 스테이지 진행률, Y: 기준 스폰 압박 배율"),
            difficultyCurve,
            new Color(0.95f, 0.35f, 0.2f),
            new Rect(0f, 0f, 1f, 2.5f),
            GUILayout.Height(80f));
        segmentCount = EditorGUILayout.IntSlider(new GUIContent("곡선 샘플 구간", "구간이 많을수록 곡선을 정밀하게 반영하지만 CSV 행이 늘어납니다."), segmentCount, 4, 60);
        waveIntervalSec = Mathf.Max(0.25f, EditorGUILayout.FloatField("웨이브 간격 (초)", waveIntervalSec));
    }

    private void DrawPlayerSettings()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("2. 일반 플레이어 전투 스탯", EditorStyles.boldLabel);
        playerAttack = Mathf.Max(0.01f, EditorGUILayout.FloatField("일반 공격력", playerAttack));
        playerAttackInterval = Mathf.Max(0.05f, EditorGUILayout.FloatField("공격 간격 (초)", playerAttackInterval));
        criticalChance = EditorGUILayout.Slider("치명타 확률", criticalChance, 0f, 1f);
        criticalDamageMultiplier = Mathf.Max(1f, EditorGUILayout.FloatField("치명타 피해 배율", criticalDamageMultiplier));
        attackEfficiency = EditorGUILayout.Slider(new GUIContent("실전 공격 효율", "이동, 빗나감, 타깃 전환 손실을 반영한 DPS 가동률"), attackEfficiency, 0.05f, 1f);
        playerMaxHp = Mathf.Max(1f, EditorGUILayout.FloatField("일반 최대 HP", playerMaxHp));
    }

    private void DrawMonsterSettings()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("3. 등장 몬스터 및 순서", EditorStyles.boldLabel);
        if (GUILayout.Button("등장 시각 자동 배치", GUILayout.Width(130f)))
        {
            AutoArrangeSpawnStarts();
        }
        if (GUILayout.Button("몬스터 추가", GUILayout.Width(95f)))
        {
            monsters.Add(new MonsterEntry());
            preview = null;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("목록 순서가 등장 순서입니다. 프리팹은 Assets/Resources 안에 있어야 하며 MonsterController의 HP/공격력을 자동으로 읽습니다.", MessageType.None);

        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterEntry entry = monsters[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(28f));
            entry.Prefab = (GameObject)EditorGUILayout.ObjectField(entry.Prefab, typeof(GameObject), false);
            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(28f)))
            {
                MonsterEntry previous = monsters[i - 1];
                monsters[i - 1] = monsters[i];
                monsters[i] = previous;
                preview = null;
            }
            GUI.enabled = i < monsters.Count - 1;
            if (GUILayout.Button("▼", GUILayout.Width(28f)))
            {
                MonsterEntry next = monsters[i + 1];
                monsters[i + 1] = monsters[i];
                monsters[i] = next;
                preview = null;
            }
            GUI.enabled = monsters.Count > 1;
            if (GUILayout.Button("삭제", GUILayout.Width(44f)))
            {
                monsters.RemoveAt(i);
                preview = null;
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            entry.SpawnStartSec = Mathf.Clamp(EditorGUILayout.FloatField("최초 등장 시각 (초)", entry.SpawnStartSec), 0f, stageDurationSec);
            entry.Weight = Mathf.Max(0.01f, EditorGUILayout.FloatField(new GUIContent("등장 비중", "같은 시점에 활성화된 몬스터 사이의 HP 예산 비중"), entry.Weight));
            DrawMonsterStats(entry.Prefab);
            EditorGUILayout.EndVertical();
        }
    }

    private static void DrawMonsterStats(GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        if (!TryReadMonsterStats(prefab, out float hp, out float attack, out string error))
        {
            EditorGUILayout.HelpBox(error, MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"감지된 스탯: HP {hp:0.##} / 공격력 {attack:0.##}", EditorStyles.miniLabel);
    }

    private void DrawPreview()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("4. 밸런싱 미리보기", EditorStyles.boldLabel);
        if (GUILayout.Button("밸런싱 계산", GUILayout.Height(30f)))
        {
            preview = BuildPreview(out validationMessage);
        }

        if (!string.IsNullOrEmpty(validationMessage))
        {
            EditorGUILayout.HelpBox(validationMessage, preview == null ? MessageType.Error : MessageType.Info);
        }

        if (preview == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"예상 플레이어 DPS: {preview.PlayerDps:0.##}");
        EditorGUILayout.LabelField($"가중 평균 몬스터 HP: {preview.AverageMonsterHp:0.##}");
        EditorGUILayout.LabelField($"최대 스폰 속도: {preview.PeakSpawnPerSecond:0.##} 마리/초");
        EditorGUILayout.LabelField($"총 스폰 예산: {preview.TotalSpawnBudget:0} 마리");
        EditorGUILayout.LabelField($"생성될 StageMonster 행: {preview.Rows.Count}개");
        EditorGUILayout.EndVertical();

        int previewCount = Mathf.Min(12, preview.Rows.Count);
        for (int i = 0; i < previewCount; i++)
        {
            MonsterSpawnBalanceCalculator.SpawnRow row = preview.Rows[i];
            EditorGUILayout.LabelField(
                $"{row.SpawnStartSec,6:0.##}~{row.SpawnEndSec,6:0.##}s  {row.MonsterId}  " +
                $"{row.WaveIntervalSec:0.##}초마다 {row.WaveSizeStart}마리 (예산 {row.TotalBudget})",
                EditorStyles.miniLabel);
        }
        if (preview.Rows.Count > previewCount)
        {
            EditorGUILayout.LabelField($"... 외 {preview.Rows.Count - previewCount}개 행", EditorStyles.miniLabel);
        }
    }

    private void DrawApplyArea()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("5. 테이블 반영", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "적용 시 Stage.csv의 해당 StageId.Time을 갱신하고, StageMonster.csv의 해당 StageId 행을 새 계산 결과로 교체합니다. " +
            "두 CSV는 변경 전에 .bak 파일로 백업됩니다.", MessageType.Warning);

        GUI.enabled = preview != null;
        if (GUILayout.Button("Stage / StageMonster 테이블에 적용", GUILayout.Height(38f)))
        {
            ApplyToTables();
        }
        GUI.enabled = true;
    }

    private void AutoArrangeSpawnStarts()
    {
        if (monsters.Count == 0)
        {
            return;
        }

        float introductionWindow = stageDurationSec * 0.65f;
        for (int i = 0; i < monsters.Count; i++)
        {
            monsters[i].SpawnStartSec = monsters.Count == 1
                ? 0f
                : introductionWindow * i / (monsters.Count - 1f);
        }
        preview = null;
    }

    private MonsterSpawnBalanceCalculator.Result BuildPreview(out string message)
    {
        List<MonsterSpawnBalanceCalculator.MonsterInput> inputs = new();
        HashSet<string> monsterIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (MonsterEntry entry in monsters)
        {
            if (entry.Prefab == null)
            {
                message = "모든 몬스터 슬롯에 프리팹을 지정해 주세요.";
                return null;
            }

            string assetPath = AssetDatabase.GetAssetPath(entry.Prefab);
            if (!assetPath.StartsWith("Assets/Resources/", StringComparison.OrdinalIgnoreCase))
            {
                message = $"{entry.Prefab.name} 프리팹이 Assets/Resources 폴더 안에 없습니다.";
                return null;
            }

            string monsterId = entry.Prefab.name;
            if (!monsterIds.Add(monsterId))
            {
                message = $"MonsterId가 중복됩니다: {monsterId}. 같은 프리팹은 한 번만 추가해 주세요.";
                return null;
            }

            if (!TryReadMonsterStats(entry.Prefab, out float hp, out float attack, out string error))
            {
                message = error;
                return null;
            }

            inputs.Add(new MonsterSpawnBalanceCalculator.MonsterInput
            {
                MonsterId = monsterId,
                MaxHp = hp,
                AttackDamage = attack,
                SpawnStartSec = entry.SpawnStartSec,
                Weight = entry.Weight
            });
        }

        if (inputs.Count == 0)
        {
            message = "최소 한 종류의 몬스터를 추가해 주세요.";
            return null;
        }

        MonsterSpawnBalanceCalculator.Settings settings = new()
        {
            StageId = stageId,
            StageDurationSec = stageDurationSec,
            TargetClearProbability = targetClearProbability,
            PlayerAttack = playerAttack,
            PlayerAttackInterval = playerAttackInterval,
            CriticalChance = criticalChance,
            CriticalDamageMultiplier = criticalDamageMultiplier,
            AttackEfficiency = attackEfficiency,
            PlayerMaxHp = playerMaxHp,
            WaveIntervalSec = waveIntervalSec,
            SegmentCount = segmentCount,
            DifficultyCurve = difficultyCurve
        };

        MonsterSpawnBalanceCalculator.Result result = MonsterSpawnBalanceCalculator.Calculate(settings, inputs);
        message = $"목표 성공 확률 {targetClearProbability:P0} 기준으로 계산했습니다. 실제 플레이테스트 결과에 따라 공격 효율과 곡선을 조정하세요.";
        return result;
    }

    private static bool TryReadMonsterStats(GameObject prefab, out float hp, out float attack, out string error)
    {
        hp = 0f;
        attack = 0f;
        error = null;
        MonsterController controller = prefab.GetComponent<MonsterController>();
        if (controller == null)
        {
            error = $"{prefab.name} 프리팹에 MonsterController가 없습니다.";
            return false;
        }

        SerializedObject serializedController = new(controller);
        SerializedProperty hpProperty = serializedController.FindProperty("maxHP");
        SerializedProperty attackProperty = serializedController.FindProperty("attackDamage");
        if (hpProperty == null || attackProperty == null)
        {
            error = $"{prefab.name}의 MonsterController 스탯을 읽을 수 없습니다.";
            return false;
        }

        hp = Mathf.Max(1f, hpProperty.floatValue);
        attack = Mathf.Max(0f, attackProperty.floatValue);
        return true;
    }

    private void ApplyToTables()
    {
        MonsterSpawnBalanceCalculator.Result latest = BuildPreview(out string message);
        if (latest == null)
        {
            preview = null;
            validationMessage = message;
            return;
        }

        try
        {
            BackupFile(StageCsvPath);
            BackupFile(StageMonsterCsvPath);
            UpdateStageCsv(stageId, stageDurationSec);
            UpdateStageMonsterCsv(stageId, latest.Rows);
            AssetDatabase.Refresh();
            preview = latest;
            validationMessage = $"StageId {stageId} 데이터를 적용했습니다. Stage.Time={stageDurationSec:0.##}초, StageMonster {latest.Rows.Count}행";
            Debug.Log(validationMessage);
        }
        catch (Exception exception)
        {
            validationMessage = $"CSV 적용 실패: {exception.Message}";
            Debug.LogException(exception);
        }
    }

    private static void BackupFile(string path)
    {
        if (File.Exists(path))
        {
            File.Copy(path, path + ".bak", true);
        }
    }

    private static void UpdateStageCsv(int targetStageId, float duration)
    {
        CsvDocument document = CsvDocument.Read(StageCsvPath);
        int stageIdIndex = document.GetColumnIndex("StageId");
        int timeIndex = document.GetColumnIndex("Time");
        if (stageIdIndex < 0 || timeIndex < 0)
        {
            throw new InvalidDataException("Stage.csv에 StageId 또는 Time 컬럼이 없습니다.");
        }

        List<string> row = document.Rows.FirstOrDefault(candidate =>
            candidate.Count > stageIdIndex && int.TryParse(candidate[stageIdIndex], out int id) && id == targetStageId);
        if (row == null)
        {
            row = Enumerable.Repeat(string.Empty, document.Headers.Count).ToList();
            row[stageIdIndex] = targetStageId.ToString(CultureInfo.InvariantCulture);
            row[timeIndex] = duration.ToString("0.###", CultureInfo.InvariantCulture);
            int nameIndex = document.GetColumnIndex("Name");
            if (nameIndex >= 0)
            {
                row[nameIndex] = $"Stage {targetStageId}";
            }
            document.Rows.Add(row);
        }
        else
        {
            EnsureColumnCount(row, document.Headers.Count);
            row[timeIndex] = duration.ToString("0.###", CultureInfo.InvariantCulture);
        }

        document.Write(StageCsvPath);
    }

    private static void UpdateStageMonsterCsv(int targetStageId, IReadOnlyList<MonsterSpawnBalanceCalculator.SpawnRow> generatedRows)
    {
        CsvDocument document = File.Exists(StageMonsterCsvPath)
            ? CsvDocument.Read(StageMonsterCsvPath)
            : new CsvDocument(new List<string>());

        string[] requiredHeaders =
        {
            "StageId", "MonsterId", "SpawnStartSec", "WaveIntervalSec", "WaveSizeStart",
            "WaveSizeGrowth", "WaveSizeMax", "TotalBudget", "MaxAliveCap", "SpawnEndSec"
        };
        foreach (string header in requiredHeaders)
        {
            document.EnsureColumn(header);
        }

        int stageIdIndex = document.GetColumnIndex("StageId");
        document.Rows.RemoveAll(row => row.Count > stageIdIndex
            && int.TryParse(row[stageIdIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            && id == targetStageId);

        foreach (MonsterSpawnBalanceCalculator.SpawnRow generated in generatedRows)
        {
            List<string> row = Enumerable.Repeat(string.Empty, document.Headers.Count).ToList();
            SetCell(document, row, "StageId", generated.StageId.ToString(CultureInfo.InvariantCulture));
            SetCell(document, row, "MonsterId", generated.MonsterId);
            SetCell(document, row, "SpawnStartSec", FormatFloat(generated.SpawnStartSec));
            SetCell(document, row, "WaveIntervalSec", FormatFloat(generated.WaveIntervalSec));
            SetCell(document, row, "WaveSizeStart", generated.WaveSizeStart.ToString(CultureInfo.InvariantCulture));
            SetCell(document, row, "WaveSizeGrowth", generated.WaveSizeGrowth.ToString(CultureInfo.InvariantCulture));
            SetCell(document, row, "WaveSizeMax", generated.WaveSizeMax.ToString(CultureInfo.InvariantCulture));
            SetCell(document, row, "TotalBudget", generated.TotalBudget.ToString(CultureInfo.InvariantCulture));
            SetCell(document, row, "MaxAliveCap", generated.MaxAliveCap.ToString(CultureInfo.InvariantCulture));
            SetCell(document, row, "SpawnEndSec", FormatFloat(generated.SpawnEndSec));
            document.Rows.Add(row);
        }

        document.Rows.Sort((left, right) =>
        {
            int leftStage = ParseIntCell(document, left, "StageId");
            int rightStage = ParseIntCell(document, right, "StageId");
            int stageCompare = leftStage.CompareTo(rightStage);
            if (stageCompare != 0)
            {
                return stageCompare;
            }
            return ParseFloatCell(document, left, "SpawnStartSec").CompareTo(ParseFloatCell(document, right, "SpawnStartSec"));
        });
        document.Write(StageMonsterCsvPath);
    }

    private static void SetCell(CsvDocument document, List<string> row, string header, string value)
    {
        row[document.GetColumnIndex(header)] = value;
    }

    private static int ParseIntCell(CsvDocument document, List<string> row, string header)
    {
        int index = document.GetColumnIndex(header);
        return index >= 0 && index < row.Count && int.TryParse(row[index], out int value) ? value : int.MaxValue;
    }

    private static float ParseFloatCell(CsvDocument document, List<string> row, string header)
    {
        int index = document.GetColumnIndex(header);
        return index >= 0 && index < row.Count
            && float.TryParse(row[index], NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : float.MaxValue;
    }

    private static string FormatFloat(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void EnsureColumnCount(List<string> row, int count)
    {
        while (row.Count < count)
        {
            row.Add(string.Empty);
        }
    }

    private sealed class CsvDocument
    {
        public readonly List<string> Headers;
        public readonly List<List<string>> Rows = new();
        private readonly Encoding encoding;

        public CsvDocument(List<string> headers, Encoding sourceEncoding = null)
        {
            Headers = headers;
            encoding = sourceEncoding ?? new UTF8Encoding(false);
        }

        public static CsvDocument Read(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Encoding sourceEncoding = DetectEncoding(bytes);
            string text = sourceEncoding.GetString(bytes);
            string[] lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return new CsvDocument(new List<string>(), sourceEncoding);
            }

            CsvDocument document = new(ParseLine(lines[0]), sourceEncoding);
            for (int i = 1; i < lines.Length; i++)
            {
                document.Rows.Add(ParseLine(lines[i]));
            }
            return document;
        }

        public int GetColumnIndex(string header) => Headers.FindIndex(value =>
            string.Equals(value.Trim(), header, StringComparison.OrdinalIgnoreCase));

        public void EnsureColumn(string header)
        {
            if (GetColumnIndex(header) >= 0)
            {
                return;
            }

            Headers.Add(header);
            foreach (List<string> row in Rows)
            {
                row.Add(string.Empty);
            }
        }

        public void Write(string path)
        {
            StringBuilder builder = new();
            builder.AppendLine(string.Join(",", Headers.Select(Escape)));
            foreach (List<string> row in Rows)
            {
                EnsureColumnCount(row, Headers.Count);
                builder.AppendLine(string.Join(",", row.Select(Escape)));
            }
            File.WriteAllText(path, builder.ToString(), encoding);
        }

        private static List<string> ParseLine(string line)
        {
            List<string> cells = new();
            StringBuilder cell = new();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char current = line[i];
                if (current == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (current == ',' && !quoted)
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                }
                else
                {
                    cell.Append(current);
                }
            }
            cells.Add(cell.ToString());
            return cells;
        }

        private static string Escape(string value)
        {
            value ??= string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }

        private static Encoding DetectEncoding(byte[] bytes)
        {
            try
            {
                UTF8Encoding strictUtf8 = new(false, true);
                strictUtf8.GetString(bytes);
                return new UTF8Encoding(false);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(949);
            }
        }
    }
}
#endif
