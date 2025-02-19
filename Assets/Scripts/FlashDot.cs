using System.Collections;
using UnityEngine;

public class FlashingObject : MonoBehaviour
{
    public GameObject targetObject;
    public float flashInterval = 0.5f; // Adjust the speed of flashing
    private bool isFlashing = false;
    private Coroutine flashCoroutine;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) && !isFlashing)
        {
            isFlashing = true;
            flashCoroutine = StartCoroutine(FlashObject());
        }
        else if (Input.GetKeyDown(KeyCode.M) && isFlashing)
        {
            isFlashing = false;
            StopCoroutine(flashCoroutine);
            targetObject.SetActive(true);
        }
    }

    private IEnumerator FlashObject()
    {
        while (isFlashing)
        {
            targetObject.SetActive(!targetObject.activeSelf);
            yield return new WaitForSeconds(flashInterval);
        }
    }
}
