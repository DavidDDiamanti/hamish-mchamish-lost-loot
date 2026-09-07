using UnityEngine;

// QuestDefinition: ScriptableObject defining a single quest's parameters.
// type determines which GameEvent drives progress (DefeatEnemies, ReachStreak, OpenChests, CollectGold).
// baseGoldReward/baseGemReward are multiplied by the current difficulty in QuestManager.CheckComplete().
// availableInLevelN flags let designers restrict quests to specific levels.
public enum QuestType
{
    DefeatEnemies,
    ReachStreak,
    OpenChests,
    CollectGold
}

[CreateAssetMenu(menuName = "Quests/Quest Definition")]
public class QuestDefinition : ScriptableObject
{
    public string questId;
    public string title;
    [TextArea] public string description;

    public QuestType type;

    public int targetAmount = 1;
    public int baseGoldReward = 10;
    public int baseGemReward = 0;

    [Header("Level Availability")]
    public bool availableInLevel1 = true;
    public bool availableInLevel2 = true;
    public bool availableInLevel3 = true;

    public bool IsAvailableForLevel(int level)
    {
        return level switch
        {
            1 => availableInLevel1,
            2 => availableInLevel2,
            3 => availableInLevel3,
            _ => false
        };
    }
}
