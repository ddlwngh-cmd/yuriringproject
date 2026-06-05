using UnityEngine;

public interface IMagnetCollectible
{
    bool IsAbsorbing { get; }
    float MoveSpeed { get; }
    void BeginAbsorb(Transform target);
    void TickMagnetMove(Transform target, float deltaTime);
    void Collect(GameObject player);
}

public abstract class MagnetCollectible : MonoBehaviour, IMagnetCollectible
{
    public const string CollectibleTag = "MagnetCollectible";

    [SerializeField, Min(0f)] private float moveSpeed = 6f;
    [SerializeField, Min(0f)] private float collectDistance = 0.1f;

    private Transform magnetBoostTarget;
    private float magnetBoostSpeedMultiplier = 1f;

    public bool IsAbsorbing { get; private set; }
    public bool IsMagnetBoosted { get; private set; }
    public float MoveSpeed => moveSpeed;

    protected virtual void Reset()
    {
        ApplyCollectibleTag();
    }

    protected virtual void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        MagnetBoostController activeMagnetBoost = MagnetBoostController.ActiveInstance;
        if (activeMagnetBoost != null && activeMagnetBoost.IsBoostActive)
        {
            activeMagnetBoost.ApplyBoostToCollectible(this);
        }
    }

    protected virtual void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        collectDistance = Mathf.Max(0f, collectDistance);
        ApplyCollectibleTag();
    }

    protected virtual void Update()
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        ExpDropManager manager = ExpDropManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (IsMagnetBoosted)
        {
            if (magnetBoostTarget == null)
            {
                ClearMagnetBoost();
            }
            else
            {
                BeginAbsorb(magnetBoostTarget);
            }
        }

        if (!IsAbsorbing)
        {
            Transform playerInRange = manager.GetPlayerTransformIfInPickupRange(transform.position);
            if (playerInRange == null)
            {
                return;
            }

            BeginAbsorb(playerInRange);
        }

        Transform player = IsMagnetBoosted && magnetBoostTarget != null ? magnetBoostTarget : manager.GetPlayerTransform();
        if (player == null)
        {
            IsAbsorbing = false;
            return;
        }

        TickMagnetMove(player, Time.deltaTime);

        if ((player.position - transform.position).sqrMagnitude <= collectDistance * collectDistance)
        {
            Collect(player.gameObject);
        }
    }

    public void BeginAbsorb(Transform target)
    {
        IsAbsorbing = target != null;
    }

    public void TickMagnetMove(Transform target, float deltaTime)
    {
        if (target == null)
        {
            IsAbsorbing = false;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.position, GetCurrentMoveSpeed() * deltaTime);
    }

    public void Collect(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        OnCollected(player);
    }

    protected abstract void OnCollected(GameObject player);

    public void ApplyMagnetBoost(Transform target, float speedMultiplier)
    {
        if (target == null)
        {
            return;
        }

        magnetBoostTarget = target;
        magnetBoostSpeedMultiplier = Mathf.Max(0f, speedMultiplier);
        IsMagnetBoosted = true;
        BeginAbsorb(target);
    }

    public void ClearMagnetBoost()
    {
        IsMagnetBoosted = false;
        magnetBoostTarget = null;
        magnetBoostSpeedMultiplier = 1f;

        ExpDropManager manager = ExpDropManager.Instance;
        if (manager == null || manager.GetPlayerTransformIfInPickupRange(transform.position) == null)
        {
            IsAbsorbing = false;
        }
    }

    private float GetCurrentMoveSpeed()
    {
        return moveSpeed * (IsMagnetBoosted ? magnetBoostSpeedMultiplier : 1f);
    }

    private void ApplyCollectibleTag()
    {
        if (!CompareTag(CollectibleTag))
        {
            gameObject.tag = CollectibleTag;
        }
    }
}
