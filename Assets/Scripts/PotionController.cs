using UnityEngine;

public class PotionController : MagnetCollectible
{
    [SerializeField, Min(0f)] private float healValue = 20f;

    public float HealValue => healValue;

    protected override void OnValidate()
    {
        base.OnValidate();
        healValue = Mathf.Max(0f, healValue);
    }

    protected override void OnCollected(GameObject player)
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = player.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null && healValue > 0f)
        {
            playerHealth.Heal(healValue);
        }

        Destroy(gameObject);
    }
}
