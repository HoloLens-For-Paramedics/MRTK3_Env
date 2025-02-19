using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using System.IO;
using TMPro;

public class JsonRender : MonoBehaviour
{
    public string jsonFileName = "PatientData.json"; // The name of the JSON file in StreamingAssets
    public TMP_Text displayText;
    //public GameObject panel;      // Reference to the Panel UI (the window)

    //private bool isPanelVisible = false; // Track panel visibility state
    private string lastJsonText = "";    // To store the last JSON content
    private float updateInterval = 5f;   // Interval for checking updates
    private float timeSinceLastUpdate = 0f; // Time passed since the last update check

    // Start is called before the first frame update
    void Start()
    {
        // Panel with info is hidden by default
        //panel.SetActive(true);

        // Display JSON data initially
        LoadAndDisplayJsonData();
    }

    // Method to toggle the visibility of the panel
    public void TogglePanel()
    {
        //isPanelVisible = !isPanelVisible;
        //panel.SetActive(isPanelVisible);
    }

    void DisplayPatientData(string jsonString)
    {
        // Parse the JSON as a single object
        JObject jsonObject = JObject.Parse(jsonString);

        // Initialize an empty string to hold the formatted text
        string formattedText = "";

        // Loop through each property in the JSON object
        foreach (var property in jsonObject.Properties())
        {
            // Check if the value is not null or empty
            if (!string.IsNullOrEmpty(property.Value?.ToString()))
            {
                // Convert the key from snake_case to Title Case with spaces
                string fieldName = System.Text.RegularExpressions.Regex.Replace(
                    property.Name, "_", " ").ToLower();
                fieldName = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                    .ToTitleCase(fieldName);

                // Add the formatted field to the text
                formattedText += $"{fieldName}: {property.Value}\n";
            }
        }

        // Set text in the UI to the formatted text
        displayText.text = formattedText;
    }

    void LoadAndDisplayJsonData()
    {
        // Path to JSON file in StreamingAssets
        string jsonPath = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        // Check if the file exists
        if (File.Exists(jsonPath))
        {
            // Read and display JSON data
            string jsonText = File.ReadAllText(jsonPath); // Read JSON into a string
            lastJsonText = jsonText; // Set as last read content
            DisplayPatientData(jsonText);
        }
        else
        {
            Debug.LogError($"File not found: {jsonPath}");
        }
    }

    void CheckForJsonUpdates()
    {
        string jsonPath = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        if (File.Exists(jsonPath))
        {
            string newJsonText = File.ReadAllText(jsonPath); // Read JSON into a string

            // Update display if JSON content has changed
            if (newJsonText != lastJsonText)
            {
                lastJsonText = newJsonText;
                DisplayPatientData(newJsonText);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Press 'T' to toggle the panel
        if (Input.GetKeyDown(KeyCode.R))
        {
            TogglePanel();
        }

        // Check if the interval has passed
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            CheckForJsonUpdates();
            timeSinceLastUpdate = 0f; // Reset the timer
        }
    }
}