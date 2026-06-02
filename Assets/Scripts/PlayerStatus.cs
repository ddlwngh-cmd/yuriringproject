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

    private float attackUpMultiplier = 1f;
    private float maxHPUpMultiplier = 1f;

    public float BaseAttack => baseAttack;
    public float CurrentAttack => currentAttack;
    public float BaseMaxHP => baseMaxHP;
    public float CurrentMaxHP => currentMaxHP;
    public float CurrentHP => currentHP;

    private void Awake()
    {
        RecalculateCurrentAttack();
        InitializeHealthForBattle();
    }

    private void OnValidate()
    {
        baseAttack = Mathf.Max(0f, baseAttack);
        attackUpMultiplier = Mathf.Max(0f, attackUpMultiplier);
        baseMaxHP = Mathf.Max(1f, baseMaxHP);
        maxHPUpMultiplier = Mathf.Max(0f, maxHPUpMultiplier);
        RecalculateCurrentAttack();

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
    }

    public void SetMaxHPUpPercent(float percentValue)
    {
        float previousMaxHP = Mathf.Max(1f, currentMaxHP);
        maxHPUpMultiplier = Mathf.Max(0f, percentValue / 100f);
        currentMaxHP = Mathf.Max(1f, baseMaxHP * maxHPUpMultiplier);

        float healthScale = currentMaxHP / previousMaxHP;
        currentHP = Mathf.Min(currentMaxHP, Mathf.Max(0f, currentHP * healthScale));
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        currentHP = Mathf.Max(0f, currentHP - damage);
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

    private void ResetHealthPreview()
    {
        currentMaxHP = baseMaxHP;
        currentHP = currentMaxHP;
    }
}
