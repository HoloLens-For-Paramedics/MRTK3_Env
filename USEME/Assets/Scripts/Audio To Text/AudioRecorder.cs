using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class AudioRecorder : MonoBehaviour
{
    private AudioClip recordingClip;
    private bool isRecording = false;
    private string directoryPath;
    private float recordingDuration = 10f; // Time in seconds for recording
    private bool waitingToSave = false;
    private AudioFileLogger fileLogger;

    void Start()
    {
        // Ensure the directory exists
        directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Find the AudioFileLogger in the scene
        fileLogger = FindObjectOfType<AudioFileLogger>();
    }

    public void StartRecording()
    {
        if (isRecording) return;

        Debug.Log("🎤 Starting continuous audio recording...");
        isRecording = true;
        waitingToSave = false;

        // Start monitoring file creations
        if (fileLogger != null)
        {
            fileLogger.StartMonitoring();
        }

        StartCoroutine(ContinuousRecording());
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        Debug.Log("🛑 Stopping audio recording...");
        isRecording = false;

        if (waitingToSave)
        {
            Debug.Log("⌛ Waiting for the last recording chunk to save...");
            StartCoroutine(FinishLastRecording());
        }
        else
        {
            Microphone.End(null);
        }
    }

    private IEnumerator ContinuousRecording()
    {
        while (isRecording)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // Generate timestamp
            string filePath = Path.Combine(directoryPath, $"recorded_audio_{timestamp}.wav");
            Debug.Log($"🎙 Recording chunk at {timestamp}...");

            recordingClip = Microphone.Start(null, false, (int)recordingDuration, 44100); // 10s, 44.1kHz
            waitingToSave = true;

            yield return new WaitForSeconds(recordingDuration); // Wait for recording to complete

            if (isRecording)
            {
                SaveAudioToFile(filePath);
                Debug.Log($"Audio files will be saved in: {directoryPath}");
            }

            waitingToSave = false;
        }
    }

    private IEnumerator FinishLastRecording()
    {
        yield return new WaitForSeconds(0.5f);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // Generate timestamp
        string filePath = Path.Combine(directoryPath, $"recorded_audio_{timestamp}.wav");
        Debug.Log("💾 Saving the final recording chunk before stopping...");
        SaveAudioToFile(filePath);

        Microphone.End(null);
        waitingToSave = false;
    }

    private void SaveAudioToFile(string filePath)
    {
        if (recordingClip == null)
        {
            Debug.LogError("❌ No recorded audio to save.");
            return;
        }

        Debug.Log($"💾 Saving recorded audio to {filePath}");
        byte[] wavData = WavUtility.FromAudioClip(recordingClip);
        File.WriteAllBytes(filePath, wavData);
        Debug.Log($"✅ Audio successfully saved: {filePath}");
    }

    public string GetLastRecordedFilePath()
    {
        // Get the most recently saved file based on timestamp
        DirectoryInfo dir = new DirectoryInfo(directoryPath);
        FileInfo latestFile = null;

        foreach (FileInfo file in dir.GetFiles("recorded_audio_*.wav"))
        {
            if (latestFile == null || file.LastWriteTime > latestFile.LastWriteTime)
            {
                latestFile = file;
            }
        }

        return latestFile != null ? latestFile.FullName : "No recordings found";
    }
}
