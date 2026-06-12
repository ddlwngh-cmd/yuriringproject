using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public sealed class StageData
{
    public int StageId;
    public string Tilemap;
    public float Time;
    public string Image;
    public string StringKey;
}

public static class StageDataRepository
{
    public static List<StageData> Load(string resourcePath = "Stage")
    {
        TextAsset csv = Resources.Load<TextAsset>(resourcePath);
        if (csv == null)
        {
            Debug.LogError($"Could not load stage CSV at Resources/{resourcePath}.csv");
            return new List<StageData>();
        }

        return Parse(csv.text);
    }

    public static List<StageData> Parse(string csvText)
    {
        List<StageData> stages = new();
        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return stages;
        }

        List<string> headers = SplitCsvLine(lines[0]);
        int stageIdIndex = FindColumnIndex(headers, "StageId");
        int tilemapIndex = FindColumnIndex(headers, "Tilemap");
        int timeIndex = FindColumnIndex(headers, "Time");
        int imageIndex = FindColumnIndex(headers, "Image");
        int stringKeyIndex = FindColumnIndex(headers, "StringKey");

        if (stageIdIndex < 0 || tilemapIndex < 0 || timeIndex < 0 || imageIndex < 0 || stringKeyIndex < 0)
        {
            Debug.LogError("Stage.csv must contain StageId, Tilemap, Time, Image, and StringKey columns.");
            return stages;
        }

        int requiredColumnCount = Mathf.Max(stageIdIndex,
            Mathf.Max(tilemapIndex, Mathf.Max(timeIndex, Mathf.Max(imageIndex, stringKeyIndex)))) + 1;

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> columns = SplitCsvLine(lines[i]);
            if (columns.Count < requiredColumnCount
                || !int.TryParse(columns[stageIdIndex].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stageId)
                || !float.TryParse(columns[timeIndex].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float time))
            {
                Debug.LogWarning($"Skipped invalid Stage.csv row {i + 1}: {lines[i]}");
                continue;
            }

            stages.Add(new StageData
            {
                StageId = stageId,
                Tilemap = columns[tilemapIndex].Trim(),
                Time = Mathf.Max(0f, time),
                Image = columns[imageIndex].Trim(),
                StringKey = columns[stringKeyIndex].Trim()
            });
        }

        return stages;
    }

    private static int FindColumnIndex(IReadOnlyList<string> headers, string columnName)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i].Trim().TrimStart('\ufeff'), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static List<string> SplitCsvLine(string line)
    {
        List<string> columns = new();
        bool insideQuotes = false;
        System.Text.StringBuilder value = new();

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];
            if (character == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (character == ',' && !insideQuotes)
            {
                columns.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        columns.Add(value.ToString());
        return columns;
    }
}

public static class StageSelectionState
{
    public static StageData SelectedStage { get; private set; }

    public static bool HasSelection => SelectedStage != null;

    public static void Select(StageData stage)
    {
        SelectedStage = stage;
    }
}
