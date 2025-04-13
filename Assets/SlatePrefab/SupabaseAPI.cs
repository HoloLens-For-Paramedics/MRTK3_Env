using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

public class SupabaseAPI : MonoBehaviour
{
    private const string SUPABASE_URL = "https://yuwrsuaqhbbfxqlrybgg.supabase.co/rest/v1/PatientData";
    private const string SUPABASE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Inl1d3JzdWFxaGJiZnhxbHJ5YmdnIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0MDA3NTk0NywiZXhwIjoyMDU1NjUxOTQ3fQ.oDOmFPwxbq9FosgsJb4YPs3xwVTPdNL4ihNlw3oZwTk";
    public JsonRender jsonRender;
    public PatientsRender PatientsRender;
    JArray jsonArray;

    public void GetUserInfo(string userId)
    {
        StartCoroutine(FetchUserInfo(userId));
    }

    IEnumerator FetchUserInfo(string userId)
    {
        string endpoint;
        bool isUserIdEmpty = string.IsNullOrEmpty(userId);

        if (isUserIdEmpty)
        {
            endpoint = $"{SUPABASE_URL}?order=PatientID.desc&limit=10";
        }
        else
        {
            endpoint = $"{SUPABASE_URL}?PatientID=eq.{userId}";
        }

        UnityWebRequest request = UnityWebRequest.Get(endpoint);
        request.SetRequestHeader("apikey", SUPABASE_KEY);
        request.SetRequestHeader("Authorization", $"Bearer {SUPABASE_KEY}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Successful Fetching User Data: {request.downloadHandler.text}");
            if (isUserIdEmpty)
            {
                Debug.Log("Fetching all patients");
                PatientsRender.DisplayPatients(request.downloadHandler.text);
            }
            else
            {
                Debug.Log($"Fetching user with ID: {userId}");
                jsonArray = JArray.Parse(request.downloadHandler.text);
                JObject jsonObject = jsonArray[0] as JObject;
                if (jsonObject != null)
                {
                    jsonRender.DisplayPatientData(jsonObject);
                }
                else
                {
                    Debug.LogError("Failed to parse JSON object.");
                }
            }
        }
        else
        {
            Debug.LogError($"Error Fetching User Data: {request.error}");
        }
    }
}
