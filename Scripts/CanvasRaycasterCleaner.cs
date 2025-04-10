using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class CanvasRaycasterCleaner : MonoBehaviour
{
    [Tooltip("Print to Console when Raycasters are removed")]
    public bool logRemovals = true;

    void Start()
    {
        int removedCount = 0;

        TrackedDeviceGraphicRaycaster[] raycasters = FindObjectsOfType<TrackedDeviceGraphicRaycaster>(true);

        foreach (var raycaster in raycasters)
        {
            if (raycaster != null)
            {
                if (logRemovals)
                {
                    Debug.LogWarning($"🧹 Removing TrackedDeviceGraphicRaycaster from: {raycaster.gameObject.name}", raycaster);
                }

                Destroy(raycaster);
                removedCount++;
            }
        }

        if (removedCount == 0 && logRemovals)
        {
            Debug.Log("✅ No TrackedDeviceGraphicRaycaster components found.");
        }
    }
}
