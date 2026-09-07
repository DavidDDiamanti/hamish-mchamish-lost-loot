using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ShopItemSlotUI: one row in the permanent upgrade shop.
// Price scales with the current level: cost = (1 + level) * item.price.
// Refresh() updates the description (showing current level and cumulative bonus for PlayerStat
// upgrades), disables the buy button when maxed, and updates the button label.
public class ShopItemSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    private ShopItemDefinition item;
    private GoldManager gold;
    private System.Action onPurchased;

    public void Init(ShopItemDefinition def, GoldManager goldManager, System.Action onPurchasedCallback)
    {
        item = def;
        gold = goldManager;
        onPurchased = onPurchasedCallback;

        if (iconImage != null) iconImage.sprite = item.icon;
        if (nameText != null) nameText.text = item.displayName;
        if (descText != null) descText.text = item.description;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(Buy);
        }

        Refresh();
    }

    public void Refresh()
    {
        int level = UpgradeSave.GetLevel(item.id);
        bool maxed = (item.maxLevel > 0 && level >= item.maxLevel);

        if (descText != null)
        {
            if (item.type == UpgradeType.PlayerStat)
            {
                float total = level * item.statAmountPerLevel;

                string levelText;

                if (item.maxLevel > 1)
                    levelText = $"Level: {level}/{item.maxLevel}";
                else
                    levelText = $"Level: {level}";

                descText.text =
                    $"{item.description}\n" +
                    $"{levelText}" +
                    (level > 0 ? $"  (+{total})" : "");
            }
        }

        if (buyButton != null)
            buyButton.interactable = !maxed;

        int price = Mathf.RoundToInt((1 + level) * item.price);

        if (buyButtonText != null)
            buyButtonText.text = maxed ? "Owned" : $"{price} gold";
    }

    private void Buy()
    {
        if (item == null) return;

        int level = UpgradeSave.GetLevel(item.id);
        bool maxed = (item.maxLevel > 0 && level >= item.maxLevel);
        if (maxed)
        {
            Refresh();
            return;
        }

        if (gold == null) gold = FindObjectOfType<GoldManager>();
        if (gold == null) return;

        if (!gold.TrySpendGold(Mathf.RoundToInt((1 + level) * item.price)))
            return;

        if (!UpgradeSave.TryAddLevel(item.id, item.maxLevel))
            return;

        Refresh();
        onPurchased?.Invoke();
    }
}
