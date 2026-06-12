using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCellView : MonoBehaviour
{
    private TMP_Text statusText;
    private TMP_Text priceText;
    private Button buyButton;
    private Image[] dots;
    private Sprite dotOffSprite;
    private Sprite dotOnSprite;
    private UpgradeStat stat;
    private IReadOnlyList<UpgradeStatusData> levels;
    private Action<UpgradeStat> purchaseRequested;

    public void Bind(
        UpgradeStat upgradeStat,
        IReadOnlyList<UpgradeStatusData> upgradeLevels,
        Sprite offSprite,
        Sprite onSprite,
        Action<UpgradeStat> onPurchaseRequested)
    {
        stat = upgradeStat;
        levels = upgradeLevels;
        dotOffSprite = offSprite;
        dotOnSprite = onSprite;
        purchaseRequested = onPurchaseRequested;

        statusText = transform.Find("TXT_Status")?.GetComponent<TMP_Text>();
        buyButton = transform.Find("BTN_Buy")?.GetComponent<Button>();
        priceText = transform.Find("BTN_Buy/Text (TMP)")?.GetComponent<TMP_Text>();

        Transform dotsRoot = transform.Find("Dots");
        dots = new Image[5];
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i] = dotsRoot?.Find($"Dots_{i + 1}")?.GetComponent<Image>();
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(RequestPurchase);
            buyButton.onClick.AddListener(RequestPurchase);
        }

        Refresh(CoinStorage.LoadTotalCoin());
    }

    public void Refresh(int currentCoin)
    {
        if (levels == null || levels.Count == 0)
        {
            if (buyButton != null)
            {
                buyButton.interactable = false;
            }

            return;
        }

        int currentLevel = Mathf.Clamp(UpgradeProgressStorage.LoadLevel(stat), 0, 5);
        UpgradeStatusData nextLevel = FindLevel(currentLevel + 1);

        if (statusText != null)
        {
            Localization localization = statusText.GetComponent<Localization>();
            if (localization != null)
            {
                localization.SetKey(levels[0].StringKey);
            }
            else
            {
                statusText.text = LocalizationManager.Get(levels[0].StringKey);
            }
        }

        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null)
            {
                dots[i].sprite = i < currentLevel ? dotOnSprite : dotOffSprite;
            }
        }

        bool canUpgrade = nextLevel != null;
        if (priceText != null)
        {
            Localization localization = priceText.GetComponent<Localization>();
            if (localization != null)
            {
                if (canUpgrade)
                {
                    localization.SetKeyAndArguments("ui_upgrade_price", nextLevel.CoinValue);
                }
                else
                {
                    localization.SetKey("ui_upgrade_max");
                }
            }
            else
            {
                priceText.text = canUpgrade
                    ? LocalizationManager.Get("ui_upgrade_price", nextLevel.CoinValue)
                    : LocalizationManager.Get("ui_upgrade_max");
            }
        }

        if (buyButton != null)
        {
            buyButton.interactable = canUpgrade && currentCoin >= nextLevel.CoinValue;
        }
    }

    private UpgradeStatusData FindLevel(int level)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i].Level == level)
            {
                return levels[i];
            }
        }

        return null;
    }

    private void RequestPurchase()
    {
        purchaseRequested?.Invoke(stat);
    }

    private void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(RequestPurchase);
        }
    }
}
