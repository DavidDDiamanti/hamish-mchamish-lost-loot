using TMPro;
using UnityEngine;
using UnityEngine.UI;

// CosmeticShopSlotUI: one row in the cosmetic gem shop.
// Single action button toggles between Buy, Equip, and Unequip depending on owned/equipped state.
// lootboxOnly base items are shown but cannot be purchased; they display "Lootbox Only".
// ColourVariant items are locked until the requiredBaseItem is owned.
// Buy() auto-equips the item immediately after purchase.
public class CosmeticShopSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;

    [Header("Single Action Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    private CosmeticItemDefinition item;
    private GemsManager gemsManager;
    private PlayerCosmetics playerCosmetics;
    private System.Action onChanged;

    public void Init(
        CosmeticItemDefinition def,
        GemsManager gems,
        PlayerCosmetics cosmetics,
        System.Action changedCallback)
    {
        item = def;
        gemsManager = gems;
        playerCosmetics = cosmetics;
        onChanged = changedCallback;

        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
            iconImage.color = item.tint;
        }

        if (nameText != null) nameText.text = item.displayName;

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionPressed);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (item == null) return;

        bool owned = CosmeticSave.IsOwned(item.id);
        bool equipped = CosmeticSave.IsEquipped(item.category, item.id);
        bool unlockedForPurchase = CanBePurchased();

        if (descText != null)
        {
            string extra = "";

            if (item.lootboxOnly && item.variantType == CosmeticVariantType.Base && !owned)
                extra += "\nUnlock from cosmetic lootbox";

            if (item.variantType == CosmeticVariantType.ColourVariant && item.requiredBaseItem != null)
                extra += $"\nRequires base: {item.requiredBaseItem.displayName}";

            if (owned)
                extra += "\nOwned";

            if (equipped)
                extra += "\nEquipped";

            descText.text = item.description + extra;
        }

        if (actionButton == null || actionButtonText == null)
            return;

        if (!owned)
        {
            if (!unlockedForPurchase)
            {
                actionButton.interactable = false;
                actionButtonText.text = item.lootboxOnly ? "Lootbox Only" : "Locked";
            }
            else
            {
                actionButton.interactable = true;
                actionButtonText.text = $"Buy - {item.gemPrice} Gems";
            }
        }
        else
        {
            actionButton.interactable = true;
            actionButtonText.text = equipped ? "Unequip" : "Equip";
        }
    }

    // Returns false for lootboxOnly base items (not purchasable in the shop) and for
    // colour variants whose required base item is not yet owned.
    private bool CanBePurchased()
    {
        if (item == null) return false;

        if (item.variantType == CosmeticVariantType.Base)
        {
            if (item.lootboxOnly)
                return false;

            return true;
        }

        if (item.requiredBaseItem == null)
            return true;

        return CosmeticSave.IsOwned(item.requiredBaseItem.id);
    }

    private void OnActionPressed()
    {
        if (item == null) return;

        bool owned = CosmeticSave.IsOwned(item.id);

        if (!owned)
        {
            Buy();
            return;
        }

        ToggleEquip();
    }

    private void Buy()
    {
        if (item == null || gemsManager == null) return;
        if (CosmeticSave.IsOwned(item.id)) return;
        if (!CanBePurchased()) return;

        if (!gemsManager.TrySpendGems(item.gemPrice))
            return;

        CosmeticSave.SetOwned(item.id, true);

        CosmeticSave.Equip(item.category, item.id);
        if (playerCosmetics != null)
            playerCosmetics.Equip(item);

        onChanged?.Invoke();
    }

    private void ToggleEquip()
    {
        if (item == null || !CosmeticSave.IsOwned(item.id)) return;

        bool equipped = CosmeticSave.IsEquipped(item.category, item.id);

        if (equipped)
        {
            CosmeticSave.Unequip(item.category);
            if (playerCosmetics != null)
                playerCosmetics.Unequip(item.category);
        }
        else
        {
            CosmeticSave.Equip(item.category, item.id);
            if (playerCosmetics != null)
                playerCosmetics.Equip(item);
        }

        onChanged?.Invoke();
    }
}
