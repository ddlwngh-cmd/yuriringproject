using UnityEngine;

public class ExpOrbController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float expValue = 1f;
    [SerializeField, Min(0f)] private float moveSpeed = 6f;

    public float ExpValue => expValue;

    private bool isAbsorbing;

    private void Update()
    {
        ExpDropManager manager = ExpDropManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (!isAbsorbing)
        {
            isAbsorbing = manager.IsPlayerInMagnetRange(transform.position);
            if (!isAbsorbing)
            {
                return;
            }
        }

        Transform player = manager.GetPlayerTransform();
        if (player == null)
        {
            isAbsorbing = false;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

        if ((player.position - transform.position).sqrMagnitude <= 0.01f)
        {
            manager.AddExp(expValue);
            Destroy(gameObject);
        }
    }
}
