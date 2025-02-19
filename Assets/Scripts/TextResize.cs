using TMPro;
using UnityEngine;

public class TextResize : MonoBehaviour
{
    public TMP_Text textObject; // Reference to the TextMeshPro object
    public float sizeStep = 2f; // Step size for font change
    public float minSize = 10f;
    public float maxSize = 100f;

    // Function to increase text size
    public void IncreaseTextSize()
    {
        if (textObject != null && textObject.fontSize < maxSize)
        {
            textObject.fontSize += sizeStep;
        }
    }

    // Function to decrease text size
    public void DecreaseTextSize()
    {
        if (textObject != null && textObject.fontSize > minSize)
        {
            textObject.fontSize -= sizeStep;
        }
    }
}