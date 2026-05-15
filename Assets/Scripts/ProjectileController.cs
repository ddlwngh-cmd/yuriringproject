using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField, Min(0f)] private float projectileDamage = 10f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveDirection = Vector2.right;
    private float moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
    }

    public void Initialize(Vector2 direction, float speed, float lifetime, float damage, float projectileScale)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        projectileDamage = damage;
        transform.localScale = Vector3.one * projectileScale;

        ApplySpriteOrientation(moveDirection);
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            transform.position += (Vector3)(moveDirection * moveSpeed * Time.fixedDeltaTime);
            return;
        }

        rb.linearVelocity = moveDirection * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CanHit(other.gameObject.layer))
        {
            return;
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(projectileDamage);
            Destroy(gameObject);
        }
    }

    private bool CanHit(int targetLayer)
    {
        return (enemyLayer.value & (1 << targetLayer)) != 0;
    }

    private void ApplySpriteOrientation(Vector2 direction)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = direction.x < 0f;
        float angle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
