using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TopDownPlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;

    private Animator animator;
    private Vector2 lastFacing = Vector2.down;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.SetFloat("moveX", lastFacing.x);
        animator.SetFloat("moveY", lastFacing.y);
        animator.SetBool("isMoving", false);
    }

    private void Update()
    {
        Vector2 rawInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector2 movement = rawInput.normalized;
        bool isMoving = rawInput.sqrMagnitude > 0f;

        if (isMoving)
        {
            transform.position += (Vector3)(movement * moveSpeed * Time.deltaTime);

            Vector2 animationDirection = GetStableAnimationDirection(rawInput);
            lastFacing = animationDirection;

            animator.SetFloat("moveX", animationDirection.x);
            animator.SetFloat("moveY", animationDirection.y);
            animator.SetBool("isMoving", true);
        }
        else
        {
            animator.SetFloat("moveX", lastFacing.x);
            animator.SetFloat("moveY", lastFacing.y);
            animator.SetBool("isMoving", false);
        }
    }

    private Vector2 GetStableAnimationDirection(Vector2 input)
    {
        float absX = Mathf.Abs(input.x);
        float absY = Mathf.Abs(input.y);

        if (absX > absY)
        {
            return new Vector2(Mathf.Sign(input.x), 0f);
        }

        if (absY > absX)
        {
            return new Vector2(0f, Mathf.Sign(input.y));
        }

        // Keep the previous facing axis on perfect diagonal input to prevent flicker.
        if (Mathf.Abs(lastFacing.x) > 0f)
        {
            return new Vector2(Mathf.Sign(input.x), 0f);
        }

        return new Vector2(0f, Mathf.Sign(input.y));
    }
}
