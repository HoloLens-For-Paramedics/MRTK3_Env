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

    // ---------------------------- PASTE KEYS HERE ---------------------------------

    // File monitoring variables
    private string conversation = "";
    private SpeechRecognizer recognizer;
    private AudioConfig audioConfig;
    private SpeechConfig speechConfig;

    private string directoryPath;
    private FileSystemWatcher fileWatcher;
    private string patientId = "";

    private string json_template;
    private string current_data = "";

    private string timestamp;
    private string QueuedText = null;
    string gptPrompt = @"
        You are GPT-4o mini, an advanced AI agent responsible for transcribing and recording confidential patient information into a structured JSON format. Your primary task is to extract, filter, and organize relevant information from the provided input while preserving existing data that remains relevant. Your output must be a fully formatted JSON string, ensuring accuracy, consistency, and adherence to predefined constraints. Follow the instructions below with utmost precision:

**General Instructions:**
1. **Maintain Data Integrity:** Do not delete existing data unless new information explicitly overrides it or renders it invalid.
2. **Do Not Fabricate Data:** If a field has no information, leave it blank but do not create speculative or incorrect data.
3. **Retain Identifying Information:** Fields such as `PatientID`, `PatientName`, `Age`, `WeightKg`, and `ZIPCode` should remain singular values and must not be converted into lists.
4. **Append Multiple Entries Where Appropriate:** If multiple values apply to a field (e.g., medications, symptoms, or past medical history), store them as a list while ensuring they are relevant and distinct.
5. **Reformat for Clarity:** Simplify and standardize input when possible. For example, `Patient has a history of severe hypertension` should be recorded as `hypertension.`
6. **Follow Structured Field Assignments:** Ensure every piece of data is allocated to its correct field and is not misplaced.
7. **MOST IMPORTANT:** ONLY RETURN A JSON OBJECT. DO NOT RETURN ANYTHING ELSE. DO NOT ENCASE IN ```JSON``` TAGS. RETURN IN A VALID JSON FORMAT. DO NOT ADD COMMENTS. IF JSON IS INVALID, IT WILL FAIL.

---

**Input Types:**
1. **Database Record (Existing Data):**
   - You will receive a JSON object `current_data` containing previously recorded patient information. Use this as the baseline data.
2. **Recent Audio Transcription (40s Context):**
   - This contains the latest spoken details from an emergency medical responder, nurse, or doctor.
3. **Additional Context:**
   - Information regarding the circumstances under which the data is recorded (e.g., emergency setting, follow-up assessment).

Additionally, you are provided with `json_template`, which contains the empty JSON structure that must be populated.

---

**Field Processing Rules:**
**1. Patient Identification & Demographics**
- **`PatientID`** (Mandatory, Unique) → **Do not modify or delete.**
- **`PatientName`** (String) → Retain unless an explicit correction is provided.
- **`Age`** (Integer/String) → Ensure only one value is present.
- **`Gender`** (M/F) → Extract from input if mentioned; use `M` for male and `F` for female.
- **`HomeAddress`** → Only update if a full address is provided.
- **`City, County, State, ZIPCode`** → Populate if new, but do not override unless certain.
- **`Race`** → Retain or update only if explicitly stated.

**2. Incident Details**
- **`IncidentNumber`** → Must remain singular.
- **`ServiceRequested`** → Include service type (e.g., BLS, ALS, transport).
- **`PrimaryRole`** → Define based on responder`s role (e.g., paramedic, firefighter, nurse).
- **`ResponseMode`** → Extract from responder dialogue (e.g., `Code 3` or `Non-emergent`).

**3. Scene & Patient Interaction**
- **`SceneType`** → Capture relevant setting (e.g., `residential`, `public street`).
- **`Category`** → Medical, trauma, behavioral, etc.
- **`CrewMembers`** → List all names or IDs if provided.
- **`NumberOfCrew`** → Integer, representing responding crew.
- **`PatientContactMade`** → Boolean (true if contact established).

**4. Clinical Observations & Symptoms**
- **`PrimaryComplaint`** → Capture the main reason for the call.
- **`OtherSymptoms`** → Extract all additional symptoms.
- **`AlcoholDrugUse`** → Mention only if stated.
- **`InitialAcuity`** → Determine severity (e.g., minor, severe).
- **`CardiacArrest`** → Boolean (true if present).
- **`PossibleInjury`** → Boolean (true if reported).
- **`SignsOfAbuse`** → Boolean (true if noted).

**5. Medical History & Medications**
- **`PastMedicalHistory`** → Convert spoken history into a concise list.
- **`CurrentMedications`** → Extract and list.
- **`MedicationAllergies`** → Ensure clarity (e.g., `penicillin` instead of `I can`t take that one antibiotic`).

**6. Vital Signs**
- **Heart Rate, Blood Pressure, Respiratory Rate, SPO2, Temperature, Glucose** → Numeric values only.
- **GCS Score & Breakdown** → Ensure accurate parsing of Eye, Verbal, and Motor scores.

**7. Assessment & Impressions**
- **`PrimaryImpression`** → The clinician`s primary diagnosis.
- **`PrimarySymptom`** → The main reported symptom.
- **`OtherSymptoms`** → List any additional complaints.

**8. Treatment & Procedures**
- **`Medication`, `Dosage`, `Route`** → Extract administered drugs and details.
- **`Procedure`** → Include any procedures performed.
- **`IVLocation, Size, Attempts, Successful`** → Record all IV details.

**9. Transport & Disposition**
- **`CrewDisposition`** → Capture decision made by the crew (e.g., treated and released, transported).
- **`TransportDisposition`** → Specify transport details.
- **`LevelOfCareProvided`** → Define level (BLS, ALS, etc.).
- **`TransportReason`** → Capture the reason for transport.
- **`TransportAgency, TransportUnit`** → Include agency and vehicle ID.

**10. Severity Determination**
- **`Severity`** → Assign one of the following based on patient condition:
  - `Undetermined`
  - `Good`
  - `Fair`
  - `Serious`
  - `Critical`

---

**Final Output Requirements:**
- **Complete JSON Object:** Ensure every field is present, even if empty.
- **Flat Structure:** No nested structures unless explicitly needed.
- **No Additional Formatting:** Output must be a valid JSON string without extra spaces or newlines.
- **Preserve All Data:** Retain prior records unless explicitly replaced by new input.

Your task is to process patient data while maintaining compliance with these rules. Follow these instructions meticulously to ensure high data fidelity and accuracy.";

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


    private async void OnDestroy()
    {
        if (recognizer != null)
        {
            await recognizer.StopContinuousRecognitionAsync();
            recognizer.Dispose();
        }
    }

    public async void StartRecording(string patientId = null)
    {
        json_template = $@"{{""PatientID"":""{patientId}"":""PatientName"":""Age"":""Gender"":""HomeAddress"":""City"":""County"":""State"":""ZIPCode"":""WeightKg"":""Race"":""IncidentNumber"":""ServiceRequested"":""OtherAgencies"":""PrimaryRole"":""ResponseMode"":""EMSShift"":""DispatchCity"":""DispatchState"":""DispatchZIP"":""DispatchCounty"":""SceneType"":""Category"":""BackInService"":""CrewMembers"":""NumberOfCrew"":""OtherAgencyOnScene"":""NumberOfPatients"":""PatientContactMade"":""ArrivedOnScene"":""FirstOnScene"":""StagePriorToContact"":""PrimaryComplaint"":""Duration"":""TimeUnits"":""AlcoholDrugUse"":""InitialAcuity"":""CardiacArrest"":""PossibleInjury"":""BaseContactMade"":""SignsOfAbuse"":""5150Hold"":""PastMedicalHistory"":""CurrentMedications"":""MedicationAllergies"":""AdvanceDirectives"":""HeartRate"":""BloodPressure"":""RespiratoryRate"":""SPO2"":""Temperature"":""Glucose"":""GCS_Eye"":""GCS_Verbal"":""GCS_Motor"":""GCS_Score"":""GCS_Qualifier"":""MentalStatus"":""AbdomenExam"":""ChestExam"":""BackSpineExam"":""SkinAssessment"":""EyeExam_Bilateral"":""EyeExam_Left"":""EyeExam_Right"":""LungExam"":""ExtremitiesExam"":""PrimaryImpression"":""PrimarySymptom"":""OtherSymptoms"":""SymptomOnset"":""TypeOfPatient"":""MedTime"":""MedCrewID"":""Medication"":""Dosage"":""MedUnits"":""Route"":""MedResponse"":""MedComplications"":""ProcTime"":""ProcCrewID"":""Procedure"":""ProcLocation"":""IVLocation"":""Size"":""Attempts"":""Successful"":""ProcResponse"":""PatientEvaluationCare"":""CrewDisposition"":""TransportDisposition"":""LevelOfCareProvided"":""TransferredCareAt"":""FinalPatientAcuity"":""TurnaroundDelay"":""TransportAgency"":""TransportUnit"":""LevelOfTransport"":""EMSPrimaryCareProvider"":""TransportReason"":""CrewSignature"":""CrewMember_PPE"":""PPEUsed"":""SuspectedExposure"":""MonitorTime"":""MonitorEventType"":""Time"":""Severity"":}}";
        GeneratePatientIdAndTimestamp();
        conversation = ""; // Reset conversation

        // Initialize speech recognition if not already initialized
        if (recognizer == null)
        {
            await InitializeSpeechRecognition();
        }

        await recognizer.StartContinuousRecognitionAsync();
        Debug.Log("Started recording...");
    }
    private async Task InitializeSpeechRecognition()
    {

        speechConfig = SpeechConfig.FromSubscription(azureKey, region);
        speechConfig.SpeechRecognitionLanguage = "en-US"; // Change as needed

        // Use the default microphone
        audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        // Subscribe to recognition events
        recognizer.Recognizing += (s, e) =>
        {
            Debug.Log($"Recognizing: {e.Result.Text}");

        };

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Debug.Log($"Final Result: {e.Result.Text}");
                conversation += " " + e.Result.Text;
                Debug.Log($"Conversation: {conversation}");
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Debug.Log("No speech recognized.");
            }

        };

        recognizer.Canceled += (s, e) =>
        {
            Debug.LogError($"Canceled: {e.Reason}, Error: {e.ErrorDetails}");
        };

        recognizer.SessionStopped += (s, e) =>
        {
            Debug.Log("Speech session stopped.");
        };

        Debug.Log("Speech recognition initialized...");
    }

    public async void StopRecording()
    {
        await recognizer.StopContinuousRecognitionAsync();
        Debug.Log("Stopped recording. Final conversation:");
        Debug.Log(conversation);
        QueuedText = conversation;
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

    public void GeneratePatientIdAndTimestamp()
    {
        patientId = GeneratePatientId();
        timestamp = GenerateTimestamp();
        Debug.Log($"Generated Patient ID: {patientId}");
        Debug.Log($"Generated Timestamp: {timestamp}");
    }

    // When we detect a new recording, we need to process the text.
    void Update()
    {
        // Process any queued text
        if (QueuedText != null)
        {
            string textToProcess = QueuedText;
            QueuedText = null;
            StartCoroutine(FetchCurrentDataAndProcessText(textToProcess));
        }
    }

    // New method to fetch current data before processing text
    private IEnumerator FetchCurrentDataAndProcessText(string textToProcess)
    {
        // Fetch current data for this patient ID
        string fetchUrl = $"{supabaseUrl}?PatientID=eq.{patientId}";
        UnityWebRequest fetchRequest = UnityWebRequest.Get(fetchUrl);
        fetchRequest.SetRequestHeader("apikey", supabaseKey);
        fetchRequest.SetRequestHeader("Authorization", "Bearer " + supabaseKey);

        yield return fetchRequest.SendWebRequest();

        if (fetchRequest.result == UnityWebRequest.Result.Success)
        {
            string response = fetchRequest.downloadHandler.text;
            // Check if we got any data back (empty array means no existing record)
            if (response != null && response.Length > 2 && !response.Equals("[]"))
            {
                // Remove the array brackets since we expect only one record
                current_data = response.Trim().TrimStart('[').TrimEnd(']');
                Debug.Log("Fetched current data: " + current_data);
            }
            else
            {
                current_data = "{}"; // Set to empty JSON object if no data found
                Debug.Log("No existing data found for patient ID: " + patientId);
            }
        }
        else
        {
            Debug.LogWarning("Failed to fetch existing data: " + fetchRequest.error);
            current_data = "{}"; // Set to empty JSON object if fetch fails
        }

        // Now process the text with the updated current_data
        StartCoroutine(SendOpenAIRequest(textToProcess));
    }

    // SendOpenAIRequest(string rawText)
    // Parameters: rawText - the text to be sent to OpenAI.
    // Returns: None
    // This function sends the transcribed text to OpenAI and returns the JSON data.
    private IEnumerator SendOpenAIRequest(string rawText)
    {
        Debug.Log($"📡 Sending to OpenAI: {rawText}");
        Debug.Log($"Current data being used: {current_data}");

        // Properly escape strings for JSON
        string escapedSystemPrompt = EscapeJsonString(gptPrompt);
        string escapedUserContent = EscapeJsonString($"Audio input: {rawText}\nEmpty template: {json_template}\nCurrent db info: {current_data}\nTimestamp: {timestamp}\nPatient ID: {patientId}");

        // Construct JSON payload with properly escaped strings
        string jsonPayload = @"{
            ""model"": ""gpt-4o"",
            ""messages"": [
                {
                    ""role"": ""system"",
                    ""content"": """ + escapedSystemPrompt + @"""
                },
                {
                    ""role"": ""user"",
                    ""content"": """ + escapedUserContent + @"""
                }
            ]
        }";

        Debug.Log($"JSON payload size: {jsonPayload.Length} bytes");
        Debug.Log($"JSON Payload preview: {(jsonPayload.Length > 200 ? jsonPayload.Substring(0, 200) + "..." : jsonPayload)}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        Debug.Log($"Request body size: {bodyRaw.Length} bytes");

        UnityWebRequest request = new UnityWebRequest(openAIURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIKey);

        // Log request details before sending
        Debug.Log($"Sending request to URL: {openAIURL}");
        Debug.Log($"Using API key: {openAIKey.Substring(0, 10)}..."); // Only show first 10 chars for security

        yield return request.SendWebRequest();

        // Log detailed response information
        Debug.Log($"Response code: {request.responseCode}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("OpenAI request successful!");
            string responseText = request.downloadHandler.text;
            Debug.Log($"Response length: {responseText.Length} bytes");
            Debug.Log($"Response preview: {(responseText.Length > 100 ? responseText.Substring(0, 100) + "..." : responseText)}");

            string responseTextContent = ExtractMessage(request.downloadHandler.text);
            responseTextContent = responseTextContent.Replace("\"PatientID\":\"\"", $"\"PatientID\":\"{patientId}\"");
            responseTextContent = responseTextContent.Replace("\"Time\":\"\"", $"\"Time\":\"{timestamp}\"");
            // File.Delete(audioFilePath);  // Delete the processed audio file to preserve memory

            StartCoroutine(SendJsonToSupabase(responseTextContent));

        }
        else
        {
            Debug.LogError($"OpenAI request failed with error: {request.error}");
            Debug.LogError($"Error details: {request.downloadHandler?.text ?? "No response body"}");

            // Check for common error causes
            if (request.responseCode == 401)
            {
                Debug.LogError("Authentication error: Check if your OpenAI API key is valid");
            }
            else if (request.responseCode == 400)
            {
                Debug.LogError("Bad request: The request format might be incorrect or the prompt might be too long");
            }
            else if (request.responseCode == 429)
            {
                Debug.LogError("Rate limit exceeded: You might be sending too many requests or have exceeded your quota");
            }
            else if (request.responseCode == 500)
            {
                Debug.LogError("Server error: OpenAI's servers might be experiencing issues");
            }

            // Try to log the first part of the payload for debugging
            if (jsonPayload.Length > 500)
            {
                Debug.LogError($"First 500 chars of payload: {jsonPayload.Substring(0, 500)}...");
            }
        }
    }

    // Helper method to properly escape strings for JSON
    private string EscapeJsonString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;

        // Replace special characters with escape sequences
        str = str.Replace("\\", "\\\\");
        str = str.Replace("\"", "\\\"");
        str = str.Replace("\n", "\\n");
        str = str.Replace("\r", "\\r");
        str = str.Replace("\t", "\\t");
        str = str.Replace("\b", "\\b");
        str = str.Replace("\f", "\\f");

        return str;
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

        // First, fetch existing data for this patient
        string fetchUrl = $"{supabaseUrl}?PatientID=eq.{patientId}";
        UnityWebRequest fetchRequest = UnityWebRequest.Get(fetchUrl);
        fetchRequest.SetRequestHeader("apikey", supabaseKey);
        fetchRequest.SetRequestHeader("Authorization", "Bearer " + supabaseKey);

        yield return fetchRequest.SendWebRequest();

        bool rowExists = false;

        Debug.Log("Sending JSON to Supabase...");
        Debug.Log("Final JSON to send: " + jsonData);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // If row exists, use PATCH to update it, otherwise use POST to create a new row
        string requestMethod = rowExists ? "PATCH" : "POST";
        string requestUrl = rowExists ? $"{supabaseUrl}?PatientID=eq.{patientId}" : supabaseUrl;

        Debug.Log($"Using {requestMethod} request to {requestUrl}");

        UnityWebRequest updateRequest = new UnityWebRequest(requestUrl, requestMethod);
        updateRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        updateRequest.downloadHandler = new DownloadHandlerBuffer();
        updateRequest.SetRequestHeader("Content-Type", "application/json");
        updateRequest.SetRequestHeader("apikey", supabaseKey);
        updateRequest.SetRequestHeader("Authorization", "Bearer " + supabaseKey);

        // For both POST and PATCH, we want to return the representation
        updateRequest.SetRequestHeader("Prefer", "return=representation");

        // For POST specifically, we want to handle duplicates by merging
        if (requestMethod == "POST")
        {
            updateRequest.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");
        }

        yield return updateRequest.SendWebRequest();

        if (updateRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Successfully {(rowExists ? "updated" : "created")} record in Supabase: " + updateRequest.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"Error {(rowExists ? "updating" : "creating")} record in Supabase: " + updateRequest.error);
            Debug.LogError("Response: " + updateRequest.downloadHandler.text);

            // If PATCH fails, try POST as a fallback
            if (requestMethod == "PATCH")
            {
                Debug.Log("PATCH failed, trying POST as fallback...");
                yield return StartCoroutine(FallbackPostToSupabase(jsonData));
            }
        }
    }

    // Fallback method to use POST if PATCH fails
    private IEnumerator FallbackPostToSupabase(string jsonData)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(supabaseUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", supabaseKey);
        request.SetRequestHeader("Authorization", "Bearer " + supabaseKey);
        request.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Fallback POST successful: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Fallback POST also failed: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }
}