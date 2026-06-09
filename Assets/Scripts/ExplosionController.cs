using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStatus))]
public class ExplosionController : MonoBehaviour
{
    private const int DefaultEnemyLayerMask = 1 << 6;
    [Header("Explosion")]
    [SerializeField, Min(0f)] private float explosionRadius = 3f;
    [SerializeField] private LayerMask affectsLayers = DefaultEnemyLayerMask;
    [SerializeField, Min(0f), Tooltip("Percent of the player's current attack dealt as explosion damage. Example: 30 = 30%.")]
    private float damageMultiplier = 30f;
    [SerializeField, Range(0f, 100f)] private float explosionChancePercent;

    [Header("Effect")]
    [SerializeField] private ExplosionEffectController effectPrefab;

    [Header("References")]
    [SerializeField] private PlayerStatus playerStatus;

    private readonly List<ExplosionEffectController> effectPool = new();

    public float ExplosionRadius
    {
        get => explosionRadius;
        set => explosionRadius = Mathf.Max(0f, value);
    }

    public LayerMask AffectsLayers
    {
        get => affectsLayers;
        set => affectsLayers = value;
    }

    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }

    public float ExplosionChancePercent
    {
        get => explosionChancePercent;
        set => explosionChancePercent = Mathf.Clamp(value, 0f, 100f);
    }

    public ExplosionEffectController EffectPrefab
    {
        get => effectPrefab;
        set => effectPrefab = value;
    }

    private void Awake()
    {
        ResolvePlayerStatus();
    }

    private void OnEnable()
    {
        DamageSystem.DamageApplied += OnDamageApplied;
    }

    private void OnDisable()
    {
        DamageSystem.DamageApplied -= OnDamageApplied;
    }

    private void OnValidate()
    {
        explosionRadius = Mathf.Max(0f, explosionRadius);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        explosionChancePercent = Mathf.Clamp(explosionChancePercent, 0f, 100f);
    }

    public void SetExplosionChancePercent(float percentValue)
    {
        ExplosionChancePercent = percentValue;
    }

    private void OnDamageApplied(DamageSystem.DamageAppliedEventArgs eventArgs)
    {
        if (GamePauseState.IsGameplayPaused || eventArgs.SourcePlayerStatus != playerStatus)
        {
            return;
        }

        IEnemyDamageable enemy = eventArgs.Target as IEnemyDamageable;
        if (enemy == null || !enemy.IsDead)
        {
            return;
        }

        Component enemyComponent = enemy as Component;
        if (enemyComponent != null)
        {
            TryExplode(enemyComponent.transform.position);
        }
    }

    private void TryExplode(Vector3 position)
    {
        if (explosionChancePercent <= 0f || explosionRadius <= 0f || playerStatus == null)
        {
            return;
        }

        if (Random.value > explosionChancePercent / 100f)
        {
            return;
        }

        Explode(position);
    }

    private void Explode(Vector3 position)
    {
        PlayEffect(position);
        ApplyExplosionDamage(position);
    }

    private void ApplyExplosionDamage(Vector3 position)
    {
        float damage = playerStatus.CurrentAttack * (damageMultiplier / 100f);
        if (damage <= 0f)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, explosionRadius, affectsLayers);
        HashSet<int> damagedTargets = new();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = hit.GetComponentInParent<IDamageable>();
            }

            if (damageable == null)
            {
                continue;
            }

            Component damageableComponent = damageable as Component;
            if (damageableComponent == null)
            {
                continue;
            }

            int targetId = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.GetInstanceID()
                : damageableComponent.GetInstanceID();
            if (!damagedTargets.Add(targetId))
            {
                continue;
            }

            DamageSystem.ApplyDamage(playerStatus, damageable, damage);
        }
    }

    private void PlayEffect(Vector3 position)
    {
        ExplosionEffectController effect = GetEffect();
        if (effect == null)
        {
            return;
        }

        effect.Play(position);
    }

    private ExplosionEffectController GetEffect()
    {
        for (int i = 0; i < effectPool.Count; i++)
        {
            ExplosionEffectController pooledEffect = effectPool[i];
            if (pooledEffect != null && !pooledEffect.gameObject.activeSelf)
            {
                return pooledEffect;
            }
        }

        if (effectPrefab == null)
        {
            return null;
        }

        ExplosionEffectController effect = Instantiate(effectPrefab);
        effectPool.Add(effect);
        return effect;
    }

    private void ResolvePlayerStatus()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.45f, 0.05f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
