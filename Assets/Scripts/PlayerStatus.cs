using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStatus : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField, Min(0f)] private float baseAttack = 10f;
    [SerializeField, Min(0f)] private float currentAttack;

    [Header("Health")]
    [SerializeField, Min(1f)] private float baseMaxHP = 100f;
    [SerializeField, Min(1f)] private float currentMaxHP;
    [SerializeField, Min(0f)] private float currentHP;

    [Header("Permanent Upgrade")]
    [SerializeField, Range(0, 10000)] private int criticalChance;
    [SerializeField, Min(0)] private int revivalCount;

    [Header("Pickup")]
    [SerializeField, Min(0f)] private float basePickupRadius = 2f;
    [SerializeField, Min(0f)] private float currentPickupRadius;

    private float attackUpMultiplier = 1f;
    private float maxHPUpMultiplier = 1f;
    private float pickupRadiusMultiplier = 1f;
    private float damageHealPercent;

    public float BaseAttack => baseAttack;
    public float CurrentAttack => currentAttack;
    public float BaseMaxHP => baseMaxHP;
    public float CurrentMaxHP => currentMaxHP;
    public float CurrentHP => currentHP;
    public float BasePickupRadius => basePickupRadius;
    public float CurrentPickupRadius => currentPickupRadius;
    public float DamageHealPercent => damageHealPercent;
    public int CriticalChance => criticalChance;
    public int RevivalCount => revivalCount;

    public event Action<float> PickupRadiusChanged;

    private void Awake()
    {
        ApplyPermanentUpgrades();
        RecalculateCurrentAttack();
        RecalculateCurrentPickupRadius(false);
        InitializeHealthForBattle();
    }

    private void OnValidate()
    {
        baseAttack = Mathf.Max(0f, baseAttack);
        attackUpMultiplier = Mathf.Max(0f, attackUpMultiplier);
        baseMaxHP = Mathf.Max(1f, baseMaxHP);
        maxHPUpMultiplier = Mathf.Max(0f, maxHPUpMultiplier);
        basePickupRadius = Mathf.Max(0f, basePickupRadius);
        pickupRadiusMultiplier = Mathf.Max(0f, pickupRadiusMultiplier);
        damageHealPercent = Mathf.Max(0f, damageHealPercent);
        criticalChance = Mathf.Clamp(criticalChance, 0, 10000);
        revivalCount = Mathf.Max(0, revivalCount);
        RecalculateCurrentAttack();
        RecalculateCurrentPickupRadius(false);

        if (!Application.isPlaying)
        {
            ResetHealthPreview();
        }
    }

    public void SetAttackUpPercent(float percentValue)
    {
        attackUpMultiplier = Mathf.Max(0f, percentValue / 100f);
        RecalculateCurrentAttack();
    }

    public void InitializeHealthForBattle()
    {
        maxHPUpMultiplier = 1f;
        currentMaxHP = baseMaxHP;
        currentHP = currentMaxHP;
        damageHealPercent = 0f;
    }

    public void SetPickupRadiusPercent(float percentValue)
    {
        pickupRadiusMultiplier = Mathf.Max(0f, percentValue / 100f);
        RecalculateCurrentPickupRadius(true);
    }

    public void RefreshPickupRadius()
    {
        RecalculateCurrentPickupRadius(true);
    }

    public void SetMaxHPUpPercent(float percentValue)
    {
        float previousMaxHP = Mathf.Max(1f, currentMaxHP);
        maxHPUpMultiplier = Mathf.Max(0f, percentValue / 100f);
        currentMaxHP = Mathf.Max(1f, baseMaxHP * maxHPUpMultiplier);

        float healthScale = currentMaxHP / previousMaxHP;
        currentHP = Mathf.Min(currentMaxHP, Mathf.Max(0f, currentHP * healthScale));
    }

    public float TakeDamage(float damage)
    {
        if (damage <= 0f)
        {
            return 0f;
        }

        float previousHP = currentHP;
        currentHP = Mathf.Max(0f, currentHP - damage);
        return previousHP - currentHP;
    }

    public void SetDamageHealPercent(float percentValue)
    {
        damageHealPercent = Mathf.Max(0f, percentValue);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentHP = Mathf.Min(currentMaxHP, currentHP + amount);
    }

    public float CalculateDamage(float damageMultiplier)
    {
        float damage = currentAttack * Mathf.Max(0f, damageMultiplier);
        bool isCritical = criticalChance > 0 && UnityEngine.Random.Range(0, 10000) < criticalChance;
        return isCritical ? damage * 2f : damage;
    }

    public void RestoreFullHealth()
    {
        currentHP = currentMaxHP;
    }

    private void ApplyPermanentUpgrades()
    {
        UpgradeStatusData attack = UpgradeStatusRepository.GetCurrent(UpgradeStat.ATK);
        UpgradeStatusData hp = UpgradeStatusRepository.GetCurrent(UpgradeStat.HP);
        UpgradeStatusData radius = UpgradeStatusRepository.GetCurrent(UpgradeStat.Radius);
        UpgradeStatusData critical = UpgradeStatusRepository.GetCurrent(UpgradeStat.CRI);
        UpgradeStatusData revival = UpgradeStatusRepository.GetCurrent(UpgradeStat.Revival);

        if (attack != null) baseAttack = Mathf.Max(0f, attack.StatValue);
        if (hp != null) baseMaxHP = Mathf.Max(1f, hp.StatValue);
        if (radius != null) basePickupRadius = Mathf.Max(0f, radius.StatValue);
        if (critical != null) criticalChance = Mathf.Clamp(Mathf.RoundToInt(critical.StatValue), 0, 10000);
        if (revival != null) revivalCount = Mathf.Max(0, Mathf.RoundToInt(revival.StatValue));
    }

    private void RecalculateCurrentAttack()
    {
        currentAttack = baseAttack * attackUpMultiplier;
    }

    private void RecalculateCurrentPickupRadius(bool notify)
    {
        float previousPickupRadius = currentPickupRadius;
        currentPickupRadius = basePickupRadius * pickupRadiusMultiplier;

        if (notify && !Mathf.Approximately(previousPickupRadius, currentPickupRadius))
        {
            PickupRadiusChanged?.Invoke(currentPickupRadius);
        }
    }

    private void ResetHealthPreview()
    {
        currentMaxHP = baseMaxHP;
        currentHP = currentMaxHP;
    }
}
