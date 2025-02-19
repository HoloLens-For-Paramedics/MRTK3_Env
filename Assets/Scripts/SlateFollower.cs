using UnityEngine;

public class SlateFollower : MonoBehaviour
{
    public float followSpeed = 3.0f;       // Speed of following gaze
    public float distanceFromUser = 1.5f;  // Default distance in front of the user
    public float positionLerpSpeed = 3.0f; // Adjusted for smoother movement
    public float rotationLerpSpeed = 2.5f; // Adjusted for better stability

    private Transform userHead;
    private Vector3 localOffset;           // Offset relative to the user's head
    private Quaternion localRotationOffset;

    private bool isBeingMoved = false;
    private bool hasBeenMovedByUser = false; // Track if user manually placed it

    private float movementThreshold = 0.0005f;  // Threshold to prevent micro-movements
    private float rotationThreshold = 0.1f;     // Prevents small rotational jitter

    void Start()
    {
        userHead = Camera.main.transform;
        SetInitialPosition();
    }

    void Update()
    {
        if (!isBeingMoved)
        {
            if (!hasBeenMovedByUser)
            {
                FollowGaze(); // Only follow gaze if the user hasn't moved it
            }
            else
            {
                MaintainUserPlacement();
            }
        }
    }

    void SetInitialPosition()
    {
        if (userHead != null)
        {
            transform.position = userHead.position + userHead.forward * distanceFromUser;
            transform.rotation = Quaternion.LookRotation(transform.position - userHead.position);

            localOffset = Quaternion.Inverse(userHead.rotation) * (transform.position - userHead.position);
            localRotationOffset = Quaternion.Inverse(userHead.rotation) * transform.rotation;
        }
    }

    void FollowGaze()
    {
        if (userHead != null)
        {
            // Update target position relative to the user's current head position and rotation
            Vector3 targetPosition = userHead.position + userHead.rotation * localOffset;
            Quaternion targetRotation = userHead.rotation * localRotationOffset;

            if (Vector3.Distance(transform.position, targetPosition) > movementThreshold)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
            }
            if (Quaternion.Angle(transform.rotation, targetRotation) > rotationThreshold)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
            }
        }
    }

    void MaintainUserPlacement()
    {
        // Keep the slate in its manually moved position relative to the user's head
        transform.position = userHead.position + userHead.rotation * localOffset;
        transform.rotation = userHead.rotation * localRotationOffset;
    }

    public void StartMoving()
    {
        isBeingMoved = true;
    }

    public void StopMoving()
    {
        isBeingMoved = false;
        hasBeenMovedByUser = true;  // Mark that the user manually placed it

        // **Store the new offset relative to the user's head**
        localOffset = Quaternion.Inverse(userHead.rotation) * (transform.position - userHead.position);
        localRotationOffset = Quaternion.Inverse(userHead.rotation) * transform.rotation;
    }

    public void ResetToGaze()
    {
        hasBeenMovedByUser = false; // Allow gaze-following again
        SetInitialPosition();
    }
}
