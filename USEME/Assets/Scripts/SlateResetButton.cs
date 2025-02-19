using UnityEngine;

public class SlateResetButton : MonoBehaviour
{
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private Vector3 defaultScale;
    private SlateFollower slateFollower;
    private Transform slateTransform;

    void Start()
    {
        // Find the top-level Slate object (not just the button's direct parent)
        slateTransform = transform.parent;

        if (slateTransform == null)
        {
            Debug.LogError("SlateResetButton: No Slate parent found! Make sure this button is a child of the Slate.");
            return;
        }

        // Store the exact initial position, rotation, and scale of the Slate
        defaultPosition = slateTransform.position;
        defaultRotation = slateTransform.rotation;
        defaultScale = slateTransform.localScale;

        // Get reference to the SlateFollower script
        slateFollower = slateTransform.GetComponent<SlateFollower>();
    }

    public void ResetSlate()
    {
        if (slateTransform != null)
        {
            // **Reset position, rotation, and scale to the original values**
            slateTransform.position = defaultPosition;
            slateTransform.rotation = defaultRotation;
            slateTransform.localScale = defaultScale;

            Debug.Log("Slate Reset: Position, Rotation, and Scale restored.");

            // **Reset SlateFollower if it exists**
            if (slateFollower != null)
            {
                slateFollower.ResetToGaze();
                Debug.Log("SlateFollower Reset: Gaze-following restored.");
            }
        }
        else
        {
            Debug.LogError("SlateResetButton: No Slate reference found!");
        }
    }
}
