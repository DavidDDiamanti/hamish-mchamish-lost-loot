using UnityEngine;

// UpgradeSave: static helper that reads and writes shop upgrade levels to PlayerPrefs,
// namespaced via GameProfileContext.GetSaveKey() so each profile has independent upgrades.
// TryAddLevel() enforces the maxLevel cap (0 = unlimited).
// ClearAllUpgrades() calls PlayerPrefs.DeleteAll():use with caution as it wipes all saved data.
public static class UpgradeSave
{
    private const string Prefix = "UPG_";
    private static string Key(string id) => GameProfileContext.GetSaveKey(Prefix + id);

    public static int GetLevel(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return 0;
        return PlayerPrefs.GetInt(Key(id), 0);
    }

    public static void SetLevel(string id, int level)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        PlayerPrefs.SetInt(Key(id), Mathf.Max(0, level));
        PlayerPrefs.Save();
    }

    // Returns false without modifying state if the item is already at maxLevel.
    // maxLevel 0 means unlimited.
    public static bool TryAddLevel(string id, int maxLevel)
    {
        int lvl = GetLevel(id);

        if (maxLevel > 0 && lvl >= maxLevel) return false;

        SetLevel(id, lvl + 1);
        return true;
    }

    public static bool IsOwned(string id) => GetLevel(id) > 0;
    public static void SetOwned(string id, bool owned) => SetLevel(id, owned ? 1 : 0);

    public static void ClearOwned(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        PlayerPrefs.DeleteKey(Key(id));
        PlayerPrefs.Save();
    }

    // WARNING: deletes ALL PlayerPrefs, including gold and cosmetics.
    public static void ClearAllUpgrades()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
