using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class JsonFetcher : MonoBehaviour
{
    public SupabaseAPI supabaseAPI;
    public void OnUserFetchButtonClick()
    {
        string userID = "";
        supabaseAPI.GetUserInfo(userID);
    }
}