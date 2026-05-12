using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveDirection = Vector2.right;
    private float moveSpeed;
    private float damage;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
    }

    public void Initialize(Vector2 direction, float speed, float lifetime, float projectileDamage, float projectileScale)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        damage = projectileDamage;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
