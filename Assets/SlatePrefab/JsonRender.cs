using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using System.IO;
using TMPro;

public class JsonRender : MonoBehaviour
{
    public TMP_Text[] displaySections;

    void Start()
    {
        // Start with empty text displays
        //displayText1.text = "";
        //displayText2.text = "";
    }

    public void DisplayPatientData(JObject jsonObject)
    {
        List<string> formattedLines = new List<string>();

        foreach (var property in jsonObject.Properties())
        {
            if (!string.IsNullOrEmpty(property.Value?.ToString()))
            {
                string fieldName = System.Text.RegularExpressions.Regex.Replace(
                    property.Name, "_", " ").ToLower();
                fieldName = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                    .ToTitleCase(fieldName);

                formattedLines.Add($"{fieldName}: {property.Value}");
            }
        }

        int linesPerSection = Mathf.CeilToInt((float)formattedLines.Count / 8f);

        for (int i = 0; i < 8; i++)
        {
            int start = i * linesPerSection;
            int count = Mathf.Min(linesPerSection, formattedLines.Count - start);

            if (i < displaySections.Length && start < formattedLines.Count)
            {
                displaySections[i].text = string.Join("\n", formattedLines.GetRange(start, count));
            }
        }
    }

    /*
    void CheckForJsonUpdates()
    {
        if (string.IsNullOrEmpty(jsonFileName)) return;

        string jsonPath = Path.Combine(folderPath, jsonFileName);

        if (File.Exists(jsonPath))
        {
            string newJsonText = File.ReadAllText(jsonPath);
            if (newJsonText != lastJsonText)
            {
                lastJsonText = newJsonText;
                DisplayPatientData(newJsonText);
            }
        }
    }

    public void SetJsonFile(string newFileName)
    {
        jsonFileName = newFileName;
        LoadAndDisplayJsonData();
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            CheckForJsonUpdates();
            timeSinceLastUpdate = 0f;
        }
    }
    */
}