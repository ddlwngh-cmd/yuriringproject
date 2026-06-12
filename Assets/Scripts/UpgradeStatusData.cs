using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum UpgradeStat
{
    ATK,
    HP,
    Radius,
    MoveSpeed,
    ATKSpeed,
    CRI,
    Revival
}

public sealed class UpgradeStatusData
{
    public UpgradeStatusData(int id, UpgradeStat stat, string stringKey, int level, float statValue, int coinValue)
    {
        Id = id;
        Stat = stat;
        StringKey = stringKey;
        Level = level;
        StatValue = statValue;
        CoinValue = coinValue;
    }

    public int Id { get; }
    public UpgradeStat Stat { get; }
    public string StringKey { get; }
    public int Level { get; }
    public float StatValue { get; }
    public int CoinValue { get; }
}

public static class UpgradeStatusRepository
{
    private const string DefaultResourcePath = "UpgradeStatus";
    private static Dictionary<UpgradeStat, List<UpgradeStatusData>> cachedTable;

    public static IReadOnlyDictionary<UpgradeStat, List<UpgradeStatusData>> Load(string resourcePath = DefaultResourcePath)
    {
        if (cachedTable != null && resourcePath == DefaultResourcePath)
        {
            return cachedTable;
        }

        Dictionary<UpgradeStat, List<UpgradeStatusData>> table = new Dictionary<UpgradeStat, List<UpgradeStatusData>>();
        TextAsset csv = Resources.Load<TextAsset>(resourcePath);
        if (csv == null)
        {
            Debug.LogError($"Could not load Resources/{resourcePath}.csv.");
            return table;
        }

        string[] lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');
            if (columns.Length < 6
                || !int.TryParse(columns[0].Trim(), out int id)
                || !Enum.TryParse(columns[1].Trim(), true, out UpgradeStat stat)
                || !int.TryParse(columns[3].Trim(), out int level)
                || !float.TryParse(columns[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float statValue)
                || !int.TryParse(columns[5].Trim(), out int coinValue))
            {
                Debug.LogWarning($"Skipped invalid upgrade CSV row {i + 1}: {lines[i]}");
                continue;
            }

            if (level < 0 || level > 5 || coinValue < 0)
            {
                Debug.LogWarning($"Skipped out-of-range upgrade CSV row {i + 1}: {lines[i]}");
                continue;
            }

            if (!table.TryGetValue(stat, out List<UpgradeStatusData> levels))
            {
                levels = new List<UpgradeStatusData>();
                table.Add(stat, levels);
            }

            levels.Add(new UpgradeStatusData(id, stat, columns[2].Trim(), level, statValue, coinValue));
        }

        foreach (List<UpgradeStatusData> levels in table.Values)
        {
            levels.Sort((left, right) => left.Level.CompareTo(right.Level));
        }

        if (resourcePath == DefaultResourcePath)
        {
            cachedTable = table;
        }

        return table;
    }

    public static UpgradeStatusData GetCurrent(UpgradeStat stat)
    {
        IReadOnlyDictionary<UpgradeStat, List<UpgradeStatusData>> table = Load();
        if (!table.TryGetValue(stat, out List<UpgradeStatusData> levels) || levels.Count == 0)
        {
            return null;
        }

        int savedLevel = UpgradeProgressStorage.LoadLevel(stat);
        UpgradeStatusData result = levels[0];
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i].Level > savedLevel)
            {
                break;
            }

            result = levels[i];
        }

        return result;
    }
}
