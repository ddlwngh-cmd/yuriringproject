using System;
using UnityEngine;

public class MonsterController : MonoBehaviour, IEnemyDamageable
{
    [Serializable]
    public struct ExpOrbDropEntry
    {
        public GameObject orbPrefab;
        [Range(0f, 1f)] public float dropChance;
    }

    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField, Min(1f)] private float maxHP = 30f;
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField] private ExpOrbDropEntry[] expOrbDropTable;

    private Transform target;
    private float currentHP;
    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void Update()
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        Vector3 direction = (target.position - transform.position);
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
    }

    public float TakeDamage(float damage)
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return 0f;
        }

        if (isDead || damage <= 0f)
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
        DropExpOrbs();
        Destroy(gameObject);
    }

    private void DropExpOrbs()
    {
        if (expOrbDropTable == null || expOrbDropTable.Length == 0)
        {
            return;
        }

        for (int i = 0; i < expOrbDropTable.Length; i++)
        {
            ExpOrbDropEntry entry = expOrbDropTable[i];
            if (entry.orbPrefab == null || entry.dropChance <= 0f)
            {
                continue;
            }

            if (UnityEngine.Random.value <= entry.dropChance)
            {
                Instantiate(entry.orbPrefab, transform.position, Quaternion.identity);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D playerCollider)
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = playerCollider.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.TryTakeDamage(attackDamage);
        }
    }
}
