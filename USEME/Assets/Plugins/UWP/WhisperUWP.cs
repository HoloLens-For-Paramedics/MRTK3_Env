using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WhisperUWP : MonoBehaviour
{
    [DllImport("whisper")]
    private static extern IntPtr ProcessAudio(string filePath);

    void Start()
    {
        // string audioPath = Application.persistentDataPath + "/test.wav";
        string audioPath = @"C:\Users\lcald\OneDrive\Documents\GitHub\unity\USEME\AudioFiles_Test\test.wav";
        IntPtr result = ProcessAudio(audioPath);
        string transcript = Marshal.PtrToStringAnsi(result);
        Debug.Log("Transcription: " + transcript);
    }
}
