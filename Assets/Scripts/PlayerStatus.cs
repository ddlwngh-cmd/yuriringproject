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

    public event Action<float> PickupRadiusChanged;

    private void Awake()
    {
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
        return currentAttack * Mathf.Max(0f, damageMultiplier);
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
