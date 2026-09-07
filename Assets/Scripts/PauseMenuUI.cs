using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PauseMenuUI: Escape-key pause menu that also serves as the quest reward collection screen.
// When a quest completes mid-level, a questCompleteNotification banner prompts the player to open
// the pause menu. Inside the menu, each completed quest shows a "Collect" button that calls
// QuestManager.TryCollectReward() to distribute gold/gems immediately.
// CollectAllCompletedQuests() is called automatically when the level completes so rewards are
// not lost if the player never opened the pause menu.
public class PauseMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseRoot;

    [Header("Quest Rows (4)")]
    [SerializeField] private TMP_Text[] questTitleTexts;     // size 4
    [SerializeField] private TMP_Text[] questProgressTexts;  // size 4

    [Header("Quest Action Buttons (4)")]
    [SerializeField] private Button[] questActionButtons;        // size 4
    [SerializeField] private TMP_Text[] questActionButtonTexts;  // size 4

    [Header("Quest Complete Notification")]
    [SerializeField] private GameObject questCompleteNotificationRoot;
    [SerializeField] private TMP_Text questCompleteNotificationText;

    [Header("Quit Level Button")]
    [SerializeField] private Button quitLevelButton;

    private bool isPaused = false;
    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>(true);

        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        if (questCompleteNotificationRoot != null)
            questCompleteNotificationRoot.SetActive(false);

        quitLevelButton.onClick.RemoveAllListeners();
        if (gameManager != null)
            quitLevelButton.onClick.AddListener(gameManager.QuitLevel);
    }

    private void OnEnable()
    {
        GameEvents.QuestCompleted += OnQuestCompleted;
    }

    private void OnDisable()
    {
        GameEvents.QuestCompleted -= OnQuestCompleted;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameManager.IsGamePaused())
                Resume();
            else if (gameManager.CanPlayerPause())
                Pause();
        }
    }

    private void OnQuestCompleted()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.HasUncollectedRewards())
            ShowQuestCompleteNotification(true);

        if (gameManager.IsLevelComplete())
        {
            CollectAllCompletedQuests();
        }
    }

    private void ShowQuestCompleteNotification(bool show)
    {
        if (questCompleteNotificationRoot == null) return;
        if (gameManager.IsLevelComplete()) return;

        if (show && !gameManager.IsLevelComplete())
        {
            if (questCompleteNotificationText != null)
                questCompleteNotificationText.text = "A quest has been completed! Press 'esc' to collect the rewards";
            questCompleteNotificationRoot.SetActive(true);
        }
        else
        {
            questCompleteNotificationRoot.GetComponent<NotificationAnimated>()?.Hide();
        }
    }

    public void Pause()
    {
        if (!gameManager.CanPlayerPause()) return;
        if (gameManager.IsLevelComplete()) return;

        gameManager.SetGamePaused(true);

        if (pauseRoot != null)
            pauseRoot.SetActive(true);

        ShowQuestCompleteNotification(false);

        RefreshQuestDisplay();
        isPaused = true;
    }

    public void Resume()
    {
        if (!gameManager.IsGamePaused() || !isPaused) return;

        gameManager.SetGamePaused(false);

        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        isPaused = false;

        bool show = QuestManager.Instance != null && QuestManager.Instance.HasUncollectedRewards();
        ShowQuestCompleteNotification(show);
    }

    // Rebuilds the quest rows: in-progress quests show a disabled button, completed-uncollected
    // quests show a "Collect" button with the reward summary, collected quests show "Completed".
    private void RefreshQuestDisplay()
    {
        var quests = QuestManager.Instance != null ? QuestManager.Instance.GetActiveQuests() : null;

        if (questTitleTexts == null || questProgressTexts == null)
        {
            Debug.LogWarning("PauseMenuUI: Quest text arrays not assigned.");
            return;
        }

        int rows = Mathf.Min(questTitleTexts.Length, questProgressTexts.Length);

        for (int i = 0; i < rows; i++)
        {
            TMP_Text title = questTitleTexts[i];
            TMP_Text prog = questProgressTexts[i];

            Button btn = (questActionButtons != null && i < questActionButtons.Length) ? questActionButtons[i] : null;
            TMP_Text btnText = (questActionButtonTexts != null && i < questActionButtonTexts.Length) ? questActionButtonTexts[i] : null;

            if (btn != null)
                btn.onClick.RemoveAllListeners();

            if (title == null || prog == null)
                continue;

            if (quests != null && i < quests.Count && quests[i] != null)
            {
                QuestDefinition q = quests[i];
                int current = QuestManager.Instance.GetProgressForQuest(q);
                int target = Mathf.Max(1, q.targetAmount);

                bool isComplete = QuestManager.Instance.IsCompleted(q);

                title.text = q.title;
                prog.text = $"{Mathf.Min(current, target)}/{target}";
                prog.color = isComplete ? Color.green : Color.grey;

                if (btnText != null)
                {
                    if (!isComplete)
                    {
                        btnText.text = "In progress";
                        if (btn != null) btn.interactable = false;
                    }
                    else
                    {
                        bool collected = QuestManager.Instance.IsRewardCollected(q);

                        if (collected)
                        {
                            btnText.text = "Completed";
                            if (btn != null) btn.interactable = false;
                        }
                        else
                        {
                            var reward = QuestManager.Instance.GetPendingReward(q);

                            string rewardText = "";
                            if (reward.gold > 0) rewardText += $"+{reward.gold} gold";
                            if (reward.gems > 0)
                            {
                                if (!string.IsNullOrEmpty(rewardText)) rewardText += ", ";
                                rewardText += $"+{reward.gems} gems";
                            }

                            btnText.text = $"Collect\n ({rewardText})";

                            if (btn != null)
                            {
                                btn.interactable = true;
                                QuestDefinition qLocal = q;
                                btn.onClick.AddListener(() => OnCollectPressed(qLocal));
                            }
                        }
                    }
                }
            }
            else
            {
                title.text = "-";
                prog.text = "";
                prog.color = Color.grey;

                if (btnText != null) btnText.text = "";
                if (btn != null) btn.interactable = false;
            }
        }
    }

    private void OnCollectPressed(QuestDefinition quest)
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.TryCollectReward(quest, out _);

        RefreshQuestDisplay();

        if (!QuestManager.Instance.HasUncollectedRewards())
            ShowQuestCompleteNotification(false);
    }

    public void OnResumeButtonPressed() => Resume();

    // Auto-collects all pending quest rewards. Called by GameManager when a level completes
    // so rewards are not lost if the player skipped opening the pause menu.
    public void CollectAllCompletedQuests()
    {
        if (QuestManager.Instance == null) return;

        var quests = QuestManager.Instance.GetActiveQuests();
        if (quests == null) return;

        bool collectedAny = false;

        foreach (var q in quests)
        {
            if (q == null) continue;

            if (QuestManager.Instance.IsCompleted(q) && !QuestManager.Instance.IsRewardCollected(q))
            {
                if (QuestManager.Instance.TryCollectReward(q, out _))
                    collectedAny = true;
            }
        }

        if (collectedAny)
            RefreshQuestDisplay();

        if (!QuestManager.Instance.HasUncollectedRewards())
            ShowQuestCompleteNotification(false);
    }

    public void HideNotification() => ShowQuestCompleteNotification(false);
}
