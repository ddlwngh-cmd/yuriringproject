using System;

public static class DamageSystem
{
    public readonly struct DamageAppliedEventArgs
    {
        public DamageAppliedEventArgs(PlayerStatus sourcePlayerStatus, IDamageable target, float requestedDamage, float appliedDamage)
        {
            SourcePlayerStatus = sourcePlayerStatus;
            Target = target;
            RequestedDamage = requestedDamage;
            AppliedDamage = appliedDamage;
        }

        public PlayerStatus SourcePlayerStatus { get; }
        public IDamageable Target { get; }
        public float RequestedDamage { get; }
        public float AppliedDamage { get; }
    }

    public static event Action<DamageAppliedEventArgs> DamageApplied;

    public static float ApplyDamage(PlayerStatus sourcePlayerStatus, IDamageable target, float damage)
    {
        if (target == null || damage <= 0f)
        {
            return 0f;
        }

        float appliedDamage = target.TakeDamage(damage);
        if (appliedDamage <= 0f)
        {
            return 0f;
        }

        DamageApplied?.Invoke(new DamageAppliedEventArgs(sourcePlayerStatus, target, damage, appliedDamage));
        return appliedDamage;
    }
}
