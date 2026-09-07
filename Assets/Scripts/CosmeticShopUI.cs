using System.Collections.Generic;
using TMPro;
using UnityEngine;

// CosmeticShopUI: lobby gem shop for cosmetic items.
// Builds a CosmeticShopSlotUI per catalog item. lootboxOnly items are visible but show
// "Lootbox Only" and cannot be purchased here.
// Displays a lootbox availability notification when pending lootboxes > 0 (sourced from
// GameManager.GetPendingCosmeticLootboxCount() which reads CosmeticSave).
public class CosmeticShopUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CosmeticCatalog catalog;

    [Header("UI")]
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private Transform contentParent;
    [SerializeField] private CosmeticShopSlotUI slotPrefab;

    [Header("Currency Display")]
    [SerializeField] private TMP_Text gemsText;

    [Header("Lootbox Notification")]
    [SerializeField] private TMP_Text lootboxNotificationText;

    [Header("Preview / Apply Target")]
    [SerializeField] private PlayerCosmetics playerCosmetics;

    private GemsManager gemsManager;
    private readonly List<CosmeticShopSlotUI> spawnedSlots = new();

    private void Awake()
    {
        gemsManager = FindObjectOfType<GemsManager>();
        if (playerCosmetics == null)
            playerCosmetics = FindObjectOfType<PlayerCosmetics>(true);

        if (shopRoot != null)
            shopRoot.SetActive(false);
    }

    private void OnEnable()
    {
        RefreshPlayerReference();
        Build();
        RefreshAll();
    }

    public void ToggleShop()
    {
        if (shopRoot == null) return;

        shopRoot.SetActive(!shopRoot.activeSelf);

        if (shopRoot.activeSelf)
        {
            RefreshPlayerReference();
            Build();
            RefreshAll();
        }
    }

    private void RefreshPlayerReference()
    {
        if (playerCosmetics == null)
            playerCosmetics = FindObjectOfType<PlayerCosmetics>(true);
    }

    private void Build()
    {
        if (catalog == null || slotPrefab == null || contentParent == null)
            return;

        foreach (var slot in spawnedSlots)
            if (slot != null) Destroy(slot.gameObject);

        spawnedSlots.Clear();

        foreach (var item in catalog.items)
        {
            if (item == null) continue;

            var slot = Instantiate(slotPrefab, contentParent);
            slot.Init(item, gemsManager, playerCosmetics, RefreshAll);
            spawnedSlots.Add(slot);
        }
    }

    public void RefreshAll()
    {
        foreach (var slot in spawnedSlots)
            if (slot != null) slot.Refresh();

        UpdateGemText();
        RefreshLootboxNotification();

        if (playerCosmetics != null)
            playerCosmetics.ApplySavedCosmetics();
    }

    private void UpdateGemText()
    {
        if (gemsText == null || gemsManager == null) return;
        gemsText.text = $"{gemsManager.Gems}";
    }

    public void RefreshLootboxNotification()
    {
        if (lootboxNotificationText == null) return;

        int count = 0;
        if (GameManager.Instance != null)
            count = GameManager.Instance.GetPendingCosmeticLootboxCount();

        if (count == 1)
        {
            lootboxNotificationText.gameObject.SetActive(true);
            lootboxNotificationText.text = $"{count} lootbox available!";
        }
        else if (count > 1)
        {
            lootboxNotificationText.text = $"{count} lootboxes available!";
        }
        else
        {
            lootboxNotificationText.gameObject.SetActive(false);
        }
    }

    public void ResetAllCosmetics()
    {
        if (catalog == null) return;

        foreach (var item in catalog.items)
        {
            if (item == null) continue;
            CosmeticSave.SetOwned(item.id, false);
        }

        CosmeticSave.Unequip(CosmeticCategory.Shirt);
        CosmeticSave.Unequip(CosmeticCategory.Kilt);
        CosmeticSave.SetPendingLootboxes(0);

        RefreshAll();

        Debug.Log("All cosmetics have been reset.");
    }
}
