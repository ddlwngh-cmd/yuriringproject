using UnityEngine;

public class monsterflip : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
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

    private void LateUpdate()
    {
        if (target == null || spriteRenderer == null)
        {
            return;
        }

        float deltaX = target.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= 0.001f)
        {
            return;
        }

        spriteRenderer.flipX = deltaX < 0f;
    }
}
