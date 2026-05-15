using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHP = 100f;
    [SerializeField, Min(0f)] private float invincibleTime = 1f;
    [SerializeField] private Image hpBar;

    [Header("Visual")]
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;

    private float currentHP;
    private bool isDead;
    private bool isInvincible;
    private Coroutine invincibleRoutine;
    private SpriteRenderer[] spriteRenderers;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsInvincible => isInvincible;

    private void Awake()
    {
        currentHP = maxHP;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        UpdateHPUI();
    }

    public bool TryTakeDamage(float damage)
    {
        if (isDead || isInvincible || damage <= 0f)
        {
            return false;
        }

        currentHP = Mathf.Max(0f, currentHP - damage);
        UpdateHPUI();

        if (currentHP <= 0f)
        {
            Die();
            return true;
        }

        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
            SetSpritesVisible(true);
        }

        invincibleRoutine = StartCoroutine(InvincibleCoroutine());
        return true;
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        UpdateHPUI();
    }

    public void IncreaseMaxHP(float amount, bool healByIncrease = true)
    {
        if (amount <= 0f)
        {
            return;
        }

        maxHP += amount;

        if (healByIncrease)
        {
            currentHP = Mathf.Min(maxHP, currentHP + amount);
        }
        else
        {
            currentHP = Mathf.Min(currentHP, maxHP);
        }

        UpdateHPUI();
    }

    private IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invincibleTime)
        {
            visible = !visible;
            SetSpritesVisible(visible);
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        SetSpritesVisible(true);
        isInvincible = false;
        invincibleRoutine = null;
    }

    private void UpdateHPUI()
    {
        if (hpBar == null)
        {
            return;
        }

        hpBar.fillAmount = maxHP <= 0f ? 0f : currentHP / maxHP;
    }

    private void SetSpritesVisible(bool visible)
    {
        if (spriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].enabled = visible;
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isInvincible = false;
        SetSpritesVisible(true);
        onDeath?.Invoke();
    }
}
