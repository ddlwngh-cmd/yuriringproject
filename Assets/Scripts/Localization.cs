using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Localization : MonoBehaviour
{
    [SerializeField] private string stringKey;

    private TMP_Text targetText;
    private object[] formatArguments;

    public string StringKey => stringKey;

    private void Awake()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Refresh;
    }

    public void SetKey(string key)
    {
        stringKey = key;
        formatArguments = null;
        Refresh();
    }

    public void SetArguments(params object[] arguments)
    {
        formatArguments = arguments;
        Refresh();
    }

    public void SetKeyAndArguments(string key, params object[] arguments)
    {
        stringKey = key;
        formatArguments = arguments;
        Refresh();
    }

    public void Refresh()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }

        if (targetText == null || string.IsNullOrWhiteSpace(stringKey))
        {
            return;
        }

        targetText.text = formatArguments is { Length: > 0 }
            ? LocalizationManager.Get(stringKey, formatArguments)
            : LocalizationManager.Get(stringKey);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        targetText = GetComponent<TMP_Text>();
        if (!Application.isPlaying && targetText != null && !string.IsNullOrWhiteSpace(stringKey))
        {
            targetText.text = LocalizationManager.Get(stringKey);
        }
    }
#endif
}
