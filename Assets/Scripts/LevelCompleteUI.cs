using TMPro;
using UnityEngine;
using UnityEngine.UI;

// LevelCompleteUI: panel shown when the player completes a level.
// Displays the reward summary string built by GameManager.CalculateLevelCompletionBonuses()
// and routes the return-to-menu button to GameManager.FinishLevel().
public class LevelCompleteUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private TMP_Text rewardsSummaryText;
    [SerializeField] private TMP_Text titleText;

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>(true);

        if (returnToMenuButton == null)
            returnToMenuButton = GetComponentInChildren<Button>(true);

        returnToMenuButton.onClick.AddListener(gameManager.FinishLevel);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Show(string rewardSummary)
    {
        if (titleText != null)
            titleText.text = "Level Complete!";

        if (rewardsSummaryText != null)
            rewardsSummaryText.text = rewardSummary;

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
