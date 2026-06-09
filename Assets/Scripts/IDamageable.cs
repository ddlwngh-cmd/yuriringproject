public interface IDamageable
{
    float TakeDamage(float damage);
}

public interface IEnemyDamageable : IDamageable
{
    bool IsDead { get; }
}
