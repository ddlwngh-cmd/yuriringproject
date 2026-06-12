using UnityEngine;

public class ExpOrbController : MagnetCollectible
{
    [SerializeField, Min(0f)] private float expValue = 1f;

    public float ExpValue => expValue;

    protected override void OnCollected(GameObject player)
    {
        XPOrbAudioPlayer.Play();

        ExpDropManager manager = ExpDropManager.Instance;
        if (manager != null)
        {
            manager.AddExp(expValue);
        }
        else
        {
            PlayerExperiences experiences = player.GetComponent<PlayerExperiences>();
            if (experiences != null && expValue > 0f && !GamePauseState.IsGameplayPaused)
            {
                experiences.AddExp(expValue);
            }
        }

        Destroy(gameObject);
    }
}
