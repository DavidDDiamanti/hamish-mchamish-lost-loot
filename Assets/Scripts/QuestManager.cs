using System.Collections.Generic;
using UnityEngine;

// QuestManager: singleton that tracks quest progress during a run.
// Listens to GameEvents (EnemyDefeated, StreakChanged, GoldGained, ChestOpened) to advance progress.
// Completed quests move to pendingRewards; rewards are not distributed until TryCollectReward()
// is called from PauseMenuUI, allowing the player to collect manually.
// FinalizeRunCompletedQuests() persists completed quest IDs to PlayerPrefs (profile-namespaced)
// when a level completes so they are never re-assigned in future runs.
// ResetAllQuestProgress() wipes all saved completions and regenerates: used for dev resets.
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [System.Serializable]
    public struct QuestReward
    {
        public int gold;
        public int gems;

        public QuestReward(int gold, int gems)
        {
            this.gold = gold;
            this.gems = gems;
        }

        public bool HasAnyReward()
        {
            return gold > 0 || gems > 0;
        }
    }

    [Header("Quest Pool")]
    [SerializeField] private List<QuestDefinition> availableQuests;

    [Header("Active Quests")]
    [SerializeField] private List<QuestDefinition> activeQuests;

    [Header("Settings")]
    [SerializeField] private int maxActiveQuests = 4;

    private readonly Dictionary<string, int> progress = new();
    private readonly HashSet<string> runCompleted = new();
    private readonly HashSet<string> permanentlyCompleted = new();

    private const string QuestCompletedKeyPrefix = "QUEST_PERMA_COMPLETED_";

    private readonly Dictionary<string, QuestReward> pendingRewards = new();
    private readonly HashSet<string> collectedRewards = new();

    private GameManager gameManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        gameManager = FindObjectOfType<GameManager>(true);

        LoadPermanentQuestCompletions();

        EnsureActiveQuests(maxActiveQuests);

        foreach (var q in activeQuests)
        {
            if (q == null) continue;
            if (!progress.ContainsKey(q.questId))
                progress[q.questId] = 0;
        }
    }

    private void OnEnable()
    {
        GameEvents.EnemyDefeated += OnEnemyDefeated;
        GameEvents.StreakChanged += OnStreakChanged;
        GameEvents.GoldGained += OnGoldGained;
        GameEvents.ChestOpened += OnChestOpened;
    }

    private void OnDisable()
    {
        GameEvents.EnemyDefeated -= OnEnemyDefeated;
        GameEvents.StreakChanged -= OnStreakChanged;
        GameEvents.GoldGained -= OnGoldGained;
        GameEvents.ChestOpened -= OnChestOpened;
    }

    private string GetPermanentQuestKey(string questId)
    {
        return GameProfileContext.GetSaveKey(QuestCompletedKeyPrefix + questId);
    }

    private bool IsQuestPermanentlyCompleted(string questId)
    {
        return PlayerPrefs.GetInt(GetPermanentQuestKey(questId), 0) == 1;
    }

    private void LoadPermanentQuestCompletions()
    {
        permanentlyCompleted.Clear();

        for (int i = 0; i < availableQuests.Count; i++)
        {
            QuestDefinition q = availableQuests[i];
            if (q == null || string.IsNullOrWhiteSpace(q.questId)) continue;

            if (IsQuestPermanentlyCompleted(q.questId))
                permanentlyCompleted.Add(q.questId);
        }
    }

    private void MarkQuestPermanentlyCompleted(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId)) return;

        permanentlyCompleted.Add(questId);
        PlayerPrefs.SetInt(GetPermanentQuestKey(questId), 1);
        PlayerPrefs.Save();
    }

    // Fills activeQuests up to desiredCount by picking from the pool, excluding already-active
    // and permanently-completed quests. Uses a safety counter to prevent infinite loops.
    private void EnsureActiveQuests(int desiredCount)
    {
        activeQuests.RemoveAll(q => q == null);
        availableQuests.RemoveAll(q => q == null);

        activeQuests.RemoveAll(q => q != null && permanentlyCompleted.Contains(q.questId));

        int safety = 1000;
        while (activeQuests.Count < desiredCount && safety-- > 0)
        {
            QuestDefinition pick = PickRandomQuestFromPoolExcludingActiveAndCompleted();
            if (pick == null) break;

            activeQuests.Add(pick);

            if (!progress.ContainsKey(pick.questId))
                progress[pick.questId] = 0;
        }
    }

    // Resets all per-run state and generates a fresh set of quests for the current level.
    // Called by GameManager at the start of each level.
    public void GenerateQuestsForCurrentLevel()
    {
        activeQuests.Clear();
        progress.Clear();
        runCompleted.Clear();
        pendingRewards.Clear();
        collectedRewards.Clear();

        EnsureActiveQuests(maxActiveQuests);

        foreach (var q in activeQuests)
        {
            if (q == null) continue;
            if (!progress.ContainsKey(q.questId))
                progress[q.questId] = 0;
        }

        GameEvents.RaiseQuestProgressChanged();
    }

    private QuestDefinition PickRandomQuestFromPoolExcludingActiveAndCompleted()
    {
        List<QuestDefinition> candidates = new();

        int currentLevel = 1;
        if (gameManager != null)
            currentLevel = gameManager.GetCurrentLevel();

        for (int i = 0; i < availableQuests.Count; i++)
        {
            var q = availableQuests[i];
            if (q == null) continue;
            if (permanentlyCompleted.Contains(q.questId)) continue;
            if (runCompleted.Contains(q.questId)) continue;
            if (activeQuests.Contains(q)) continue;
            if (!q.IsAvailableForLevel(currentLevel)) continue;

            candidates.Add(q);
        }

        if (candidates.Count == 0)
            return null;

        int idx = Random.Range(0, candidates.Count);
        return candidates[idx];
    }

    private void OnEnemyDefeated()
    {
        foreach (var q in activeQuests)
        {
            if (q == null || q.type != QuestType.DefeatEnemies) continue;
            AddProgress(q, 1);
        }
    }

    // ReachStreak quests track the highest streak reached, not cumulative count.
    private void OnStreakChanged(int streak)
    {
        foreach (var q in activeQuests)
        {
            if (q == null || q.type != QuestType.ReachStreak) continue;

            int current = GetProgress(q);
            if (streak > current)
                SetProgress(q, streak);
        }
    }

    private void OnGoldGained(int amount)
    {
        if (amount <= 0) return;

        foreach (var q in activeQuests)
        {
            if (q == null || q.type != QuestType.CollectGold) continue;
            AddProgress(q, amount);
        }
    }

    private void OnChestOpened()
    {
        foreach (var q in activeQuests)
        {
            if (q == null || q.type != QuestType.OpenChests) continue;
            AddProgress(q, 1);
        }
    }

    private int GetProgress(QuestDefinition q) =>
        progress.TryGetValue(q.questId, out int v) ? v : 0;

    private void SetProgress(QuestDefinition q, int value)
    {
        if (runCompleted.Contains(q.questId)) return;

        progress[q.questId] = value;
        GameEvents.RaiseQuestProgressChanged();
        CheckComplete(q);
    }

    private void AddProgress(QuestDefinition q, int delta)
    {
        if (runCompleted.Contains(q.questId)) return;

        progress[q.questId] = GetProgress(q) + delta;
        GameEvents.RaiseQuestProgressChanged();
        CheckComplete(q);
    }

    // Calculates the reward scaled by current difficulty and stores it in pendingRewards.
    // The reward is not distributed until TryCollectReward() is called from PauseMenuUI.
    private void CheckComplete(QuestDefinition q)
    {
        if (GetProgress(q) < q.targetAmount) return;
        if (runCompleted.Contains(q.questId)) return;

        runCompleted.Add(q.questId);

        int difficulty = (gameManager != null) ? gameManager.GetCurrentDifficulty() : 1;
        int multiplier = Mathf.Max(1, difficulty);

        int goldPayout = q.baseGoldReward * multiplier;
        int gemPayout = q.baseGemReward * multiplier;

        QuestReward reward = new QuestReward(goldPayout, gemPayout);
        pendingRewards[q.questId] = reward;

        Debug.Log($"Quest complete: {q.title} (pending +{goldPayout} gold, +{gemPayout} gems, diff {difficulty})");

        GameEvents.RaiseQuestCompleted();
    }

    public IReadOnlyList<QuestDefinition> GetActiveQuests() => activeQuests;

    public int GetProgressForQuest(QuestDefinition q)
    {
        if (q == null) return 0;
        return progress.TryGetValue(q.questId, out int v) ? v : 0;
    }

    public bool IsCompleted(QuestDefinition q)
    {
        if (q == null) return false;
        return runCompleted.Contains(q.questId);
    }

    public bool HasUncollectedRewards()
    {
        foreach (var qid in runCompleted)
        {
            if (pendingRewards.ContainsKey(qid) && !collectedRewards.Contains(qid))
                return true;
        }
        return false;
    }

    public bool IsRewardCollected(QuestDefinition q)
    {
        if (q == null) return false;
        return collectedRewards.Contains(q.questId);
    }

    public QuestReward GetPendingReward(QuestDefinition q)
    {
        if (q == null) return default;
        return pendingRewards.TryGetValue(q.questId, out QuestReward reward) ? reward : default;
    }

    // Distributes gold and gems via their respective managers and marks the quest collected.
    // Returns false if already collected, not completed, or if the reward has no value.
    public bool TryCollectReward(QuestDefinition q, out QuestReward reward)
    {
        reward = default;

        if (q == null) return false;
        if (!runCompleted.Contains(q.questId)) return false;
        if (collectedRewards.Contains(q.questId)) return false;
        if (!pendingRewards.TryGetValue(q.questId, out reward)) return false;
        if (!reward.HasAnyReward()) return false;

        collectedRewards.Add(q.questId);

        if (reward.gold > 0 && GoldManager.Instance != null)
            GoldManager.Instance.AddGold(reward.gold);

        if (reward.gems > 0 && GemsManager.Instance != null)
            GemsManager.Instance.AddGems(reward.gems);

        GameEvents.RaiseQuestCollected();
        return true;
    }

    // Writes all run-completed quests to PlayerPrefs so they are excluded from future runs.
    // Called by GameManager.CompleteCurrentLevel() after the level-complete flow finishes.
    public void FinalizeRunCompletedQuests()
    {
        foreach (var q in activeQuests)
        {
            if (q == null) continue;
            if (!runCompleted.Contains(q.questId)) continue;

            MarkQuestPermanentlyCompleted(q.questId);
        }
    }

    // Wipes all PlayerPrefs quest completion keys and regenerates quests from scratch.
    public void ResetAllQuestProgress()
    {
        for (int i = 0; i < availableQuests.Count; i++)
        {
            QuestDefinition q = availableQuests[i];
            if (q == null || string.IsNullOrWhiteSpace(q.questId))
                continue;

            PlayerPrefs.DeleteKey(GetPermanentQuestKey(q.questId));
        }

        PlayerPrefs.Save();

        permanentlyCompleted.Clear();
        runCompleted.Clear();
        progress.Clear();
        pendingRewards.Clear();
        collectedRewards.Clear();
        activeQuests.Clear();

        GenerateQuestsForCurrentLevel();

        PlayerUI playerUI = FindObjectOfType<PlayerUI>(true);
        if (playerUI != null)
            playerUI.RefreshQuestDisplay();
    }
}
