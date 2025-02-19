using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DynamicChecklist : MonoBehaviour
{
    public GameObject scrollContent; // Assign Scroll View's Content here
    public Button buttonPrefab; // Assign a Button prefab in the Inspector
    public Toggle checkboxPrefab; // Assign a Checkbox prefab in the Inspector
    public Button backButtonPrefab; // Assign a Back Button prefab in the Inspector

    // Sample Data
    private string[] mainMenuOptions = { "Adult Protocols", "Pediatric Protocols", "Standard Protocols", "Procedures", "Optional Scope", "Policies" };
    private Dictionary<string, string[]> subMenuOptions = new Dictionary<string, string[]>
    {
        { "Adult Protocols", new[] {  "A01. Abdominal Emergencies", "A02. Seizure", "A03. Hypoglycemia", "A04. Sepsis", "A05. Bradycardia", "A06. Burns", "A07. Cardiac Arrest", "A08. Chest Pain-Suspected Cardiac Ischemia", "A09. Environmental Emergencies", "A10. Shock", "A11. Respiratory Distress", "A12. Allergic Reaction / Anaphylaxis", "A13. Stroke", "A14. Tachycardia with Pulses", "A15. Poisoning and Overdose", "A16. Trauma Care", "A18. Gynecological and Obstetrical Emergencies", "A19. Crush Injury Syndrome", "A20. Behavioral Emergency - Combative"  } },
        { "Pediatric Protocols", new[] { "Pediatric Procedure 1", "Pediatric Procedure 2" } },
        { "Standard Protocols", new[] { "Standard Procedure 1", "Standard Procedure 2", "Standard Procedure 3" } },
        { "Procedures", new[] { "Procedure 1", "Procedure 2", "Procedure 3", "Procedure 4" } },
        { "Optional Scope", new[] { "Scope Option 1", "Scope Option 2" } },
        { "Policies", new[] { "Policy 1", "Policy 2", "Policy 3" } }
    };

    private List<GameObject> currentUIElements = new List<GameObject>();
    private Stack<System.Action> navigationStack = new Stack<System.Action>();

    // Dictionary to save checkbox states
    private Dictionary<string, bool> checkboxStates = new Dictionary<string, bool>();

    private void Start()
    {
        // Generate main menu buttons on start
        GenerateButtons(mainMenuOptions, OnMainMenuButtonClick);
    }

    private void GenerateButtons(string[] options, System.Action<string> onClickCallback)
    {
        ClearCurrentUI(); // Clears any previously generated buttons

        foreach (string option in options)
        {
            // Instantiate a new button from the prefab
            Button newButton = Instantiate(buttonPrefab, scrollContent.transform);
            TMPro.TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            // Set the text of the button to the current option
            if (buttonText != null)
            {
                buttonText.text = option;  // This should correctly set the button text
            }
            else
            {
                Debug.LogError("Button prefab is missing a Text component! Please ensure the prefab has a Text component.");
            }

            // Add an onClick listener to the button
            newButton.onClick.AddListener(() => onClickCallback(option));

            // Keep track of the newly created UI element
            currentUIElements.Add(newButton.gameObject);
        }

        // Add a back button if there is a previous menu
        if (navigationStack.Count > 0)
        {
            AddBackButton();
        }
    }

    private void GenerateCheckboxes(string[] options)
    {
        ClearCurrentUI();
        foreach (var option in options)
        {
            Toggle newCheckbox = Instantiate(checkboxPrefab, scrollContent.transform);
            Text checkboxText = newCheckbox.GetComponentInChildren<Text>();
            if (checkboxText != null)
            {
                checkboxText.text = option;  // This should correctly set the checkbox text
            }
            else
            {
                Debug.LogError("Checkbox prefab is missing a Text component! Please ensure the prefab has a Text component.");
            }

            // Restore checkbox state if it was previously saved
            if (checkboxStates.ContainsKey(option))
            {
                newCheckbox.isOn = checkboxStates[option];
            }
            else
            {
                newCheckbox.isOn = false; // Default to unchecked
            }

            // Save checkbox state when its value changes
            newCheckbox.onValueChanged.AddListener((value) =>
            {
                checkboxStates[option] = value;
            });

            currentUIElements.Add(newCheckbox.gameObject);
        }

        // Add a back button if there is a previous menu
        if (navigationStack.Count > 0)
        {
            AddBackButton();
        }
    }

    private void AddBackButton()
    {
        if (backButtonPrefab != null)
        {
            Button backButton = Instantiate(backButtonPrefab, scrollContent.transform);
            Text backButtonText = backButton.GetComponentInChildren<Text>();
            if (backButtonText != null)
            {
                backButtonText.text = "Back";  // Ensure the Back button text is correctly set
            }
            backButton.onClick.AddListener(OnBackButtonClick);
            currentUIElements.Add(backButton.gameObject);
        }
        else
        {
            Debug.LogError("Back Button Prefab is missing. Please assign a back button prefab.");
        }
    }

    private void OnMainMenuButtonClick(string selectedOption)
    {
        if (subMenuOptions.ContainsKey(selectedOption))
        {
            // Save the current menu generator function to the navigation stack (main menu button click)
            navigationStack.Push(() => GenerateButtons(mainMenuOptions, OnMainMenuButtonClick));

            // Generate the submenu
            GenerateButtons(subMenuOptions[selectedOption], OnSubMenuButtonClick);
        }
        else
        {
            Debug.LogError($"Submenu for '{selectedOption}' not found!");
        }
    }

    private void OnSubMenuButtonClick(string selectedSubOption)
    {
        // Save the current submenu generator function to the stack
        string[] checkboxOptions = new[] { $"{selectedSubOption} Task 1", $"{selectedSubOption} Task 2", $"{selectedSubOption} Task 3" };
        navigationStack.Push(() => GenerateButtons(subMenuOptions[selectedSubOption], OnSubMenuButtonClick));

        // Generate checkboxes for the selected procedure or protocol
        GenerateCheckboxes(checkboxOptions);
    }

    private void OnBackButtonClick()
    {
        if (navigationStack.Count > 0)
        {
            // Get the last menu generator function and invoke it (this should take us to the previous menu)
            var previousMenu = navigationStack.Pop();
            previousMenu.Invoke();
        }
        else
        {
            Debug.LogError("No previous menu in the navigation stack!");
        }
    }


    private void ClearCurrentUI()
    {
        foreach (var element in currentUIElements)
        {
            Destroy(element);
        }
        currentUIElements.Clear();
    }
}
