using System;

// GameEvents: static event bus for loosely coupled communication between game systems.
// Raised by gameplay objects; subscribed to by QuestManager, PlayerUI, and other listeners.
// Using static events avoids requiring direct references between unrelated MonoBehaviours.
public static class GameEvents
{
    public static event Action EnemyDefeated;
    public static event Action<int> StreakChanged;
    public static event Action ChestOpened;
    public static event Action<int> GoldGained;
    public static event Action QuestCompleted;
    public static System.Action QuestProgressChanged;
    public static System.Action QuestCollected;

    public static void RaiseQuestProgressChanged() => QuestProgressChanged?.Invoke();
    public static void RaiseQuestCollected() => QuestCollected?.Invoke();
    public static void RaiseEnemyDefeated() => EnemyDefeated?.Invoke();
    public static void RaiseStreakChanged(int streak) => StreakChanged?.Invoke(streak);
    public static void RaiseChestOpened() => ChestOpened?.Invoke();
    public static void RaiseGoldGained(int amt) => GoldGained?.Invoke(amt);
    public static void RaiseQuestCompleted() => QuestCompleted?.Invoke();
}
