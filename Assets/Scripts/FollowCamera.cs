using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 5f;

    private Transform playerTarget;
    private float fixedZ;

    private void Awake()
    {
        fixedZ = transform.position.z;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("FollowCamera could not find an object with the Player tag.");
        }
    }

    private void LateUpdate()
    {
        if (playerTarget == null)
        {
            return;
        }

        Vector3 targetPosition = playerTarget.position;
        targetPosition.z = fixedZ;

        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}
