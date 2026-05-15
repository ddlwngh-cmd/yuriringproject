using UnityEngine;

public class ExpDropManager : MonoBehaviour
{
    public static ExpDropManager Instance { get; private set; }

    [SerializeField, Min(0f)] private float magnetRange = 2f;
    [SerializeField, Min(0f)] private float currentExp;

    public float MagnetRange => magnetRange;
    public float CurrentExp => currentExp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        if (amount <= 0f)
        {
            return;
        }

        currentExp += amount;
    }
}
