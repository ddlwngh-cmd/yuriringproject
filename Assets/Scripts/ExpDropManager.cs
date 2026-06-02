using System;
using UnityEngine;

public class ExpDropManager : MonoBehaviour
{
    public static ExpDropManager Instance { get; private set; }

    [SerializeField, Min(0f)] private float magnetRange = 2f;
    [SerializeField] private PlayerExperiences playerExperiences;
    [SerializeField] private PlayerStatus playerStatus;

    private Transform playerTransform;
    private float cachedPickupRadius;

    public float MagnetRange => CurrentPickupRadius;
    public float CurrentPickupRadius => cachedPickupRadius;
    public float CurrentExp => playerExperiences != null ? playerExperiences.CurrentExp : 0f;

    public event Action<float> PickupRadiusChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cachedPickupRadius = magnetRange;
        CachePlayerReferences();
        SubscribeToPlayerStatus();
        RefreshPickupRadius();
    }

    private void Start()
    {
        CachePlayerReferences();
        SubscribeToPlayerStatus();
        RefreshPickupRadius();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        UnsubscribeFromPlayerStatus();
    }

    public bool IsPlayerInMagnetRange(Vector3 collectiblePosition)
    {
        return GetPlayerTransformIfInPickupRange(collectiblePosition) != null;
    }

    public Transform GetPlayerTransformIfInPickupRange(Vector3 collectiblePosition)
    {
        Transform player = GetPlayerTransform();
        if (player == null)
        {
            return null;
        }

        float pickupRadius = CurrentPickupRadius;
        return (player.position - collectiblePosition).sqrMagnitude <= pickupRadius * pickupRadius ? player : null;
    }

    public Transform GetPlayerTransform()
    {
        if (playerTransform == null)
        {
            CachePlayerReferences();
        }

        return playerTransform;
    }

    public void AddExp(float amount)
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        if (amount <= 0f)
        {
            return;
        }

        CachePlayerReferences();
        if (playerExperiences != null)
        {
            playerExperiences.AddExp(amount);
        }
    }

    public void RefreshPickupRadius()
    {
        if (playerStatus != null)
        {
            playerStatus.RefreshPickupRadius();
            cachedPickupRadius = playerStatus.CurrentPickupRadius;
            return;
        }

        float previousPickupRadius = cachedPickupRadius;
        cachedPickupRadius = magnetRange;

        if (!Mathf.Approximately(previousPickupRadius, cachedPickupRadius))
        {
            PickupRadiusChanged?.Invoke(cachedPickupRadius);
        }
    }

    private void CachePlayerReferences()
    {
        GameObject player = null;
        if (playerTransform == null || playerExperiences == null || playerStatus == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerTransform == null && player != null)
        {
            playerTransform = player.transform;
        }

        if (playerExperiences == null && player != null)
        {
            playerExperiences = player.GetComponent<PlayerExperiences>();
        }

        if (playerStatus == null && player != null)
        {
            playerStatus = player.GetComponent<PlayerStatus>();
        }

        if (playerExperiences == null)
        {
            playerExperiences = PlayerExperiences.Instance;
            if (playerExperiences != null && playerTransform == null)
            {
                playerTransform = playerExperiences.transform;
            }
        }

        if (playerStatus == null && playerTransform != null)
        {
            playerStatus = playerTransform.GetComponent<PlayerStatus>();
        }
    }

    private void SubscribeToPlayerStatus()
    {
        if (playerStatus == null)
        {
            return;
        }

        playerStatus.PickupRadiusChanged -= OnPlayerPickupRadiusChanged;
        playerStatus.PickupRadiusChanged += OnPlayerPickupRadiusChanged;
    }

    private void UnsubscribeFromPlayerStatus()
    {
        if (playerStatus != null)
        {
            playerStatus.PickupRadiusChanged -= OnPlayerPickupRadiusChanged;
        }
    }

    private void OnPlayerPickupRadiusChanged(float pickupRadius)
    {
        cachedPickupRadius = pickupRadius;
        PickupRadiusChanged?.Invoke(cachedPickupRadius);
    }
}
