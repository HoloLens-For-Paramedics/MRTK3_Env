using System;
using System.IO;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class TranscriptionAgent : MonoBehaviour
{
    private string azureKey;
    private string openAIKey;
    private string openAIURL = "https://api.openai.com/v1/chat/completions";
    private string region = "westus";
    private string audioFilePath;

    private string json_template = @"{""name"":""patient_id"":""date_of_birth"":""primary_address"":""medication_allergies"":""food_allergies"":""other_allergies"":""current_medications"":""medical_conditions"":""emergency_contact"":""emergency_contact_phone"":""symptoms"":}";
    private string prompt = "You are to fill out the following JSON data with the corresponding string input. If data is missing from input, leave the value of that name empty. Do not add information to the JSON that does not exist. Only add what you are certain matches with JSON field. Do not give your answer formatted. Omit newline, tab, or any other formatting. Return your JSON data as a readable string. Make sure to return the complete JSON template, even if data is missing. From input, you may reformat the answer to be more easily readable. Ex: \"I have an allergy to peanuts\" may just be \"peanuts\".";


    [Serializable]
    public class AppSettings
    {
        public string OpenAIApiKey;
        public string AzureSubscriptionKey;
        public string AzureRegion;
    }

    void Awake()
    {
        LoadConfiguration();
    }

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

    async void Start()
    {
        if (!File.Exists(audioFilePath))
        {
            Debug.LogError("Audio file not found: " + audioFilePath);
            return;
        }

        Debug.Log("Starting transcription...");
        await AzureAudioToJSON(audioFilePath);
    }

    private async System.Threading.Tasks.Task AzureAudioToJSON(string filePath)
    {
        var speechConfig = SpeechConfig.FromSubscription(azureKey, region);
        var audioConfig = AudioConfig.FromWavFileInput(filePath);

        using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
        {
            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                Debug.Log("Transcription: " + result.Text);
                StartCoroutine(SendOpenAIRequest(result.Text));
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                Debug.LogWarning("No speech could be recognized.");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                Debug.LogError($"Speech recognition canceled: {cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Debug.LogError($"Error Code: {cancellation.ErrorCode}");
                    Debug.LogError($"Error Details: {cancellation.ErrorDetails}");
                }
            }
        }
    }

    IEnumerator SendOpenAIRequest(string rawText)
    {
        string promptAndInput = prompt.Replace("\"", "\\\"") +
            " input: " + rawText.Replace("\"", "\\\"") +
            " template: " + json_template.Replace("\"", "\\\"");

        string jsonPayload = "{" +
            "\"model\": \"gpt-4\"," +
            "\"messages\": [{" +
                "\"role\": \"user\"," +
                "\"content\": \"" + promptAndInput + "\"" +
            "}]" +
        "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        Debug.Log("Sending request to OpenAI...");
        Debug.Log($"Using API Key: {openAIKey}");

        UnityWebRequest request = new UnityWebRequest(openAIURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseTextContent = ExtractMessage(request.downloadHandler.text);
            Debug.Log("OpenAI Response: " + responseTextContent);
        }
        else
        {
            Debug.Log("Error: " + request.error);
        }
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

    private string ExtractMessage(string jsonResponse)
    {
        OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(jsonResponse);
        return response.choices[0].message.content;
    }

    public async System.Threading.Tasks.Task ProcessAudioFile(string audioPath)
    {
        audioFilePath = audioPath;
        if (!File.Exists(audioPath))
        {
            Debug.LogError("Audio file not found: " + audioPath);
            return;
        }

        Debug.Log("Starting transcription...");
        await AzureAudioToJSON(audioPath);
    }
}