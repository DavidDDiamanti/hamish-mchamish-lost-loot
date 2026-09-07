using System.IO;
using TMPro;
using UnityEngine;

// CustomCsvImportUI: lobby panel for importing three CSV files (one per level) to create a
// custom question run. Validates that the paths exist before reading, then delegates parsing
// and validation to CustomCsvRunManager.CreateRunFromCsvTexts().
// Feedback text reports success or failure to the user in-panel.
public class CustomCsvImportUI : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject panel;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField level1PathInput;
    [SerializeField] private TMP_InputField level2PathInput;
    [SerializeField] private TMP_InputField level3PathInput;
    [SerializeField] private TMP_InputField runNameInput;
    [SerializeField] private TMP_Text feedbackText;

    void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void TogglePanel()
    {
        if (panel == null) return;

        panel.SetActive(!panel.activeSelf);
    }

    public void OpenPanel()
    {
        if (panel == null) return;

        panel.SetActive(true);
        feedbackText.text = "";
    }

    public void ClosePanel()
    {
        if (panel == null) return;

        panel.SetActive(false);
    }

    public void CreateCustomRun()
    {
        string path1 = level1PathInput.text.Trim();
        string path2 = level2PathInput.text.Trim();
        string path3 = level3PathInput.text.Trim();

        if (!File.Exists(path1) || !File.Exists(path2) || !File.Exists(path3))
        {
            feedbackText.text = "One or more CSV paths are invalid.";
            return;
        }

        string csv1 = File.ReadAllText(path1);
        string csv2 = File.ReadAllText(path2);
        string csv3 = File.ReadAllText(path3);

        string runName = string.IsNullOrWhiteSpace(runNameInput.text)
            ? "Custom CSV Run"
            : runNameInput.text.Trim();

        bool ok = CustomCsvRunManager.Instance.CreateRunFromCsvTexts(csv1, csv2, csv3, runName);

        feedbackText.text = ok
            ? "Custom CSV run created successfully."
            : "Failed to create custom CSV run.";
    }
}
