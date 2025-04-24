// AzureDocInt.cs
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using TMPro;

public class AzureDocInt : MonoBehaviour
{
    [Header("Azure Settings")]
    [SerializeField] private string endpoint = "";
    [SerializeField] private string apiKey = "";

    [Header("UI Elements")]
    public TextMeshPro Info;
    public GameObject Window;

    public void StartFormAnalysis(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError("Image file not found: " + imagePath);
            return;
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        StartCoroutine(AnalyzeForm(imageBytes, imagePath));
    }

    private IEnumerator AnalyzeForm(byte[] imageBytes, string imagePath)
    {
        string url = endpoint + "formrecognizer/documentModels/prebuilt-layout:analyze?api-version=2024-11-30";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(imageBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);
        request.SetRequestHeader("Content-Type", "application/octet-stream");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string operationLocation = request.GetResponseHeader("operation-location");
            if (!string.IsNullOrEmpty(operationLocation))
            {
                StartCoroutine(PollForResult(operationLocation, imagePath));
            }
            else
            {
                Debug.LogError("No operation-location header found.");
            }
        }
        else
        {
            Debug.LogError("❌ Upload failed: " + request.responseCode + " " + request.error + " - " + request.downloadHandler.text);
        }
    }

    private IEnumerator PollForResult(string operationLocation, string imagePath)
    {
        bool done = false;

        while (!done)
        {
            yield return new WaitForSeconds(1f);

            UnityWebRequest request = UnityWebRequest.Get(operationLocation);
            request.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                if (json.Contains("\"status\":\"succeeded\""))
                {
                    string extractedText = AzureDocParser.ParseDocument(json);
                    Debug.Log("✅ Extracted Data:\n" + extractedText);
                    Info.text = extractedText;
                    Window.SetActive(true);
                    done = true;
                    File.Delete(imagePath);
                }
                else
                {
                    Debug.Log("⌛ Awaiting results...");
                }
            }
            else
            {
                Debug.LogError("Polling failed: " + request.error);
                done = true;
            }
        }
    }
}
