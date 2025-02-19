using UnityEngine;

public class SlateVisibilityToggle : MonoBehaviour
{
    private bool isSlateHidden = false;  

    public void ToggleSlateVisibility()
    {
        // Iterate through all child objects of the Slate except the button itself
        foreach (Transform child in transform.parent)
        {
            if (child != transform) 
            {
                child.gameObject.SetActive(isSlateHidden);
            }
        }
        isSlateHidden = !isSlateHidden;
    }
}
