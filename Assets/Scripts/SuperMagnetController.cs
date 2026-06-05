using UnityEngine;

public class SuperMagnetController : MagnetCollectible
{
    protected override void OnCollected(GameObject player)
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        MagnetBoostController magnetBoostController = player.GetComponent<MagnetBoostController>();
        if (magnetBoostController == null)
        {
            magnetBoostController = player.GetComponentInParent<MagnetBoostController>();
        }

        if (magnetBoostController == null)
        {
            magnetBoostController = player.GetComponentInChildren<MagnetBoostController>();
        }

        if (magnetBoostController != null)
        {
            magnetBoostController.MagnetBoost();
        }

        Destroy(gameObject);
    }
}
