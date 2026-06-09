using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TreasureBoxController : MonoBehaviour, IEnemyDamageable
{
    [Serializable]
    public struct DropEntry
    {
        [InspectorName("DropPrefab")]
        public GameObject dropPrefab;

        [InspectorName("Chance"), Min(0f)]
        public float chance;
    }

    [SerializeField, InspectorName("MaxHP"), Min(1f)] private float maxHP = 100f;
    [SerializeField] private DropEntry[] dropTable;

    private float currentHP;
    private bool isDead;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHP = maxHP;
    }

    private void OnValidate()
    {
        maxHP = Mathf.Max(1f, maxHP);

        if (dropTable == null)
        {
            return;
        }

        for (int i = 0; i < dropTable.Length; i++)
        {
            DropEntry entry = dropTable[i];
            entry.chance = Mathf.Max(0f, entry.chance);
            dropTable[i] = entry;
        }
    }

    public float TakeDamage(float damage)
    {
        if (GamePauseState.IsGameplayPaused || isDead || damage <= 0f)
        {
            return 0f;
        }

        float previousHP = currentHP;
        currentHP = Mathf.Max(0f, currentHP - damage);
        float appliedDamage = previousHP - currentHP;

        if (currentHP <= 0f)
        {
            Die();
        }

        return appliedDamage;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        DropReward();
        Destroy(gameObject);
    }

    private void DropReward()
    {
        if (dropTable == null || dropTable.Length == 0)
        {
            return;
        }

        float totalChance = 0f;
        for (int i = 0; i < dropTable.Length; i++)
        {
            DropEntry entry = dropTable[i];
            if (entry.dropPrefab != null && entry.chance > 0f)
            {
                totalChance += entry.chance;
            }
        }

        if (totalChance <= 0f)
        {
            return;
        }

        float roll = UnityEngine.Random.value * totalChance;
        GameObject fallbackPrefab = null;

        for (int i = 0; i < dropTable.Length; i++)
        {
            DropEntry entry = dropTable[i];
            if (entry.dropPrefab == null || entry.chance <= 0f)
            {
                continue;
            }

            fallbackPrefab = entry.dropPrefab;
            roll -= entry.chance;
            if (roll <= 0f)
            {
                Instantiate(entry.dropPrefab, transform.position, Quaternion.identity);
                return;
            }
        }

        if (fallbackPrefab != null)
        {
            Instantiate(fallbackPrefab, transform.position, Quaternion.identity);
        }
    }
}
