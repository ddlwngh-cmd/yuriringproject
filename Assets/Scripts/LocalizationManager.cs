using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public enum GameLanguage
{
    KOR,
    ENG
}

public static class LocalizationManager
{
    private const string StringTableResourcePath = "StringTable";
    private static readonly Dictionary<string, LocalizedString> strings = new(StringComparer.OrdinalIgnoreCase);
    private static bool isLoaded;

    public static event Action LanguageChanged;

    public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.KOR;

    public static void SetLanguage(GameLanguage language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        CurrentLanguage = language;
        LanguageChanged?.Invoke();
    }

    public static void SetKorean()
    {
        SetLanguage(GameLanguage.KOR);
    }

    public static void SetEnglish()
    {
        SetLanguage(GameLanguage.ENG);
    }

    public static string Get(string stringKey)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(stringKey))
        {
            return string.Empty;
        }

        if (!strings.TryGetValue(stringKey, out LocalizedString value))
        {
            Debug.LogWarning($"StringTable key was not found: {stringKey}");
            return stringKey;
        }

        return CurrentLanguage == GameLanguage.ENG ? value.English : value.Korean;
    }

    public static string Get(string stringKey, params object[] arguments)
    {
        string format = Get(stringKey);
        try
        {
            return string.Format(CultureInfo.CurrentCulture, format, arguments);
        }
        catch (FormatException exception)
        {
            Debug.LogError($"Invalid localized format for key '{stringKey}': {format}\n{exception.Message}");
            return format;
        }
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        isLoaded = true;
        TextAsset csv = Resources.Load<TextAsset>(StringTableResourcePath);
        if (csv == null)
        {
            Debug.LogError($"Could not load Resources/{StringTableResourcePath}.csv.");
            return;
        }

        List<List<string>> rows = ParseCsv(csv.text);
        if (rows.Count == 0)
        {
            return;
        }

        int keyIndex = FindColumnIndex(rows[0], "StringKey");
        int koreanIndex = FindColumnIndex(rows[0], "KOR");
        int englishIndex = FindColumnIndex(rows[0], "ENG");
        if (keyIndex < 0 || koreanIndex < 0 || englishIndex < 0)
        {
            Debug.LogError("StringTable.csv must contain StringKey, KOR, and ENG columns.");
            return;
        }

        int requiredColumnCount = Mathf.Max(keyIndex, Mathf.Max(koreanIndex, englishIndex)) + 1;
        for (int i = 1; i < rows.Count; i++)
        {
            List<string> columns = rows[i];
            if (columns.Count < requiredColumnCount)
            {
                Debug.LogWarning($"Skipped invalid StringTable.csv row {i + 1}.");
                continue;
            }

            string key = columns[keyIndex].Trim();
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            strings[key] = new LocalizedString(columns[koreanIndex], columns[englishIndex]);
        }
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

    private static List<List<string>> ParseCsv(string csvText)
    {
        List<List<string>> rows = new();
        List<string> row = new();
        StringBuilder value = new();
        bool insideQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char character = csvText[i];
            if (character == '"')
            {
                if (insideQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
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
                row.Add(value.ToString());
                value.Clear();
            }
            else if ((character == '\r' || character == '\n') && !insideQuotes)
            {
                if (character == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(value.ToString());
                value.Clear();
                if (row.Count > 1 || row[0].Length > 0)
                {
                    rows.Add(row);
                }
                row = new List<string>();
            }
            else
            {
                value.Append(character);
            }
        }

        if (value.Length > 0 || row.Count > 0)
        {
            row.Add(value.ToString());
            rows.Add(row);
        }

        return rows;
    }

    private readonly struct LocalizedString
    {
        public LocalizedString(string korean, string english)
        {
            Korean = korean;
            English = string.IsNullOrEmpty(english) ? korean : english;
        }

        public string Korean { get; }
        public string English { get; }
    }
}
