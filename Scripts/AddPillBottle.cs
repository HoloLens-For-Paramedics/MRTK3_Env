using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

public class AddPillBottle : MonoBehaviour
{
    public DynamicChecklist checklist; // Assign your DynamicChecklist object in the Inspector

    public string newMenuTitle = "New Protocols";
    public string[] newSubmenuItems = { "NP01. New Item 1", "NP02. New Item 2", "NP03. New Item 3" };

    public void AddNewMenuItem()
    {
        if (checklist == null)
        {
            Debug.LogError("Checklist reference is missing!");
            return;
        }

        // Get private fields via reflection
        var checklistType = checklist.GetType();
        var mainMenuField = checklistType.GetField("mainMenuOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        var subMenuField = checklistType.GetField("subMenuOptions", BindingFlags.NonPublic | BindingFlags.Instance);

        if (mainMenuField == null || subMenuField == null)
        {
            Debug.LogError("Failed to access private fields.");
            return;
        }

        var mainMenuArray = mainMenuField.GetValue(checklist) as string[];
        var subMenuDict = subMenuField.GetValue(checklist) as Dictionary<string, string[]>;

        if (mainMenuArray == null || subMenuDict == null)
        {
            Debug.LogError("Main menu or submenu is null.");
            return;
        }

        // Only add if it doesn't already exist
        if (!Array.Exists(mainMenuArray, item => item == newMenuTitle))
        {
            // Update submenu
            subMenuDict[newMenuTitle] = newSubmenuItems;

            // Update main menu
            var updatedMenu = new List<string>(mainMenuArray) { newMenuTitle };
            mainMenuField.SetValue(checklist, updatedMenu.ToArray());

            // Call GenerateButtons(string[], Action<string>) using reflection
            var generateButtonsMethod = checklistType.GetMethod("GenerateButtons", BindingFlags.NonPublic | BindingFlags.Instance);
            var callback = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), checklist, "OnMainMenuButtonClick");

            generateButtonsMethod.Invoke(checklist, new object[] { updatedMenu.ToArray(), callback });

            Debug.Log($"'{newMenuTitle}' added to menu and UI updated.");
        }
        else
        {
            Debug.LogWarning($"'{newMenuTitle}' already exists.");
        }
    }
}