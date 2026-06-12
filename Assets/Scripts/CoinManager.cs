using System;
using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [SerializeField, Min(0)] private int currentCoin;
    [SerializeField] private TMP_Text coinText;

    private bool hasSavedSessionCoin;

    public event Action<int> CoinChanged;

    public int CurrentCoin => currentCoin;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("A duplicate CoinManager was destroyed.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (coinText == null)
        {
            coinText = GetComponent<TMP_Text>();
        }

        UpdateCoinText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddCoin(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentCoin = (int)Math.Min((long)currentCoin + amount, int.MaxValue);
        UpdateCoinText();
        CoinChanged?.Invoke(currentCoin);
    }

    public int SaveSessionCoin()
    {
        if (hasSavedSessionCoin)
        {
            return CoinStorage.LoadTotalCoin();
        }

        hasSavedSessionCoin = true;
        return CoinStorage.AddCoin(currentCoin);
    }

    private void OnValidate()
    {
        currentCoin = Mathf.Max(0, currentCoin);

        if (coinText == null)
        {
            coinText = GetComponent<TMP_Text>();
        }

        if (!Application.isPlaying)
        {
            UpdateCoinText();
        }
    }

    private void UpdateCoinText()
    {
        if (coinText != null)
        {
            Localization localization = coinText.GetComponent<Localization>();
            if (localization != null)
            {
                localization.SetKeyAndArguments("ui_game_coin", currentCoin);
            }
            else
            {
                coinText.text = LocalizationManager.Get("ui_game_coin", currentCoin);
            }
        }
    }
}
