using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStatus))]
public class FireRingController : MonoBehaviour
{
    private const int DefaultEnemyLayerMask = 1 << 6;

    [Header("Orbit")]
    [SerializeField, Min(0f), Tooltip("Orbit diameter. Each fire orb is placed at half this distance from the player center.")]
    private float orbitRadius = 3f;
    [SerializeField, Tooltip("Orbit rotation speed in degrees per second.")]
    private float orbitSpeed = 45f;

    [Header("Damage")]
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("Minimum seconds between repeated damage ticks on the same monster.")]
    private float damageCooldown = 0.35f;
    [SerializeField] private LayerMask enemyLayer = DefaultEnemyLayerMask;

    [Header("Prefab")]
    [SerializeField] private GameObject fireOrbPrefab;
    [SerializeField] private PlayerStatus playerStatus;

    private readonly List<GameObject> fireOrbs = new();
    private readonly Dictionary<int, float> nextDamageTimesByTarget = new();
    private float currentAngle;

    public float OrbitRadius
    {
        get => orbitRadius;
        set => orbitRadius = Mathf.Max(0f, value);
    }

    public float OrbitSpeed
    {
        get => orbitSpeed;
        set => orbitSpeed = value;
    }

    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }

    public float DamageCooldown
    {
        get => damageCooldown;
        set => damageCooldown = Mathf.Max(0f, value);
    }

    public GameObject FireOrbPrefab
    {
        get => fireOrbPrefab;
        set => fireOrbPrefab = value;
    }

    public int OrbCount => fireOrbs.Count;

    private void Awake()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
    }

    private void OnValidate()
    {
        orbitRadius = Mathf.Max(0f, orbitRadius);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        damageCooldown = Mathf.Max(0f, damageCooldown);
    }

    private void Update()
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        if (fireOrbs.Count == 0)
        {
            return;
        }

        currentAngle = Mathf.Repeat(currentAngle + orbitSpeed * Time.deltaTime, 360f);
        UpdateOrbPositions();
    }

    private void OnDestroy()
    {
        for (int i = fireOrbs.Count - 1; i >= 0; i--)
        {
            if (fireOrbs[i] != null)
            {
                Destroy(fireOrbs[i]);
            }
        }

        fireOrbs.Clear();
        nextDamageTimesByTarget.Clear();
    }

    public void SetOrbCount(int count)
    {
        int targetCount = Mathf.Max(0, count);
        if (fireOrbPrefab == null && targetCount > fireOrbs.Count)
        {
            Debug.LogWarning("FireRingController cannot create fire orbs because FireOrbPrefab is not assigned.");
            return;
        }

        while (fireOrbs.Count < targetCount)
        {
            CreateFireOrb();
        }

        while (fireOrbs.Count > targetCount)
        {
            int lastIndex = fireOrbs.Count - 1;
            GameObject orb = fireOrbs[lastIndex];
            fireOrbs.RemoveAt(lastIndex);
            if (orb != null)
            {
                Destroy(orb);
            }
        }

        UpdateOrbPositions();
    }

    public void AddOrbCount(int amount)
    {
        SetOrbCount(OrbCount + amount);
    }

    internal void TryDamage(Collider2D targetCollider)
    {
        if (GamePauseState.IsGameplayPaused || targetCollider == null || !CanHit(targetCollider.gameObject.layer))
        {
            return;
        }

        IDamageable damageable = targetCollider.GetComponent<IDamageable>();
        if (damageable == null)
        {
            damageable = targetCollider.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
        {
            return;
        }

        Component damageableComponent = damageable as Component;
        if (damageableComponent == null)
        {
            return;
        }

        int targetId = GetCooldownTargetId(targetCollider, damageableComponent);
        if (nextDamageTimesByTarget.TryGetValue(targetId, out float nextDamageTime) && Time.time < nextDamageTime)
        {
            return;
        }

        float damage = playerStatus != null ? playerStatus.CalculateDamage(damageMultiplier) : 0f;
        float appliedDamage = DamageSystem.ApplyDamage(playerStatus, damageable, damage);
        if (appliedDamage > 0f)
        {
            nextDamageTimesByTarget[targetId] = Time.time + damageCooldown;
        }
    }

    private void CreateFireOrb()
    {
        GameObject orb = Instantiate(fireOrbPrefab, transform);
        orb.name = $"FireOrb_{fireOrbs.Count + 1}";
        ConfigureOrbObject(orb);
        fireOrbs.Add(orb);
    }

    private void ConfigureOrbObject(GameObject orb)
    {
        Rigidbody2D rb = orb.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Collider2D[] colliders = orb.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
            FireRingOrb hitbox = colliders[i].GetComponent<FireRingOrb>();
            if (hitbox == null)
            {
                hitbox = colliders[i].gameObject.AddComponent<FireRingOrb>();
            }

            hitbox.Initialize(this);
        }

        if (colliders.Length == 0)
        {
            FireRingOrb hitbox = orb.GetComponent<FireRingOrb>();
            if (hitbox == null)
            {
                hitbox = orb.AddComponent<FireRingOrb>();
            }

            hitbox.Initialize(this);
        }
    }

    private void UpdateOrbPositions()
    {
        if (fireOrbs.Count == 0)
        {
            return;
        }

        float distanceFromPlayer = orbitRadius * 0.5f;
        float angleStep = 360f / fireOrbs.Count;
        for (int i = 0; i < fireOrbs.Count; i++)
        {
            GameObject orb = fireOrbs[i];
            if (orb == null)
            {
                continue;
            }

            float angle = (currentAngle + angleStep * i) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distanceFromPlayer;
            orb.transform.localPosition = offset;
            orb.transform.localRotation = Quaternion.identity;
        }
    }

    private bool CanHit(int targetLayer)
    {
        return (enemyLayer.value & (1 << targetLayer)) != 0;
    }

    private static int GetCooldownTargetId(Collider2D targetCollider, Component damageableComponent)
    {
        return targetCollider.attachedRigidbody != null
            ? targetCollider.attachedRigidbody.GetInstanceID()
            : damageableComponent.GetInstanceID();
    }
}

internal class FireRingOrb : MonoBehaviour
{
    private FireRingController owner;

    public void Initialize(FireRingController controller)
    {
        owner = controller;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        owner?.TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        owner?.TryDamage(other);
    }
}
