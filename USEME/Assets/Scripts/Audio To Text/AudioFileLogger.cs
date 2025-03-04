/*
  * Copyright (c) 2025 EMT Vision SCU. All rights reserved.
  *
  * This software is the exclusive property of EMT Vision SCU. Unauthorized use,
  * modification, distribution, or reproduction of this software is prohibited
  * without explicit written permission from EMT Vision SCU. This software is
  * provided "as-is", without any express or implied warranties. EMT Vision SCU
  * shall not be liable for any damages arising from the use of this software.
  *
  * Author: Logan Calder | lcalder@scu.edu
*/

using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;


// AudioFileLogger.cs
// This script is used to monitor a directory for new audio files, then process them through Microsoft Azure Speech Services and OpenAI GPT-4o.
//
// Usage: Attach to a GameObject in the scene. Call StartRecording() to begin recording. Call StopRecording() to stop recording.


public class AudioFileLogger : MonoBehaviour
{
    // IMPORTANT: API Keys & Configs (accessed from appsettings.json, not included in repo)
    // AZURE DATA    
    private string azureKey = "";
    private string region = "westus";
    // OPEN AI DATA
    private string openAIKey = "";
    private string openAIURL = "https://api.openai.com/v1/chat/completions";
    // SUPABASE DATA
    private string supabaseUrl = "https://yuwrsuaqhbbfxqlrybgg.supabase.co/rest/v1/PatientData";
    private string supabaseKey = ""; // Replace with your Supabase API key service role key


    // File monitoring variables
    private string directoryPath;
    private FileSystemWatcher fileWatcher;
    private bool isMonitoring = false;
    private string audioFilePath;
    private string patientId = "";

    private string json_template;

    private string timestamp;
    private string prompt = "You are to fill out the following JSON data with the corresponding string input. Do not delete Patient ID. Do not add information to the JSON that does not exist. Only add what you are certain matches with JSON field. Do not give your answer formatted. Omit newline, tab, or any other formatting. Return your JSON data as a readable string. Make sure to return the complete JSON template, even if data is missing. You should also add, to the section Severity, a severity level based on the severity of the patient's condition (Undetermined, Good, Fair, Serious, Critical). From input, you may reformat the answer to be more easily readable. Ex: \"I have an allergy to peanuts\" may just be \"peanuts\".";

    // AppSettings class
    // This class is used to store the API keys and region.
    // It is accessed from appsettings.json, not included in repo.

    [Serializable]
    public class AppSettings
    {
        public string OpenAIApiKey;
        public string AzureSubscriptionKey;
        public string AzureRegion;
    }

    private string QueuedText = null;


    // load config variables from appsettings.json
    void Awake()
    {
        // LoadConfiguration();
        // Call the GeneratePatientId function and store the result
        json_template = $@"{{""PatientID"":""{patientId}"":""PatientName"":""Age"":""Gender"":""HomeAddress"":""City"":""County"":""State"":""ZIPCode"":""WeightKg"":""Race"":""IncidentNumber"":""ServiceRequested"":""OtherAgencies"":""PrimaryRole"":""ResponseMode"":""EMSShift"":""DispatchCity"":""DispatchState"":""DispatchZIP"":""DispatchCounty"":""SceneType"":""Category"":""BackInService"":""CrewMembers"":""NumberOfCrew"":""OtherAgencyOnScene"":""NumberOfPatients"":""PatientContactMade"":""ArrivedOnScene"":""FirstOnScene"":""StagePriorToContact"":""PrimaryComplaint"":""Duration"":""TimeUnits"":""AlcoholDrugUse"":""InitialAcuity"":""CardiacArrest"":""PossibleInjury"":""BaseContactMade"":""SignsOfAbuse"":""5150Hold"":""PastMedicalHistory"":""CurrentMedications"":""MedicationAllergies"":""AdvanceDirectives"":""HeartRate"":""BloodPressure"":""RespiratoryRate"":""SPO2"":""Temperature"":""Glucose"":""GCS_Eye"":""GCS_Verbal"":""GCS_Motor"":""GCS_Score"":""GCS_Qualifier"":""MentalStatus"":""AbdomenExam"":""ChestExam"":""BackSpineExam"":""SkinAssessment"":""EyeExam_Bilateral"":""EyeExam_Left"":""EyeExam_Right"":""LungExam"":""ExtremitiesExam"":""PrimaryImpression"":""PrimarySymptom"":""OtherSymptoms"":""SymptomOnset"":""TypeOfPatient"":""MedTime"":""MedCrewID"":""Medication"":""Dosage"":""MedUnits"":""Route"":""MedResponse"":""MedComplications"":""ProcTime"":""ProcCrewID"":""Procedure"":""ProcLocation"":""IVLocation"":""Size"":""Attempts"":""Successful"":""ProcResponse"":""PatientEvaluationCare"":""CrewDisposition"":""TransportDisposition"":""LevelOfCareProvided"":""TransferredCareAt"":""FinalPatientAcuity"":""TurnaroundDelay"":""TransportAgency"":""TransportUnit"":""LevelOfTransport"":""EMSPrimaryCareProvider"":""TransportReason"":""CrewSignature"":""CrewMember_PPE"":""PPEUsed"":""SuspectedExposure"":""MonitorTime"":""MonitorEventType"":""Time"":""Severity"":}}";
    }

    // LoadConfiguration()
    // This function loads the configuration from appsettings.json. 
    // You must import this yourself as git will ignore it.
    // private void LoadConfiguration()
    // {
    //     string configPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "appsettings.json");
    //     if (File.Exists(configPath))
    //     {
    //         try
    //         {
    //             string jsonContent = File.ReadAllText(configPath);
    //             var config = JsonUtility.FromJson<AppSettings>(jsonContent);

    //             openAIKey = config?.OpenAIApiKey;
    //             region = config?.AzureRegion;
    //             azureKey = config?.AzureSubscriptionKey;
    //         }
    //         catch (Exception ex)
    //         {
    //             Debug.LogError($"Error loading configuration: {ex.Message}");
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogError($"appsettings.json not found at: {configPath}");
    //         Debug.LogError($"Please ensure appsettings.json exists in the project root directory: {Path.GetDirectoryName(configPath)}");
    //     }
    // }

    // GeneratePatientId()
    // This function generates a Patient ID in the format PAT-YYYYMMDD-HHMMSS-XXXX
    private string GeneratePatientId()
    {
        string datePart = DateTime.Now.ToString("yyyyMMdd");
        string timePart = DateTime.Now.ToString("HHmmss");
        string randomPart = Guid.NewGuid().ToString().Substring(0, 4); // Get the first 4 characters of a new GUID
        return $"PAT-{datePart}-{timePart}-{randomPart}";
    }

    private string GenerateTimestamp()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // StartMonitoring()
    // This function starts the file monitoring process.
    // Once a file is created, the OnNewFileCreated() function is called.
    public void StartMonitoring()
    {
        if (isMonitoring) return; // Prevent multiple starts

        Debug.Log("FILELOGGER: Starting Audio File Monitoring...");

        directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        // directoryPath = Path.Combine(Application.dataPath, "Recordings"); // dev use only

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }


        fileWatcher = new FileSystemWatcher(directoryPath, "*.wav");
        fileWatcher.Created += OnNewFileCreated;
        fileWatcher.EnableRaisingEvents = true; // Start monitoring
        isMonitoring = true;
        Debug.Log($"FILELOGGER: Monitoring for new audio files in: {directoryPath}");
    }

    // OnNewFileCreated()
    // This function is called when a new file is created in the directory.
    // It processes the audio file through Azure Speech Services and OpenAI GPT-4o.
    private async void OnNewFileCreated(object sender, FileSystemEventArgs e)
    {
        Debug.Log($"FILELOGGER: New recording saved: {Path.GetFileName(e.FullPath)}");
        Debug.Log("FILELOGGER: Processing audio file...");
        await AzureAudioToJSON(e.FullPath);
    }

    // OnDestroy()
    // This function is called when the script is destroyed.
    // It cleans up the file watcher to prevent memory leaks.
    void OnDestroy()
    {
        if (fileWatcher != null)
        {
            fileWatcher.EnableRaisingEvents = false;
            fileWatcher.Dispose();
        }
    }

    // AzureAudioToJSON(string filePath)
    // Parameters: filePath - the path to the audio file to be transcribed.
    // Returns: None
    // This function transcribes the audio file using Azure and sends the text to OpenAI.
    private async System.Threading.Tasks.Task AzureAudioToJSON(string filePath)
    {
        Debug.Log($"FILELOGGER: Attempting to process file: {filePath}");

        var speechConfig = SpeechConfig.FromSubscription(azureKey, region);
        var audioConfig = AudioConfig.FromWavFileInput(filePath);

        using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
        {
            var result = await recognizer.RecognizeOnceAsync();
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                QueuedText = result.Text;
                audioFilePath = filePath;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                Debug.LogWarning("FILELOGGER: No speech recognized.");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                Debug.LogError($"FILELOGGER: Speech recognition canceled: {cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Debug.LogError($"FILELOGGER: Error Code: {cancellation.ErrorCode}");
                    Debug.LogError($"FILELOGGER: Error Details: {cancellation.ErrorDetails}");
                }
            }
        }
    }

    // Update should process any queued text obtained from Azure.
    // This was, literally, the only way I could get OpenAI to work here. I have 0 clue why
    void Update()
    {
        // Process any queued text
        if (QueuedText != null)
        {
            string textToProcess = QueuedText;
            QueuedText = null;
            StartCoroutine(SendOpenAIRequest(textToProcess));
        }
    }

    // SendOpenAIRequest(string rawText)
    // Parameters: rawText - the text to be sent to OpenAI.
    // Returns: None
    // This function sends the transcribed text to OpenAI and returns the JSON data.
    private IEnumerator SendOpenAIRequest(string rawText)
    {
        Debug.Log($"📡 Sending to OpenAI: {rawText}");
        string promptAndInput = prompt.Replace("\"", "\\\"") +
            " input: " + rawText.Replace("\"", "\\\"") +
            " template: " + json_template.Replace("\"", "\\\"");

        // Do not touch this, is the template for generating the proper response from GPT
        string jsonPayload = "{" +
            "\"model\": \"gpt-4\"," +
            "\"messages\": [{" +
                "\"role\": \"user\"," +
                "\"content\": \"" + promptAndInput + "\"" +
            "}]" +
        "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(openAIURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseTextContent = ExtractMessage(request.downloadHandler.text);
            responseTextContent = responseTextContent.Replace("\"PatientID\":\"\"", $"\"PatientID\":\"{patientId}\"");
            responseTextContent = responseTextContent.Replace("\"Time\":\"\"", $"\"Time\":\"{timestamp}\"");
            File.Delete(audioFilePath);  // Delete the processed audio file to preserve memory
            Debug.Log("Sending JSON to Supabase...");
            StartCoroutine(SendJsonToSupabase(responseTextContent));
            Debug.Log("JSON Sent to Supabase");
        }
        else
        {
            Debug.Log("Error: " + request.error);
        }
    }

    // ExtractMessage(string jsonResponse)
    // Parameters: jsonResponse - the response from OpenAI.
    // Returns: A string of the JSON data.
    // This function extracts the JSON data from the response from OpenAI.
    private string ExtractMessage(string jsonResponse)
    {
        OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(jsonResponse);
        return response.choices[0].message.content;
    }

    [System.Serializable]
    public class OpenAIResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string content;
    }

    public void GeneratePatientIdAndTimestamp()
    {
        patientId = GeneratePatientId();
        timestamp = GenerateTimestamp();
        Debug.Log($"Generated Patient ID: {patientId}");
        Debug.Log($"Generated Timestamp: {timestamp}");
    }

    // SendJsonToSupabase(string jsonData)
    // Parameters: jsonData - the JSON string to be sent to Supabase.
    // Returns: None
    // This function sends the provided JSON data to Supabase.

    private IEnumerator SendJsonToSupabase(string jsonData)
    {
        Debug.Log("Current JSON: " + jsonData);

        // First, fetch existing data for this patient
        string fetchUrl = $"{supabaseUrl}?PatientID=eq.{patientId}";
        UnityWebRequest fetchRequest = UnityWebRequest.Get(fetchUrl);
        fetchRequest.SetRequestHeader("apikey", supabaseKey);
        fetchRequest.SetRequestHeader("Authorization", "Bearer " + supabaseKey);

        yield return fetchRequest.SendWebRequest();

        string mergedJson = jsonData;

        if (fetchRequest.result == UnityWebRequest.Result.Success)
        {
            string existingData = fetchRequest.downloadHandler.text;
            Debug.Log("Fetched existing data: " + existingData);

            // Check if we got any data back (empty array means no existing record)
            if (existingData != null && existingData.Length > 2 && !existingData.Equals("[]"))
            {
                // Remove the array brackets since we expect only one record
                existingData = existingData.Trim().TrimStart('[').TrimEnd(']');

                // Merge the existing data with the new data
                mergedJson = MergeJsonData(existingData, jsonData);
            }
        }
        else
        {
            Debug.LogWarning("Failed to fetch existing data: " + fetchRequest.error);
            // Continue with just the new data if fetch fails
        }

        // Now send the merged data back to Supabase
        byte[] bodyRaw = Encoding.UTF8.GetBytes(mergedJson);

        UnityWebRequest updateRequest = new UnityWebRequest(supabaseUrl, "POST");
        updateRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        updateRequest.downloadHandler = new DownloadHandlerBuffer();
        updateRequest.SetRequestHeader("Content-Type", "application/json");
        updateRequest.SetRequestHeader("apikey", supabaseKey);
        updateRequest.SetRequestHeader("Authorization", "Bearer " + supabaseKey);
        updateRequest.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");

        yield return updateRequest.SendWebRequest();

        if (updateRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Successfully sent merged JSON to Supabase: " + updateRequest.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error sending merged JSON to Supabase: " + updateRequest.error);
            Debug.LogError("Response: " + updateRequest.downloadHandler.text);
        }
    }

    // Helper method to merge JSON data
    private string MergeJsonData(string existingJson, string newJson)
    {
        try
        {
            // Simple JSON parsing approach using string manipulation
            // This avoids dependencies on external JSON libraries

            // Remove the curly braces
            string existingContent = existingJson.Trim().TrimStart('{').TrimEnd('}');
            string newContent = newJson.Trim().TrimStart('{').TrimEnd('}');

            // Split by key-value pairs
            Dictionary<string, string> existingPairs = new Dictionary<string, string>();
            Dictionary<string, string> newPairs = new Dictionary<string, string>();

            // Parse existing JSON
            string[] existingItems = SplitJsonIntoPairs(existingContent);
            foreach (string item in existingItems)
            {
                if (string.IsNullOrEmpty(item.Trim())) continue;

                int colonIndex = item.IndexOf(':');
                if (colonIndex > 0)
                {
                    string key = item.Substring(0, colonIndex).Trim().Trim('"');
                    string value = item.Substring(colonIndex + 1).Trim().Trim('"');
                    existingPairs[key] = value;
                }
            }

            // Parse new JSON
            string[] newItems = SplitJsonIntoPairs(newContent);
            foreach (string item in newItems)
            {
                if (string.IsNullOrEmpty(item.Trim())) continue;

                int colonIndex = item.IndexOf(':');
                if (colonIndex > 0)
                {
                    string key = item.Substring(0, colonIndex).Trim().Trim('"');
                    string value = item.Substring(colonIndex + 1).Trim().Trim('"');
                    newPairs[key] = value;
                }
            }

            // Merge the dictionaries
            Dictionary<string, string> mergedPairs = new Dictionary<string, string>(existingPairs);

            foreach (var pair in newPairs)
            {
                string key = pair.Key;
                string newValue = pair.Value;

                // Skip empty values in new data
                if (string.IsNullOrEmpty(newValue)) continue;

                // If the key exists and has a value, combine old and new
                if (existingPairs.ContainsKey(key) && !string.IsNullOrEmpty(existingPairs[key]))
                {
                    string existingValue = existingPairs[key];

                    // Don't duplicate if values are the same
                    if (existingValue != newValue)
                    {
                        mergedPairs[key] = $"{existingValue}, {newValue}";
                    }
                    // else keep existing value
                }
                else
                {
                    // Key doesn't exist or has empty value, just use new value
                    mergedPairs[key] = newValue;
                }
            }

            // Build the merged JSON
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            bool first = true;
            foreach (var pair in mergedPairs)
            {
                if (!first) sb.Append(',');
                sb.Append($"\"{pair.Key}\":\"{pair.Value}\"");
                first = false;
            }

            sb.Append('}');
            return sb.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error merging JSON data: {ex.Message}");
            // Return the new JSON if merging fails
            return newJson;
        }
    }

    // Helper method to split JSON string into key-value pairs
    private string[] SplitJsonIntoPairs(string jsonContent)
    {
        List<string> pairs = new List<string>();
        int startIndex = 0;
        bool inQuotes = false;

        for (int i = 0; i < jsonContent.Length; i++)
        {
            char c = jsonContent[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                pairs.Add(jsonContent.Substring(startIndex, i - startIndex));
                startIndex = i + 1;
            }
        }

        // Add the last pair
        if (startIndex < jsonContent.Length)
        {
            pairs.Add(jsonContent.Substring(startIndex));
        }

        return pairs.ToArray();
    }
}