using System;
using UnityEngine;

public static class CoinStorage
{
    private const string TotalCoinKey = "TotalCoin";

    public static int LoadTotalCoin()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(TotalCoinKey, 0));
    }

    public static int AddCoin(int amount)
    {
        int currentTotal = LoadTotalCoin();
        if (amount <= 0)
        {
            return currentTotal;
        }

        int newTotal = (int)Math.Min((long)currentTotal + amount, int.MaxValue);
        SaveTotalCoin(newTotal);
        return newTotal;
    }

    public static void ResetCoin()
    {
        SaveTotalCoin(0);
    }

    private static void SaveTotalCoin(int amount)
    {
        PlayerPrefs.SetInt(TotalCoinKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }
}
