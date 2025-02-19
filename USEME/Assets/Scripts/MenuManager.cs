using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    public List<RawImage> menuImages = new List<RawImage>(); // Ensure the list is initialized
    public List<GameObject> selectorMenu = new List<GameObject>(); // Ensure the list is initialized

    void Start()
    {
        // Hide all images by default
        foreach (RawImage image in menuImages)
        {
            image.gameObject.SetActive(false);
        }

        // Hide all selectors by default
        foreach (GameObject selector in selectorMenu)
        {
            selector.SetActive(false);
        }
    }

    public void ShowImage(RawImage selectedImage)
    {
        foreach (RawImage image in menuImages)
        {
            image.gameObject.SetActive(image == selectedImage);
        }
    }

    public void ShowSelector(GameObject selectedSelector)
    {


        foreach (GameObject selector in selectorMenu)
        {
            selector.SetActive(selector == selectedSelector);
        }
    }
}
