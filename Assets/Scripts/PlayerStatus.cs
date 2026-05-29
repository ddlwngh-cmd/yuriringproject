using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStatus : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField, Min(0f)] private float baseAttack = 10f;
    [SerializeField, Min(0f)] private float currentAttack;

    private float attackUpMultiplier = 1f;

    public float BaseAttack => baseAttack;
    public float CurrentAttack => currentAttack;

    private void Awake()
    {
        RecalculateCurrentAttack();
    }

    private void OnValidate()
    {
        baseAttack = Mathf.Max(0f, baseAttack);
        attackUpMultiplier = Mathf.Max(0f, attackUpMultiplier);
        RecalculateCurrentAttack();
    }

    public void SetAttackUpPercent(float percentValue)
    {
        attackUpMultiplier = Mathf.Max(0f, percentValue / 100f);
        RecalculateCurrentAttack();
    }

    public float CalculateDamage(float damageMultiplier)
    {
        return currentAttack * Mathf.Max(0f, damageMultiplier);
    }

    private void RecalculateCurrentAttack()
    {
        currentAttack = baseAttack * attackUpMultiplier;
    }
}
