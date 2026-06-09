using UnityEngine;

public class CoinController : MagnetCollectible
{
    [SerializeField, Min(0)] private int minCoin = 1;
    [SerializeField, Min(0)] private int maxCoin = 5;

    private bool isCollected;

    public int MinCoin => minCoin;
    public int MaxCoin => maxCoin;

    protected override void OnValidate()
    {
        base.OnValidate();
        minCoin = Mathf.Max(0, minCoin);
        maxCoin = Mathf.Max(minCoin, maxCoin);
    }

    protected override void OnCollected(GameObject player)
    {
        if (isCollected || GamePauseState.IsGameplayPaused)
        {
            return;
        }

        CoinManager coinManager = CoinManager.Instance;
        if (coinManager == null)
        {
            Debug.LogWarning("CoinPocket could not find a CoinManager, so it was not collected.", this);
            return;
        }

        isCollected = true;
        coinManager.AddCoin(Random.Range(minCoin, maxCoin + 1));
        Destroy(gameObject);
    }
}
