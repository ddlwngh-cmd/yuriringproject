using UnityEngine;

public class MonsterController : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField, Min(1f)] private float maxHP = 30f;
    [SerializeField, Min(0f)] private float attackDamage = 10f;

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
            Destroy(gameObject);
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
