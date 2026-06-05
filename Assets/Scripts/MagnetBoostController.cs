using UnityEngine;

[DisallowMultipleComponent]
public class MagnetBoostController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float boostDuration = 5f;
    [SerializeField, Min(0f)] private float boostSpeedMultiplier = 4f;

    private float remainingBoostTime;
    private bool isBoostActive;

    public static MagnetBoostController ActiveInstance { get; private set; }

    public float BoostDuration => boostDuration;
    public float BoostSpeedMultiplier => boostSpeedMultiplier;
    public bool IsBoostActive => isBoostActive;

    private void Awake()
    {
        ActiveInstance = this;
    }

    private void OnValidate()
    {
        boostDuration = Mathf.Max(0f, boostDuration);
        boostSpeedMultiplier = Mathf.Max(0f, boostSpeedMultiplier);
    }

    private void OnDestroy()
    {
        if (ActiveInstance == this)
        {
            ActiveInstance = null;
        }
    }

    private void Update()
    {
        if (!isBoostActive)
        {
            return;
        }

        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        remainingBoostTime -= Time.deltaTime;
        if (remainingBoostTime <= 0f)
        {
            StopMagnetBoost();
            return;
        }

        ApplyBoostToCollectiblesInScene();
    }

    public void MagnetBoost()
    {
        remainingBoostTime = boostDuration;
        isBoostActive = boostDuration > 0f;

        if (!isBoostActive)
        {
            StopMagnetBoost();
            return;
        }

        ApplyBoostToCollectiblesInScene();
    }

    public void ApplyBoostToCollectible(MagnetCollectible collectible)
    {
        if (!isBoostActive || collectible == null || collectible.gameObject == gameObject)
        {
            return;
        }

        collectible.ApplyMagnetBoost(transform, boostSpeedMultiplier);
    }

    private void ApplyBoostToCollectiblesInScene()
    {
        GameObject[] collectibleObjects = GameObject.FindGameObjectsWithTag(MagnetCollectible.CollectibleTag);
        for (int i = 0; i < collectibleObjects.Length; i++)
        {
            GameObject collectibleObject = collectibleObjects[i];
            if (collectibleObject == null)
            {
                continue;
            }

            ApplyBoostToCollectible(collectibleObject.GetComponent<MagnetCollectible>());
        }
    }

    private void StopMagnetBoost()
    {
        isBoostActive = false;
        remainingBoostTime = 0f;

        GameObject[] collectibleObjects = GameObject.FindGameObjectsWithTag(MagnetCollectible.CollectibleTag);
        for (int i = 0; i < collectibleObjects.Length; i++)
        {
            GameObject collectibleObject = collectibleObjects[i];
            if (collectibleObject == null)
            {
                continue;
            }

            MagnetCollectible collectible = collectibleObject.GetComponent<MagnetCollectible>();
            if (collectible != null)
            {
                collectible.ClearMagnetBoost();
            }
        }
    }
}
