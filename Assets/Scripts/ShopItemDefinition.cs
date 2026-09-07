using UnityEngine;

// ShopItemDefinition: ScriptableObject defining a permanent upgrade in the lobby shop.
// type determines how the upgrade is applied in GameManager.ApplyPermanentStatUpgrades().
// maxLevel: 1 = one-time purchase, 0 = unlimited, >1 = levelled upgrade.
// Price scales per level: cost = (1 + currentLevel) * price.
public enum UpgradeType
{
    CursorType,
    PlayerStat,
    UnlockFeature
}

public enum PlayerStatType
{
    MoveSpeed,
    DigPower,
    Damage,
    MaxHealth,
    DigSpeed
}

public enum UnlockFeatureType
{
    None,
    AutoClicker
}

[CreateAssetMenu(menuName = "Shop/Shop Item Definition", fileName = "ShopItem_")]
public class ShopItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Must be unique. Used for saving ownership/levels.")]
    public string id;

    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Cost")]
    public int price;

    [Header("Type")]
    public UpgradeType type;

    [Header("Purchase Rules")]
    [Tooltip("1 = one-time purchase. 0 = unlimited. >1 = max level.")]
    public int maxLevel = 1;

    [Header("CursorType")]
    public GameObject cursorPrefabUpgrade;

    [Header("PlayerStat")]
    public PlayerStatType statType;
    public float statAmountPerLevel = 1f;

    [Header("UnlockFeature")]
    public UnlockFeatureType featureType;
}
