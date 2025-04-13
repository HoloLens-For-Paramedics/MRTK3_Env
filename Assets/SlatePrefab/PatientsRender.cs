using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using System.Linq;
using MixedReality.Toolkit.UX;

public class PatientsRender : MonoBehaviour
{
    public GameObject[] buttons;
    private JArray jsonArray;

    public JsonRender jsonRender;

    public void DisplayPatients(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("DisplayPatients: jsonString is null or empty.");
            return;
        }

        jsonArray = JArray.Parse(jsonString);

        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogError("DisplayPatients: buttons array is null or empty.");
            return;
        }

        int loopCount = Mathf.Min(buttons.Length, jsonArray.Count); // Ensure we don't exceed the smaller size

        for (int i = 0; i < loopCount; i++)
        {
            if (buttons[i] == null)
            {
                Debug.LogError($"DisplayPatients: Button GameObject at index {i} is null.");
                continue;
            }

            var pressableButton = buttons[i].GetComponent<PressableButton>();
            if (pressableButton == null)
            {
                Debug.LogError($"DisplayPatients: MRTK PressableButton component not found on GameObject '{buttons[i].name}' at index {i}.");
                continue;
            }

            int capturedIndex = i;
            JObject patient = (JObject)jsonArray[capturedIndex];

            // Update button text with patient name
            JToken nameToken;
            if (patient.TryGetValue("PatientName", out nameToken))
            {
                string patientName = nameToken.ToString();

                TMP_Text tmpText = buttons[i].GetComponentInChildren<TMP_Text>();
                if (tmpText != null)
                {
                    tmpText.text = patientName;
                }
                else
                {
                    Debug.LogWarning($"DisplayPatients: No TMP_Text found in button '{buttons[i].name}' at index {i}.");
                }
            }
            else
            {
                Debug.LogWarning($"DisplayPatients: 'PatientName' not found in JSON at index {i}.");
            }

            // Set up click event
            pressableButton.OnClicked.RemoveAllListeners();
            pressableButton.OnClicked.AddListener(() => HandlePatientClick(patient));
        }

    }

    void HandlePatientClick(JObject patient)
    {
        Debug.Log($"Patient clicked: {patient["name"]}");
        jsonRender.DisplayPatientData(patient);
    }
}
