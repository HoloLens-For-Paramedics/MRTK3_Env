using System;
using System.IO;
using UnityEngine;

public class AudioFileLogger : MonoBehaviour
{
    private string directoryPath;
    private FileSystemWatcher fileWatcher;
    private bool isMonitoring = false;

    public void StartMonitoring()
    {
        if (isMonitoring) return; // Prevent multiple starts

        Debug.Log("🔍 Starting Audio File Monitoring...");

        // Ensure the directory exists
        directoryPath = Path.Combine(Application.persistentDataPath, "Recordings");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Set up file system watcher
        fileWatcher = new FileSystemWatcher(directoryPath, "*.wav");
        fileWatcher.Created += OnNewFileCreated;
        fileWatcher.EnableRaisingEvents = true; // Start monitoring

        isMonitoring = true;
        Debug.Log($"File Monitor:📂 Monitoring for new audio files in: {directoryPath}");
    }

    private void OnNewFileCreated(object sender, FileSystemEventArgs e)
    {
        Debug.Log($"File Montor:📢 New recording saved: {Path.GetFileName(e.FullPath)}");
    }

    void OnDestroy()
    {
        // Clean up file watcher to prevent memory leaks
        if (fileWatcher != null)
        {
            fileWatcher.EnableRaisingEvents = false;
            fileWatcher.Dispose();
        }
    }
}
