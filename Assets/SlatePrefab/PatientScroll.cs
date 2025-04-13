using TMPro;
using UnityEngine;

public class PatientScroll : MonoBehaviour
{
    public GameObject page1;
    public GameObject page2;



    public void ShowPage1()
    {
        if (page1 != null)
        {
            page1.gameObject.SetActive(true);

        }
        if (page2 != null)
        {
            page2.gameObject.SetActive(false);
        }
    }

    // Function to activate text object 2, deactivate text object 1, and set font size
    public void ShowPage2()
    {
        if (page1 != null)
        {
            page1.gameObject.SetActive(false);
        }
        if (page2 != null)
        {
            page2.gameObject.SetActive(true);
        }
    }
}