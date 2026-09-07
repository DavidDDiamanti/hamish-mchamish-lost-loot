using UnityEngine;

// GameProfileContext: static class that namespaces all PlayerPrefs keys by the active profile ID.
// All managers that persist data (GoldManager, GemsManager, QuestManager, CosmeticSave, UpgradeSave)
// call GetSaveKey() to get a profile-prefixed key, so base and custom CSV profiles never share data.
// ActiveProfileId persists in PlayerPrefs under "ACTIVE_PROFILE_ID".
// IsBaseProfile is checked by EntitySpawner and GameManager to choose the correct question source.
public static class GameProfileContext
{
    private const string ActiveProfileKey = "ACTIVE_PROFILE_ID";
    public const string BaseProfileId = "BASE";

    public static string ActiveProfileId
    {
        get => PlayerPrefs.GetString(ActiveProfileKey, BaseProfileId);
        set
        {
            PlayerPrefs.SetString(ActiveProfileKey, string.IsNullOrWhiteSpace(value) ? BaseProfileId : value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsBaseProfile => ActiveProfileId == BaseProfileId;

    public static string GetSaveKey(string rawKey)
    {
        return $"{ActiveProfileId}_{rawKey}";
    }

    public static void SetBaseProfile()
    {
        ActiveProfileId = BaseProfileId;
    }

    public static void SetCustomProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            profileId = "CUSTOM";

        ActiveProfileId = profileId;
    }

    public static void ClearActiveProfile()
    {
        PlayerPrefs.DeleteKey(ActiveProfileKey);
        PlayerPrefs.Save();
    }
}
