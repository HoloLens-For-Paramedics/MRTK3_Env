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

    // GPT-4o Template & Prompt
    // json_template is incomplete, needs revised. works for now.
    private string json_template = @"{""name"":""patient_id"":""date_of_birth"":""primary_address"":""medication_allergies"":""food_allergies"":""other_allergies"":""current_medications"":""medical_conditions"":""emergency_contact"":""emergency_contact_phone"":""symptoms"":}";
    private string prompt = "You are to fill out the following JSON data with the corresponding string input. If data is missing from input, leave the value of that name empty. Do not add information to the JSON that does not exist. Only add what you are certain matches with JSON field. Do not give your answer formatted. Omit newline, tab, or any other formatting. Return your JSON data as a readable string. Make sure to return the complete JSON template, even if data is missing. From input, you may reformat the answer to be more easily readable. Ex: \"I have an allergy to peanuts\" may just be \"peanuts\".";

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

    // StartMonitoring()
    // This function starts the file monitoring process.
    // Once a file is created, the OnNewFileCreated() function is called.
    public void StartMonitoring()
    {
        if (isMonitoring) return; // Prevent multiple starts

        Debug.Log("FILELOGGER: Starting Audio File Monitoring...");

        directoryPath = @"C:\Users\lcald\OneDrive\Documents\GitHub\unity\USEME\AudioFiles_Test"; // TODO: CHANGE THIS DIRECTORY. THIS IS A PLACEHOLDER DIRECTORY
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
        await GPTAudioToJSON(e.FullPath);
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

    // GPTAudioToJSON(string filePath)
    // Parameters: filePath - the path to the audio file to be transcribed.
    // Returns: None
    // This function transcribes the audio file using Azure and sends the text to OpenAI.
    private async System.Threading.Tasks.Task GPTAudioToJSON(string filePath)
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
            Debug.Log("OpenAI Response: " + responseTextContent); // TODO: Remove this, replace with populating text fields
            File.Delete(audioFilePath);  // Delete the processed audio file to preserve memory
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
}