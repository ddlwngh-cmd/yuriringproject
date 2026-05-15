using System;
using UnityEngine;

public class MonsterController : MonoBehaviour, IDamageable
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

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        currentHP = Mathf.Max(0f, currentHP - damage);
        if (currentHP <= 0f)
        {
            DropExpOrbs();
            Destroy(gameObject);
        }
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
