using UnityEngine;
using TMPro;

public class TextToggle : MonoBehaviour
{
    public TMP_Text[] displayTexts; // Assign all 8 text objects in the Inspector
    private int currentIndex = 0;
    private float defaultFontSize = 35f;

    void Start()
    {
        // Make sure only the first is visible at start
        SetActiveText(currentIndex);
    }

    // Activate text at currentIndex, deactivate all others
    private void SetActiveText(int index)
    {
        for (int i = 0; i < displayTexts.Length; i++)
        {
            if (displayTexts[i] != null)
            {
                displayTexts[i].gameObject.SetActive(i == index);
                if (i == index)
                {
                    displayTexts[i].fontSize = defaultFontSize;
                }
            }
        }
    }

    // Go to the next text display (looping)
    public void ShowNextText()
    {
        currentIndex = (currentIndex + 1) % displayTexts.Length;
        SetActiveText(currentIndex);
    }

    // Go to the previous text display (looping)
    public void ShowPreviousText()
    {
        currentIndex = (currentIndex - 1 + displayTexts.Length) % displayTexts.Length;
        SetActiveText(currentIndex);
    }
}
