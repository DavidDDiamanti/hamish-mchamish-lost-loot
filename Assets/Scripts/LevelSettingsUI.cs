using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

// LevelSettingsUI: lobby panel for choosing level, difficulty, and chest count before a run.
// Level dropdown is built from GameManager.GetMaxUnlockedLevel() so only unlocked levels appear.
// Selecting a newly-unlocked level calls GameManager.AcknowledgeUnlockedLevel() to clear the
// "Level X unlocked!" notification. ApplySettings() commits the selections to GameManager and
// loads MainScene immediately.
public class LevelSettingsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown levelDropdown;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private TMP_Dropdown chestCountDropdown;
    [SerializeField] private TMP_Text difficultyDescriptionText;

    [Header("Optional")]
    [SerializeField] private GameObject panelRoot;

    [Header("Scene to load")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private TMP_Text newlyUnlockedLevelText;
    private GameManager gameManager;

    private readonly int[] difficultyValues = { 1, 2, 3 };
    private readonly int[] chestCountValues = { 10, 20, 30 };
    private readonly List<int> availableLevelValues = new List<int>();

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>(true);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (levelDropdown != null) levelDropdown.ClearOptions();
        if (difficultyDropdown != null) difficultyDropdown.ClearOptions();
        if (chestCountDropdown != null) chestCountDropdown.ClearOptions();

        BuildLevelDropdown();

        difficultyDropdown.AddOptions(new List<string> { "1", "2", "3" });
        chestCountDropdown.AddOptions(new List<string> { "10", "20", "30" });

        SetDropdownToValue(difficultyDropdown, difficultyValues, gameManager.GetCurrentDifficulty());
        SetDropdownToValue(chestCountDropdown, chestCountValues, gameManager.GetNumberOfUnopenedChests());
        SetLevelDropdownToCurrentLevel();

        difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);

        if (levelDropdown != null)
            levelDropdown.onValueChanged.AddListener(OnLevelChanged);

        UpdateDifficultyDescription();
        UpdateNewLevelNotification();
    }

    private void OnDestroy()
    {
        if (difficultyDropdown != null)
            difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged);

        if (levelDropdown != null)
            levelDropdown.onValueChanged.RemoveListener(OnLevelChanged);
    }

    private void BuildLevelDropdown()
    {
        if (levelDropdown == null || gameManager == null) return;

        levelDropdown.ClearOptions();
        availableLevelValues.Clear();

        int maxUnlocked = gameManager.GetMaxUnlockedLevel();
        List<string> levelOptions = new List<string>();

        for (int level = 1; level <= maxUnlocked; level++)
        {
            availableLevelValues.Add(level);
            levelOptions.Add(level.ToString());
        }

        levelDropdown.AddOptions(levelOptions);
    }

    private void SetDropdownToValue(TMP_Dropdown dd, int[] values, int target)
    {
        if (dd == null) return;

        int idx = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == target)
            {
                idx = i;
                break;
            }
        }

        dd.value = idx;
        dd.RefreshShownValue();
    }

    private void SetLevelDropdownToCurrentLevel()
    {
        if (levelDropdown == null || gameManager == null || availableLevelValues.Count == 0)
            return;

        int currentLevel = gameManager.GetCurrentLevel();
        int idx = 0;

        for (int i = 0; i < availableLevelValues.Count; i++)
        {
            if (availableLevelValues[i] == currentLevel)
            {
                idx = i;
                break;
            }
        }

        levelDropdown.value = idx;
        levelDropdown.RefreshShownValue();
    }

    private void OnDifficultyChanged(int _)
    {
        UpdateDifficultyDescription();
    }

    private void UpdateDifficultyDescription()
    {
        if (difficultyDescriptionText == null || difficultyDropdown == null)
            return;

        int chosenDifficulty = difficultyValues[difficultyDropdown.value];

        switch (chosenDifficulty)
        {
            case 1:
                difficultyDescriptionText.text =
                    "Base difficulty:\n\n" +
                    "- Enemies are slow and weak\n\n" +
                    "- Rewards are at their normal values";
                break;

            case 2:
                difficultyDescriptionText.text =
                    "Intermediate difficulty:\n\n" +
                    "- There are more enemies, and they are faster and stronger\n\n" +
                    "- All rewards are doubled";
                break;

            case 3:
                difficultyDescriptionText.text =
                    "Hard difficulty:\n\n" +
                    "- There are many more enemies, and they are very fast and very strong\n\n" +
                    "- All rewards are tripled";
                break;
        }
    }

    public void ApplySettings()
    {
        if (gameManager == null) return;
        if (availableLevelValues.Count == 0) return;

        int chosenLevel = availableLevelValues[levelDropdown.value];
        int chosenDifficulty = difficultyValues[difficultyDropdown.value];
        int chosenChestCount = chestCountValues[chestCountDropdown.value];

        gameManager.SetCurrentLevel(chosenLevel);
        gameManager.setDifficulty(chosenDifficulty);
        gameManager.setChestCount(chosenChestCount);

        SceneManager.LoadScene(mainSceneName);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        BuildLevelDropdown();
        SetLevelDropdownToCurrentLevel();
        UpdateNewLevelNotification();
        UpdateDifficultyDescription();
        gameManager.SetGamePaused(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        gameManager.SetGamePaused(false);
    }

    private void OnLevelChanged(int _)
    {
        if (gameManager == null || levelDropdown == null || availableLevelValues.Count == 0)
            return;

        int selectedLevel = availableLevelValues[levelDropdown.value];

        gameManager.AcknowledgeUnlockedLevel(selectedLevel);
        UpdateNewLevelNotification();
    }

    private void UpdateNewLevelNotification()
    {
        if (newlyUnlockedLevelText == null || gameManager == null)
            return;

        int newlyUnlockedLevel = gameManager.GetNewlyUnlockedLevel();

        bool show =
            newlyUnlockedLevel > 0 &&
            levelDropdown != null &&
            availableLevelValues.Contains(newlyUnlockedLevel) &&
            availableLevelValues[levelDropdown.value] != newlyUnlockedLevel;

        newlyUnlockedLevelText.gameObject.SetActive(show);

        if (show)
        {
            newlyUnlockedLevelText.text =
                "Level " + newlyUnlockedLevel + " has been unlocked!";
        }
    }

    public bool IsShowing() => panelRoot != null && panelRoot.activeSelf;
}
