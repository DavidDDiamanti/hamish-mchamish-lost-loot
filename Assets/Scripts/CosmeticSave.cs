using UnityEngine;

// CosmeticSave: static helper that reads and writes cosmetic ownership, equipped state,
// and pending lootbox count to PlayerPrefs, all namespaced via GameProfileContext.GetSaveKey().
// Owned/equipped state is per-profile so base and custom profiles have independent wardrobes.
public static class CosmeticSave
{
    private const string OwnedPrefix = "COSMETIC_OWNED_";
    private const string EquippedPrefix = "COSMETIC_EQUIPPED_";
    private const string PendingLootboxKey = "COSMETIC_PENDING_LOOTBOXES";
    private static string OwnedKey(string cosmeticId) => GameProfileContext.GetSaveKey(OwnedPrefix + cosmeticId);
    private static string EquippedKey(CosmeticCategory category) => GameProfileContext.GetSaveKey(EquippedPrefix + category);
    private static string PendingLootboxProfileKey => GameProfileContext.GetSaveKey(PendingLootboxKey);

    public static bool IsOwned(string cosmeticId)
    {
        if (string.IsNullOrEmpty(cosmeticId)) return false;
        return PlayerPrefs.GetInt(OwnedKey(cosmeticId), 0) == 1;
    }

    public static void SetOwned(string cosmeticId, bool owned = true)
    {
        if (string.IsNullOrEmpty(cosmeticId)) return;
        PlayerPrefs.SetInt(OwnedKey(cosmeticId), owned ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static string GetEquippedId(CosmeticCategory category)
    {
        return PlayerPrefs.GetString(EquippedKey(category), string.Empty);
    }

    public static void Equip(CosmeticCategory category, string cosmeticId)
    {
        PlayerPrefs.SetString(EquippedKey(category), cosmeticId ?? string.Empty);
        PlayerPrefs.Save();
    }

    public static void Unequip(CosmeticCategory category)
    {
        PlayerPrefs.SetString(EquippedKey(category), string.Empty);
        PlayerPrefs.Save();
    }

    public static bool IsEquipped(CosmeticCategory category, string cosmeticId)
    {
        return GetEquippedId(category) == cosmeticId;
    }

    public static int GetPendingLootboxes()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(PendingLootboxProfileKey, 0));
    }

    public static void SetPendingLootboxes(int amount)
    {
        PlayerPrefs.SetInt(PendingLootboxProfileKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    public static void AddPendingLootboxes(int amount)
    {
        SetPendingLootboxes(GetPendingLootboxes() + amount);
    }

    public static void ClearAllCosmetics()
    {
    }
}
