using UnityEngine;

public class AutoShooter : MonoBehaviour
{
    [Header("Auto Fire")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float fireInterval = 0.35f;
    [SerializeField] private float minTurnCooldown = 0.1f;

    [Header("Projectile Stats")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float speed = 7f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float scale = 1f;

    private Vector2 lastInputDirection = Vector2.right;
    private Vector2 lastShotDirection = Vector2.right;
    private float nextFireTime;

    private void Awake()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        nextFireTime = Time.time + fireInterval;
    }

    private void Update()
    {
        UpdateAimDirection();

        if (projectilePrefab == null || Time.time < nextFireTime)
        {
            return;
        }

        Fire(lastInputDirection);
    }

    private void UpdateAimDirection()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (input.sqrMagnitude > 0f)
        {
            lastInputDirection = input.normalized;
        }
    }

    private void Fire(Vector2 direction)
    {
        bool isTurned = Vector2.Angle(lastShotDirection, direction) > 0.01f;
        float cooldown = fireInterval;

        if (isTurned)
        {
            cooldown += minTurnCooldown;
            lastShotDirection = direction;
        }

        Projectile projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        projectile.Initialize(direction, speed, lifetime, damage, scale);

        nextFireTime = Time.time + cooldown;
    }
}
