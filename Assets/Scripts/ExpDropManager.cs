using UnityEngine;

public class ExpDropManager : MonoBehaviour
{
    public static ExpDropManager Instance { get; private set; }

    [SerializeField, Min(0f)] private float magnetRange = 2f;
    [SerializeField] private PlayerExperiences playerExperiences;

    public float MagnetRange => magnetRange;
    public float CurrentExp => playerExperiences != null ? playerExperiences.CurrentExp : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CachePlayerExperiences();
    }

    public bool IsPlayerInMagnetRange(Vector3 orbPosition)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return false;
        }

        float distance = Vector3.Distance(player.transform.position, orbPosition);
        return distance <= magnetRange;
    }

    public Transform GetPlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
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

        CachePlayerExperiences();
        if (playerExperiences != null)
        {
            playerExperiences.AddExp(amount);
        }
    }

    private void CachePlayerExperiences()
    {
        if (playerExperiences != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerExperiences = player.GetComponent<PlayerExperiences>();
        }

        if (playerExperiences == null)
        {
            playerExperiences = PlayerExperiences.Instance;
        }
    }
}
