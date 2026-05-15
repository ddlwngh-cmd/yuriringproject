using UnityEngine;

public class monsterflip : MonoBehaviour
{
    [SerializeField] private Transform target;

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

        float xDelta = target.position.x - transform.position.x;
        if (Mathf.Abs(xDelta) <= 0.01f)
        {
            return;
        }

        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * (xDelta < 0f ? -1f : 1f);
        transform.localScale = localScale;
    }
}
