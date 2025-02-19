using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class ClearJsonValues : MonoBehaviour
{
    public string filePath = "Assets/StreamingAssets/patient_data.json"; // Update the path if needed

    public void ClearValues()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (data != null)
            {
                foreach (var key in new List<string>(data.Keys))
                {
                    data[key] = "";
                }

                string updatedJson = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, updatedJson);
                Debug.Log("JSON values cleared.");
            }
        }
        else
        {
            Debug.LogError("JSON file not found!");
        }
    }
}