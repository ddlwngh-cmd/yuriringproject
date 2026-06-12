using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerStatus))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField, Min(0f)] private float invincibleTime = 1f;
    [SerializeField] private Image hpBar;

    [Header("Visual")]
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;

    private bool isDead;
    private bool isInvincible;
    private Coroutine invincibleRoutine;
    private SpriteRenderer[] spriteRenderers;
    private int remainingRevivals;

    public float MaxHP => playerStatus != null ? playerStatus.CurrentMaxHP : 0f;
    public float CurrentHP => playerStatus != null ? playerStatus.CurrentHP : 0f;
    public bool IsInvincible => isInvincible;
    public event Action Died;

    private void Awake()
    {
        ResolvePlayerStatus();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Start()
    {
        playerStatus?.InitializeHealthForBattle();
        remainingRevivals = playerStatus != null ? playerStatus.RevivalCount : 0;
        UpdateHPUI();
    }

    private void OnEnable()
    {
        DamageSystem.DamageApplied += OnDamageApplied;
    }

    private void OnDisable()
    {
        DamageSystem.DamageApplied -= OnDamageApplied;
    }

    public bool TryTakeDamage(float damage)
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return false;
        }

        if (isDead || isInvincible || damage <= 0f || playerStatus == null)
        {
            return false;
        }

        playerStatus.TakeDamage(damage);
        UpdateHPUI();

        if (playerStatus.CurrentHP <= 0f)
        {
            if (TryRevive())
            {
                return true;
            }

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
        if (isDead || amount <= 0f || playerStatus == null)
        {
            return;
        }

        playerStatus.Heal(amount);
        UpdateHPUI();
    }

    public void SetMaxHPUpPercent(float percentValue)
    {
        if (isDead || playerStatus == null)
        {
            return;
        }

        playerStatus.SetMaxHPUpPercent(percentValue);
        UpdateHPUI();
    }

    public void SetDamageHealPercent(float percentValue)
    {
        if (playerStatus == null)
        {
            return;
        }

        playerStatus.SetDamageHealPercent(percentValue);
    }

    private void OnDamageApplied(DamageSystem.DamageAppliedEventArgs eventArgs)
    {
        if (isDead || playerStatus == null || eventArgs.SourcePlayerStatus != playerStatus)
        {
            return;
        }

        float healAmount = eventArgs.AppliedDamage * (playerStatus.DamageHealPercent / 100f);
        if (healAmount <= 0f)
        {
            return;
        }

        playerStatus.Heal(healAmount);
        UpdateHPUI();
    }

    private void ResolvePlayerStatus()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
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

        float maxHP = MaxHP;
        hpBar.fillAmount = maxHP <= 0f ? 0f : CurrentHP / maxHP;
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

    private bool TryRevive()
    {
        if (remainingRevivals <= 0 || playerStatus == null)
        {
            return false;
        }

        remainingRevivals--;
        playerStatus.RestoreFullHealth();
        UpdateHPUI();

        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
        }

        SetSpritesVisible(true);
        invincibleRoutine = StartCoroutine(InvincibleCoroutine());
        return true;
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
        Died?.Invoke();
        onDeath?.Invoke();
    }
}
