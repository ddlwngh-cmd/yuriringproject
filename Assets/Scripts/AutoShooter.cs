using UnityEngine;

public class AutoShooter : MonoBehaviour
{
    [Header("Auto Fire")]
    [SerializeField] private ProjectileController projectilePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private float fireInterval = 0.35f;
    [SerializeField] private float minTurnCooldown = 0.1f;

    [Header("Projectile Stats")]
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;
    [SerializeField] private float speed = 7f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float scale = 1f;

    private const float MinFireInterval = 0.05f;

    private Vector2 lastInputDirection = Vector2.right;
    private Vector2 lastShotDirection = Vector2.right;
    private float nextFireTime;

    private void Awake()
    {
        UpgradeStatusData upgrade = UpgradeStatusRepository.GetCurrent(UpgradeStat.ATKSpeed);
        if (upgrade != null)
        {
            fireInterval = Mathf.Max(MinFireInterval, upgrade.StatValue);
        }

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }

        nextFireTime = Time.time + fireInterval;
    }

    public void AddDamageMultiplier(float amount)
    {
        damageMultiplier = Mathf.Max(0f, damageMultiplier + amount);
    }

    public void AddDamageMultiplierPercent(float percent)
    {
        damageMultiplier = Mathf.Max(0f, damageMultiplier * (1f + percent / 100f));
    }

    public void AddFireInterval(float amount)
    {
        fireInterval = Mathf.Max(MinFireInterval, fireInterval + amount);
    }

    public void ReduceFireIntervalPercent(float percent)
    {
        fireInterval = Mathf.Max(MinFireInterval, fireInterval * (1f - percent / 100f));
    }

    public void AddProjectileSpeed(float amount)
    {
        speed = Mathf.Max(0f, speed + amount);
    }

    public void AddProjectileSpeedPercent(float percent)
    {
        speed = Mathf.Max(0f, speed * (1f + percent / 100f));
    }

    public void AddProjectileLifetime(float amount)
    {
        lifetime = Mathf.Max(0.01f, lifetime + amount);
    }

    public void AddProjectileScale(float amount)
    {
        scale = Mathf.Max(0.01f, scale + amount);
    }

    private void Update()
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

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

        ProjectileController projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        projectile.Initialize(direction, speed, lifetime, playerStatus, damageMultiplier, scale);

        nextFireTime = Time.time + cooldown;
    }
}
