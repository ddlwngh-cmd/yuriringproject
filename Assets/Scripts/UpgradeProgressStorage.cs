using System;
using UnityEngine;

public static class UpgradeProgressStorage
{
    private const string LevelKeyPrefix = "UpgradeLevel_";

    public static event Action ProgressChanged;

    public static int LoadLevel(UpgradeStat stat)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(GetLevelKey(stat), 0), 0, 5);
    }

    public static void SaveLevel(UpgradeStat stat, int level)
    {
        PlayerPrefs.SetInt(GetLevelKey(stat), Mathf.Clamp(level, 0, 5));
        PlayerPrefs.Save();
        ProgressChanged?.Invoke();
    }

    public static void ResetAllLevels()
    {
        foreach (UpgradeStat stat in Enum.GetValues(typeof(UpgradeStat)))
        {
            PlayerPrefs.DeleteKey(GetLevelKey(stat));
        }

        PlayerPrefs.Save();
        ProgressChanged?.Invoke();
    }

    private static string GetLevelKey(UpgradeStat stat)
    {
        return $"{LevelKeyPrefix}{stat}";
    }
}
