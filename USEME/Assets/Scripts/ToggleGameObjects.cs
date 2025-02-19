using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleGameObjects : MonoBehaviour
{
    public GameObject objectToHide;
    public GameObject objectToShow;

    public void ToggleObjects()  // Ensure it's public and has no parameters
    {
        if (objectToHide != null) objectToHide.SetActive(false);
        if (objectToShow != null) objectToShow.SetActive(true);
    }
}
