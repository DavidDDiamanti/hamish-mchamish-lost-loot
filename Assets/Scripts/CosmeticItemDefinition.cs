using UnityEngine;

// CosmeticItemDefinition: ScriptableObject defining a single cosmetic item.
// lootboxOnly items are never shown in the gem shop; they can only be obtained from lootboxes
// awarded by GameManager.GetEligibleCosmeticLootboxDrops() on first level completion.
// ColourVariant items require their requiredBaseItem to be owned before they can be purchased.
public enum CosmeticCategory
{
    Shirt,
    Kilt
}

public enum CosmeticVariantType
{
    Base,
    ColourVariant
}

[CreateAssetMenu(menuName = "Cosmetics/Cosmetic Item", fileName = "CosmeticItem_")]
public class CosmeticItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Cosmetic Data")]
    public CosmeticCategory category;
    public CosmeticVariantType variantType = CosmeticVariantType.Base;

    [Tooltip("For colour variants, set this to the base cosmetic that must already be owned.")]
    public CosmeticItemDefinition requiredBaseItem;

    [Header("Unlocking")]
    [Tooltip("If enabled, this base cosmetic can only be unlocked from a cosmetic lootbox.")]
    public bool lootboxOnly = false;

    [Header("Visuals")]
    public Sprite cosmeticSprite;
    public Color tint = Color.white;

    [Header("Cost")]
    public int gemPrice = 10;
}
