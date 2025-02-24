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
  * Co-Author: Jack Landers | jlanders@scu.edu
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


// AudioFileLogger.cs
// This script is used to monitor a directory for new audio files, then process them through Microsoft Azure Speech Services and OpenAI GPT-4o.
//
// Usage: Attach to a GameObject in the scene. Call StartRecording() to begin recording. Call StopRecording() to stop recording.


public class AudioFileLogger : MonoBehaviour
{
    // IMPORTANT: API Keys & Configs (accessed from appsettings.json, not included in repo)
    private string azureKey;
    private string openAIKey;
    private string openAIURL = "https://api.openai.com/v1/chat/completions";
    private string region = "westus";

    // File monitoring variables
    private string directoryPath;
    private FileSystemWatcher fileWatcher;
    private bool isMonitoring = false;
    private string audioFilePath;
    private string patientId = "";

    private string json_template;
    private string current_json;
    private string timestamp;
    private string prompt = "You are to fill out the following JSON data with the corresponding string input. Do not overwrite existing data, rather add to it if something already exists. For example, if there is a pre-existing allergy, and you learn of a new one, add to the lits of allergies, but do not delete the already known one. If you have no information for a field, rather that be in the provided template or the context passed, leave it empty. Do not delete Patient ID. Do not add information to the JSON that does not exist. Only add what you are certain matches with JSON field. Do not give your answer formatted. Omit newline, tab, or any other formatting. Return your JSON data as a readable string. Make sure to return the complete JSON template, even if data is missing. From input, you may reformat the answer to be more easily readable. Ex: \"I have an allergy to peanuts\" may just be \"peanuts\".";

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
        LoadConfiguration();
        json_template = $@"{{""PatientID"":""{patientId}"":""PatientName"":""Age"":""Gender"":""HomeAddress"":""City"":""County"":""State"":""ZIPCode"":""WeightKg"":""Race"":""IncidentNumber"":""ServiceRequested"":""OtherAgencies"":""PrimaryRole"":""ResponseMode"":""EMSShift"":""DispatchCity"":""DispatchState"":""DispatchZIP"":""DispatchCounty"":""SceneType"":""Category"":""BackInService"":""CrewMembers"":""NumberOfCrew"":""OtherAgencyOnScene"":""NumberOfPatients"":""PatientContactMade"":""ArrivedOnScene"":""FirstOnScene"":""StagePriorToContact"":""PrimaryComplaint"":""Duration"":""TimeUnits"":""AlcoholDrugUse"":""InitialAcuity"":""CardiacArrest"":""PossibleInjury"":""BaseContactMade"":""SignsOfAbuse"":""5150Hold"":""PastMedicalHistory"":""CurrentMedications"":""MedicationAllergies"":""AdvanceDirectives"":""HeartRate"":""BloodPressure"":""RespiratoryRate"":""SPO2"":""Temperature"":""Glucose"":""GCS_Eye"":""GCS_Verbal"":""GCS_Motor"":""GCS_Score"":""GCS_Qualifier"":""MentalStatus"":""AbdomenExam"":""ChestExam"":""BackSpineExam"":""SkinAssessment"":""EyeExam_Bilateral"":""EyeExam_Left"":""EyeExam_Right"":""LungExam"":""ExtremitiesExam"":""PrimaryImpression"":""PrimarySymptom"":""OtherSymptoms"":""SymptomOnset"":""TypeOfPatient"":""MedTime"":""MedCrewID"":""Medication"":""Dosage"":""MedUnits"":""Route"":""MedResponse"":""MedComplications"":""ProcTime"":""ProcCrewID"":""Procedure"":""ProcLocation"":""IVLocation"":""Size"":""Attempts"":""Successful"":""ProcResponse"":""PatientEvaluationCare"":""CrewDisposition"":""TransportDisposition"":""LevelOfCareProvided"":""TransferredCareAt"":""FinalPatientAcuity"":""TurnaroundDelay"":""TransportAgency"":""TransportUnit"":""LevelOfTransport"":""EMSPrimaryCareProvider"":""TransportReason"":""CrewSignature"":""CrewMember_PPE"":""PPEUsed"":""SuspectedExposure"":""MonitorTime"":""MonitorEventType"":""Time"":}}";
        current_json = json_template;
    }

    // LoadConfiguration()
    // This function loads the configuration from appsettings.json. 
    // You must import this yourself as git will ignore it.
    private void LoadConfiguration()
    {
        string configPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "appsettings.json");
        if (File.Exists(configPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(configPath);
                var config = JsonUtility.FromJson<AppSettings>(jsonContent);

                openAIKey = config?.OpenAIApiKey;
                region = config?.AzureRegion;
                azureKey = config?.AzureSubscriptionKey;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading configuration: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError($"appsettings.json not found at: {configPath}");
            Debug.LogError($"Please ensure appsettings.json exists in the project root directory: {Path.GetDirectoryName(configPath)}");
        }
    }

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
        // Call the GeneratePatientId function and store the result
        patientId = GeneratePatientId();
        timestamp = GenerateTimestamp();
        Debug.Log($"Generated Patient ID: {patientId}");
        Debug.Log($"Generated Timestamp: {timestamp}");

        if (isMonitoring) return; // Prevent multiple starts

        Debug.Log("FILELOGGER: Starting Audio File Monitoring...");

        directoryPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "AudioFiles_Test");
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
            " template: " + current_json.Replace("\"", "\\\"");

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
            current_json = responseTextContent;
            current_json = current_json.Replace("\"PatientID\":\"\"", $"\"PatientID\":\"{patientId}\"");
            current_json = current_json.Replace("\"Time\":\"\"", $"\"Time\":\"{timestamp}\"");
            File.Delete(audioFilePath);  // Delete the processed audio file to preserve memory
            Debug.Log("Sending JSON to Supabase...");
            StartCoroutine(SendJsonToSupabase(current_json));
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

    // SendJsonToSupabase(string jsonData)
    // Parameters: jsonData - the JSON string to be sent to Supabase.
    // Returns: None
    // This function sends the provided JSON data to Supabase.

    private IEnumerator SendJsonToSupabase(string jsonData)
    {
        Debug.Log("Current JSON: " + jsonData);
        string supabaseUrl = "https://yuwrsuaqhbbfxqlrybgg.supabase.co/rest/v1/PatientData";
        string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Inl1d3JzdWFxaGJiZnhxbHJ5YmdnIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0MDA3NTk0NywiZXhwIjoyMDU1NjUxOTQ3fQ.oDOmFPwxbq9FosgsJb4YPs3xwVTPdNL4ihNlw3oZwTk"; // Replace with your Supabase API key service role key

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(supabaseUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", supabaseKey); // Include the apikey header
        request.SetRequestHeader("Authorization", "Bearer " + supabaseKey);
        // The Prefer header instructs Supabase to merge duplicates and return the updated/created record.
        request.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Successfully sent JSON to Supabase: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error sending JSON to Supabase: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }
}