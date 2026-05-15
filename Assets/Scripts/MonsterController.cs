using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 2f;

    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = (target.position - transform.position);
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
    }
}
