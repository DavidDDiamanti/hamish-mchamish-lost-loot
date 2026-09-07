using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

// GameManager: singleton that persists across scenes and controls all top-level game state.
// Drives the MainScene lifecycle: scene load -> PrepareLevel -> ForceFreezeForStartGate ->
// PressStartLevel (player action) -> gameplay -> CompleteCurrentLevel -> FinishLevel.
// Coordinates pausing across Player, enemies, and cursor particles via PauseMovement().
// Gold and gems are delegated to GoldManager/GemsManager (run-commit model).
// Applies permanent shop upgrades to the Player after each scene load.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private string mainSceneName  = "MainScene";

    [Header("Game States")]
    [SerializeField] private bool playerIsInQuizUI = false;
    [SerializeField] private bool gameIsPaused = false;
    [SerializeField] private bool gameIsOver = false;

    [Header("Level Management")]
    [SerializeField] private int chestCount = 10;
    [SerializeField] private int difficulty = 2;
    [SerializeField] private bool levelComplete = false;

    [Header("Upgrades")]
    [SerializeField] private ShopCatalog shopCatalog;
    private string upgradedCursorUpgradeId = "cursor_upgrade_v1";
    private GameObject upgradedCursorInstance;

    [Header("Level Complete Bonus Rewards")]
    [SerializeField] private int perfectStreakTarget = 10;
    [SerializeField] private int perfectStreakBaseGoldReward = 250;
    [SerializeField] private int firstClearBaseGoldReward = 100;
    [SerializeField] private int perfectStreakBaseGemReward = 50;
    [SerializeField] private int firstClearBaseGemReward = 25;

    [Header("Question Banks By Level")]
    [SerializeField] private QuestionBank level1QuestionBank;
    [SerializeField] private QuestionBank level2QuestionBank;
    [SerializeField] private QuestionBank level3QuestionBank;

    [Header("Cosmetic Lootboxes")]
    [SerializeField] private CosmeticCatalog cosmeticCatalog;
    [SerializeField] private int cosmeticLootboxesPerFirstClear = 1;

    private string MaxUnlockedLevelKeyProfile => GameProfileContext.GetSaveKey(MaxUnlockedLevelKey);
    private string NewlyUnlockedLevelKeyProfile => GameProfileContext.GetSaveKey(NewlyUnlockedLevelKey);

    private string GetLevelCompletedKey(int level)
    {
        return GameProfileContext.GetSaveKey(LevelCompletedKeyPrefix + level);
    }

    public QuestionBank GetQuestionBankForCurrentLevel()
    {
        switch (currentLevel)
        {
            case 1: return level1QuestionBank;
            case 2: return level2QuestionBank;
            case 3: return level3QuestionBank;
            default: return level1QuestionBank;
        }
    }

    private const string LevelCompletedKeyPrefix = "LEVEL_COMPLETED_";
    private const string NewlyUnlockedLevelKey = "NEWLY_UNLOCKED_LEVEL";

    private PlayerUI playerUI;
    private Player player;
    private GoldManager goldManager;
    private GemsManager gemsManager;
    private QuestManager questManager;
    private EntitySpawner entitySpawner;
    private GameOverUI gameOverUI;
    private CursorProximityParticles2 cursorParticles;
    private CursorProximityParticles cursorParticlesUpgraded;
    private LevelStartUI levelStartUI;
    private LevelCompleteUI levelCompleteUI;
    private PauseMenuUI pauseMenuUI;

    private const string MaxUnlockedLevelKey = "MAX_UNLOCKED_LEVEL";
    private int maxUnlockedLevel = 1;
    private int currentLevel = 1;
    private int streak = 0;
    private bool levelStarted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        maxUnlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt(MaxUnlockedLevelKeyProfile, 1), 1, 3);
        currentLevel = Mathf.Clamp(currentLevel, 1, maxUnlockedLevel);
        Debug.Log("GameManager Awake: Max Unlocked Level = " + maxUnlockedLevel + ", Current Level = " + currentLevel);
    }

    // Must be called after every scene load as FindObjectOfType results are scene-bound and go stale.
    private void RebindSceneReferences()
    {
        player = FindObjectOfType<Player>();
        playerUI = FindObjectOfType<PlayerUI>(true);
        goldManager = FindObjectOfType<GoldManager>();
        gemsManager = FindObjectOfType<GemsManager>();
        entitySpawner = FindObjectOfType<EntitySpawner>();
        gameOverUI = FindObjectOfType<GameOverUI>(true);
        cursorParticles = FindObjectOfType<CursorProximityParticles2>(true);
        cursorParticlesUpgraded = FindObjectOfType<CursorProximityParticles>(true);
        levelStartUI = FindObjectOfType<LevelStartUI>(true);
        pauseMenuUI = FindObjectOfType<PauseMenuUI>(true);
        levelCompleteUI = FindObjectOfType<LevelCompleteUI>(true);
        questManager = FindObjectOfType<QuestManager>();
    }

    // Called when the active profile changes (e.g. switching between BASE and a custom CSV profile).
    // Reloads level progress and currency state scoped to the new profile's PlayerPrefs keys.
    public void ReloadForActiveProfile()
    {
        maxUnlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt(MaxUnlockedLevelKeyProfile, 1), 1, 3);
        currentLevel = Mathf.Clamp(1, 1, maxUnlockedLevel);

        GoldManager.Instance?.ReloadForActiveProfile();
        GemsManager.Instance?.ReloadForActiveProfile();

        Debug.Log($"GameManager reloaded for profile '{GameProfileContext.ActiveProfileId}'. MaxUnlockedLevel = {maxUnlockedLevel}");
    }

    private ShopItemDefinition GetShopItemById(string id)
    {
        if (shopCatalog == null || shopCatalog.items == null) return null;

        for (int i = 0; i < shopCatalog.items.Count; i++)
        {
            var it = shopCatalog.items[i];
            if (it != null && it.id == id)
                return it;
        }
        return null;
    }

    private void SpawnUpgradedCursorIfOwned()
    {
        Debug.Log("Checking if upgraded cursor should be spawned...");
        if (!UpgradeSave.IsOwned(upgradedCursorUpgradeId)) return;
        Debug.Log("Upgraded cursor is owned, attempting to spawn...");

        if (upgradedCursorInstance != null)
        {
            Destroy(upgradedCursorInstance);
            upgradedCursorInstance = null;
        }

        var item = GetShopItemById(upgradedCursorUpgradeId);
        if (item == null)
        {
            Debug.LogWarning($"GameManager: No ShopItemDefinition found for id '{upgradedCursorUpgradeId}'.");
            return;
        }

        if (item.cursorPrefabUpgrade == null)
        {
            Debug.LogWarning($"GameManager: Shop item '{item.id}' has no cursorPrefabUpgrade assigned.");
            return;
        }

        upgradedCursorInstance = Instantiate(
            item.cursorPrefabUpgrade,
            Vector3.zero,
            Quaternion.identity,
            null
        );
    }

    // Iterates the shop catalog and applies all purchased PlayerStat upgrades to the player.
    // Called on each MainScene load so permanent upgrades persist across level restarts and sessions.
    private void ApplyPermanentStatUpgrades()
    {
        if (player == null) return;
        if (shopCatalog == null || shopCatalog.items == null) return;

        foreach (var item in shopCatalog.items)
        {
            if (item == null) continue;
            if (item.type != UpgradeType.PlayerStat) continue;

            int level = UpgradeSave.GetLevel(item.id);
            if (level <= 0) continue;

            float total = item.statAmountPerLevel * level;

            switch (item.statType)
            {
                case PlayerStatType.MoveSpeed:
                    player.IncreaseMovementSpeed(total);
                    break;

                case PlayerStatType.DigPower:
                    player.AddDigPower(total);
                    break;

                case PlayerStatType.Damage:
                    player.IncreaseDamage(total);
                    break;

                case PlayerStatType.MaxHealth:
                    player.IncreaseMaxHealth(Mathf.RoundToInt(total));
                    break;

                case PlayerStatType.DigSpeed:
                    player.IncreaseDigSpeed(total);
                    break;
            }
        }

        playerUI.ShowPickupNotification("");
    }

    private void ApplyPermanentFeatureUpgrades()
    {
        if (player == null) return;
        if (shopCatalog == null || shopCatalog.items == null) return;

        player.SetAutoClickEnabled(false);

        foreach (var item in shopCatalog.items)
        {
            if (item == null) continue;
            if (item.type != UpgradeType.UnlockFeature) continue;
            if (!UpgradeSave.IsOwned(item.id)) continue;

            switch (item.featureType)
            {
                case UnlockFeatureType.AutoClicker:
                    player.SetAutoClickEnabled(true);
                    break;
            }
        }
    }

    public void setDifficulty(int newDifficulty)
    {
        difficulty = newDifficulty;
    }

    public int GetCurrentLevel() => currentLevel;

    public int GetMaxUnlockedLevel() => maxUnlockedLevel;

    public bool IsLevelUnlocked(int level)
    {
        return level >= 1 && level <= maxUnlockedLevel;
    }

    public void SetCurrentLevel(int newLevel)
    {
        currentLevel = Mathf.Clamp(newLevel, 1, maxUnlockedLevel);
    }

    public void AddLevelProgress()
    {
        UnlockNextLevelIfNeeded();
    }

    private bool HasCompletedLevelBefore(int level)
    {
        return PlayerPrefs.GetInt(GetLevelCompletedKey(level), 0) == 1;
    }

    private void MarkLevelAsCompleted(int level)
    {
        PlayerPrefs.SetInt(GetLevelCompletedKey(level), 1);
        PlayerPrefs.Save();
    }

    private struct LevelBonusResult
    {
        public int goldReward;
        public int gemReward;
        public bool gotPerfectStreakReward;
        public bool gotFirstClearReward;
        public int difficultyMultiplier;
        public string summaryText;
    }

    // Calculates end-of-level bonus gold/gems based on perfect streak and first-clear status.
    // All rewards are scaled by the difficulty multiplier. Returns a summary string for the UI.
    private LevelBonusResult CalculateLevelCompletionBonuses()
    {
        LevelBonusResult result = new LevelBonusResult();
        result.difficultyMultiplier = difficulty;

        bool perfectStreakAchieved = streak >= perfectStreakTarget;
        bool firstClear = !HasCompletedLevelBefore(currentLevel);

        int totalGold = 0;
        int totalGems = 0;

        List<string> lines = new List<string>();
        lines.Add($"Difficulty multiplier: x{result.difficultyMultiplier}");

        if (perfectStreakAchieved)
        {
            int reward = perfectStreakBaseGoldReward * result.difficultyMultiplier;
            totalGold += reward;
            result.gotPerfectStreakReward = true;
            int gemReward = perfectStreakBaseGemReward * result.difficultyMultiplier;
            totalGems += gemReward;
            lines.Add($"Perfect streak ({perfectStreakTarget}): +{reward} gold & {gemReward} gems");
        }

        if (firstClear)
        {
            int reward = firstClearBaseGoldReward * result.difficultyMultiplier;
            totalGold += reward;
            result.gotFirstClearReward = true;
            int gemReward = firstClearBaseGemReward * result.difficultyMultiplier;
            totalGems += gemReward;
            lines.Add($"First clear reward: +{reward} gold & {gemReward} gems");
        }

        result.goldReward = totalGold;
        result.gemReward = totalGems;

        if (totalGold <= 0 && totalGems <= 0)
        {
            lines.Add("No extra level bonuses earned.");
        }
        else
        {
            if (totalGold > 0)
                lines.Add($"Total bonus gold: +{totalGold}");

            if (totalGems > 0)
                lines.Add($"Total bonus gems: +{totalGems}");
        }

        result.summaryText = string.Join("\n", lines);
        return result;
    }

    private void UnlockNextLevelIfNeeded()
    {
        int nextLevel = currentLevel + 1;

        if (nextLevel > 3) return;

        if (nextLevel > maxUnlockedLevel)
        {
            maxUnlockedLevel = nextLevel;
            PlayerPrefs.SetInt(MaxUnlockedLevelKeyProfile, maxUnlockedLevel);
            PlayerPrefs.SetInt(NewlyUnlockedLevelKeyProfile, maxUnlockedLevel);
            PlayerPrefs.Save();

            Debug.Log("Unlocked level " + maxUnlockedLevel);
        }
    }

    public void setChestCount(int newChestCount)
    {
        chestCount = newChestCount;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // Entry point for all scene transitions. On MainScene load: spawns entities, applies upgrades,
    // shows the LevelStartUI gate, and freezes all movement until the player presses Start.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindSceneReferences();

        if (scene.name == mainSceneName)
        {
            gameIsOver = false;
            playerIsInQuizUI = false;
            levelStarted = false;
            levelComplete = false;
            streak = 0;

            QuestManager.Instance.GenerateQuestsForCurrentLevel();
            PrepareLevel();

            ForceFreezeForStartGate();
            levelStartUI.Show();
            SpawnUpgrades();
            RebindSceneReferences();
            PauseMovement(true);
        }
    }

    private void SpawnUpgrades()
    {
        SpawnUpgradedCursorIfOwned();
    }

    // Spawns entities, initialises UI, and applies shop upgrades.
    // Hides the player action area until PressStartLevel() is called.
    private void PrepareLevel()
    {
        if (entitySpawner != null)
            entitySpawner.InitializeLevel();

        if (playerUI != null)
            playerUI.SetUI();

        ApplyPermanentStatUpgrades();
        ApplyPermanentFeatureUpgrades();

        if (player != null)
        {
            player.HideActionArea();
            player.SetLevelStarted(false);
        }
    }

    private void ForceFreezeForStartGate()
    {
        gameIsPaused = true;
        PauseMovement(true);
    }

    // Called by the LevelStartUI Start button. Unfreezes the game and reveals the player action area.
    public void PressStartLevel()
    {
        Debug.Log("Start Level button pressed.");
        if (levelStarted) return;
        Debug.Log("Starting level...");
        if (SceneManager.GetActiveScene().name != mainSceneName) return;

        levelStartUI.Hide();
        playerUI.Show();

        levelStarted = true;

        gameIsPaused = false;
        PauseMovement(false);

        if (player != null)
            player.ShowActionArea();

        player.SetLevelStarted(true);
        playerUI.setLevelStarted(true);
    }

    public bool HasLevelStarted() => levelStarted;

    public bool IsLevelComplete() => levelComplete;

    public void StartLevel()
    {
        goldManager.BeginRun();
        player.ShowActionArea();
        entitySpawner.InitializeLevel();
        playerUI.SetUI();
    }

    private void increaseStreak()
    {
        streak++;
        GameEvents.RaiseStreakChanged(streak);
        playerUI.UpdateStreak(streak);
    }

    public void resetStreak()
    {
        streak = 0;
        GameEvents.RaiseStreakChanged(streak);
        playerUI.UpdateStreak(streak);
    }

    // Called by ChestBehaviour after QuizUI resolves. Awards gold/gems scaled by streak and difficulty
    // on a correct answer, or resets streak on a wrong answer. Always decrements chest count.
    public void chestOpened(bool answeredCorrectly, int goldReward)
    {
        if (playerUI != null)
            playerUI.ShowChestResultNotification(answeredCorrectly);

        if (answeredCorrectly)
        {
            GameEvents.RaiseChestOpened();
            increaseStreak();
            int reward = goldReward * streak * (difficulty);
            goldManager.AddGold(reward);
            gemsManager.AddGems(3 * streak * (difficulty));
        }
        else
        {
            resetStreak();
            int reward = goldReward;
            goldManager.AddGold(reward);
        }

        DecrementUnopenedChestsCount();
    }

    public int GetStreak() => streak;

    public void addGold(int amount)
    {
        goldManager.AddGold(amount);
    }

    public void EnemyDied()
    {
        goldManager.AddGold(5 * difficulty);
    }

    // Decrements chest count. When it reaches zero, triggers CompleteCurrentLevel().
    public void DecrementUnopenedChestsCount()
    {
        chestCount = Mathf.Max(0, chestCount - 1);

        if (chestCount == 0)
        {
            CompleteCurrentLevel(false);
        }
        else if (playerUI != null)
        {
            playerUI.UpdateRemainingChests(chestCount);
        }
    }

    public void ReturnToLobby()
    {
        gameIsPaused = false;
        SceneManager.LoadScene(lobbySceneName);
    }

    public void QuitLevel()
    {
        goldManager.RollbackRun();
        ReturnToLobby();
    }

    public void FinishLevel()
    {
        UnlockNextLevelIfNeeded();
        goldManager?.CommitRun();
        gemsManager?.CommitRun();
        ReturnToLobby();
    }

    public int GetNumberOfUnopenedChests() => chestCount;

    public bool IsGamePaused() => gameIsPaused;

    public void SetGamePaused(bool paused)
    {
        Debug.Log("Attempting to set game paused to " + paused);
        if (paused == gameIsPaused) return;
        gameIsPaused = paused;
        PauseMovement(paused);
    }

    public bool CanPlayerPause()
    {
        Debug.Log("Game is paused: " + gameIsPaused);
        if (gameIsOver || playerIsInQuizUI || gameIsPaused || levelComplete)
        {
            Debug.Log("Cannot pause: " +
                (gameIsOver ? "Game is over. " : "") +
                (playerIsInQuizUI ? "Player is in quiz UI. " : "") +
                (gameIsPaused ? "Game is already paused. " : ""));
            return false;
        }
        Debug.Log("Player can pause the game.");
        return true;
    }

    // Propagates pause state to the player, cursor particles, all active enemies, and the action area.
    private void PauseMovement(bool pause)
    {
        Debug.Log("Setting game movement paused: " + pause);

        if (player != null)
            player.SetCantMove(pause);

        if (cursorParticles != null)
        {
            Debug.Log("Setting cursorParticles paused: " + pause);
            cursorParticles.SetPaused(pause);
        }
        else
        {
            Debug.Log("cursorParticles reference is null. Cannot set paused state.");
        }

        if (cursorParticlesUpgraded != null)
        {
            cursorParticlesUpgraded.SetPaused(pause);
        }
        else
        {
            Debug.Log("cursorParticlesUpgraded reference is null. Cannot set paused state.");
        }

        PauseEnemyMovement(pause);

        if (player != null)
        {
            if (pause) player.HideActionArea();
            else       player.ShowActionArea();
        }
    }

    public void PauseEnemyMovement(bool pause)
    {
        var flyEnemies = FindObjectsOfType<FlyEnemy>();
        var antEnemies = FindObjectsOfType<AntEnemy>();
        foreach (var enemy in flyEnemies)
            enemy.SetPaused(pause);
        foreach (var enemy in antEnemies)
            enemy.SetPaused(pause);
    }

    public bool IsGameOver() => gameIsOver;

    public void SetGameOver(bool over)
    {
        gameIsOver = over;
        PauseMovement(over);
        gameOverUI.ShowGameOver(over);
        goldManager.RollbackRun();
        gemsManager.RollbackRun();
    }

    public bool IsPlayerInQuizUI() => playerIsInQuizUI;

    public void SetPlayerInQuizUI(bool inQuiz)
    {
        playerIsInQuizUI = inQuiz;

        if (levelComplete) return;

        PauseMovement(inQuiz);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        RebindSceneReferences();
        PauseMovement(true);
    }

    public int GetCurrentDifficulty()
    {
        return difficulty;
    }

    public int GetNewlyUnlockedLevel()
    {
        return PlayerPrefs.GetInt(NewlyUnlockedLevelKeyProfile, 0);
    }

    public bool HasUnacknowledgedUnlockedLevel()
    {
        int newlyUnlocked = GetNewlyUnlockedLevel();
        return newlyUnlocked > 0;
    }

    public void AcknowledgeUnlockedLevel(int level)
    {
        int newlyUnlocked = GetNewlyUnlockedLevel();

        if (level == newlyUnlocked)
        {
            PlayerPrefs.DeleteKey(NewlyUnlockedLevelKeyProfile);
            PlayerPrefs.Save();
        }
    }

    public void ResetGameData()
    {
        Debug.LogWarning("ResetGameData is temporarily disabled until a profile-specific reset is implemented.");

        maxUnlockedLevel = 1;
        currentLevel = 1;

        chestCount = 10;
        difficulty = 2;
        levelComplete = false;
        levelStarted = false;
        streak = 0;

        gameIsPaused = false;
        gameIsOver = false;
        playerIsInQuizUI = false;

        Debug.Log("All game data reset. Max unlocked level set to 1, current level set to 1.");
    }

    // Returns cosmetics that are lootbox-only base variants not yet owned by the player.
    // Used to decide whether to award a lootbox and to pick a random reward on open.
    private List<CosmeticItemDefinition> GetEligibleCosmeticLootboxDrops()
    {
        List<CosmeticItemDefinition> eligible = new List<CosmeticItemDefinition>();

        if (cosmeticCatalog == null || cosmeticCatalog.items == null)
            return eligible;

        foreach (var item in cosmeticCatalog.items)
        {
            if (item == null) continue;
            if (item.variantType != CosmeticVariantType.Base) continue;
            if (!item.lootboxOnly) continue;
            if (CosmeticSave.IsOwned(item.id)) continue;

            eligible.Add(item);
        }

        return eligible;
    }

    public bool HasAnyCosmeticLootboxDropsRemaining()
    {
        return GetEligibleCosmeticLootboxDrops().Count > 0;
    }

    public int GetPendingCosmeticLootboxCount()
    {
        return CosmeticSave.GetPendingLootboxes();
    }

    public void GrantCosmeticLootboxes(int amount)
    {
        if (amount <= 0) return;
        if (!HasAnyCosmeticLootboxDropsRemaining()) return;

        CosmeticSave.AddPendingLootboxes(amount);
        Debug.Log($"Granted {amount} cosmetic lootbox(es). Pending = {CosmeticSave.GetPendingLootboxes()}");
    }

    // Consumes one pending lootbox, picks a random eligible cosmetic, and marks it owned.
    // If no eligible cosmetics remain, flushes all stale pending boxes instead.
    public CosmeticItemDefinition OpenCosmeticLootbox()
    {
        int pending = CosmeticSave.GetPendingLootboxes();
        if (pending <= 0)
        {
            Debug.Log("No pending cosmetic lootboxes to open.");
            return null;
        }

        List<CosmeticItemDefinition> eligible = GetEligibleCosmeticLootboxDrops();

        if (eligible.Count == 0)
        {
            CosmeticSave.SetPendingLootboxes(0);
            Debug.Log("No eligible cosmetic lootbox drops remain.");
            return null;
        }

        CosmeticItemDefinition unlocked = eligible[Random.Range(0, eligible.Count)];
        CosmeticSave.SetOwned(unlocked.id, true);
        CosmeticSave.AddPendingLootboxes(-1);

        Debug.Log($"Lootbox unlocked cosmetic: {unlocked.displayName}");
        return unlocked;
    }

    // Handles the full end-of-level sequence: calculates bonuses, awards lootbox on first clear,
    // finalises quests, shows LevelCompleteUI, auto-collects quest rewards, and freezes movement.
    private void CompleteCurrentLevel(bool forceTreatAsFirstClear = false)
    {
        if (levelComplete) return;

        levelComplete = true;
        gameIsPaused = true;

        if (pauseMenuUI != null)
            pauseMenuUI.HideNotification();

        if (playerUI != null)
            playerUI.Hide();

        bool wasAlreadyCompleted = HasCompletedLevelBefore(currentLevel);

        if (forceTreatAsFirstClear && wasAlreadyCompleted)
        {
            PlayerPrefs.SetInt(GetLevelCompletedKey(currentLevel), 0);
            PlayerPrefs.Save();

            Debug.Log($"[Lootbox Debug] Forced level {currentLevel} to count as not previously completed.");
        }

        LevelBonusResult bonusResult = CalculateLevelCompletionBonuses();
        string summaryText = bonusResult.summaryText;

        if (bonusResult.goldReward > 0 && goldManager != null)
            goldManager.AddGold(bonusResult.goldReward);

        if (bonusResult.gemReward > 0 && gemsManager != null)
            gemsManager.AddGems(bonusResult.gemReward);

        bool hasDropsRemaining = HasAnyCosmeticLootboxDropsRemaining();
        bool awardedLootbox = false;

        Debug.Log($"[Lootbox Debug] CurrentLevel={currentLevel}");
        Debug.Log($"[Lootbox Debug] HasCompletedLevelBefore={HasCompletedLevelBefore(currentLevel)}");
        Debug.Log($"[Lootbox Debug] bonusResult.gotFirstClearReward={bonusResult.gotFirstClearReward}");
        Debug.Log($"[Lootbox Debug] cosmeticCatalog assigned={(cosmeticCatalog != null)}");
        Debug.Log($"[Lootbox Debug] Eligible lootbox drops remaining={GetEligibleCosmeticLootboxDrops().Count}");

        if (bonusResult.gotFirstClearReward && hasDropsRemaining)
        {
            GrantCosmeticLootboxes(cosmeticLootboxesPerFirstClear);
            awardedLootbox = true;
            summaryText += "\nOne lootbox earned!";
        }

        if (QuestManager.Instance != null)
            QuestManager.Instance.FinalizeRunCompletedQuests();

        MarkLevelAsCompleted(currentLevel);

        if (levelCompleteUI != null)
            levelCompleteUI.Show(summaryText);

        playerUI.Hide();

        if (pauseMenuUI != null)
            pauseMenuUI.CollectAllCompletedQuests();

        Debug.Log(
            $"[Lootbox Debug] Level completed. FirstClear={bonusResult.gotFirstClearReward}, " +
            $"PerfectStreak={bonusResult.gotPerfectStreakReward}, " +
            $"HasDropsRemaining={hasDropsRemaining}, " +
            $"LootboxAwarded={awardedLootbox}, " +
            $"PendingLootboxes={CosmeticSave.GetPendingLootboxes()}"
        );

        PauseMovement(true);
    }
}
