using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ShopUI: lobby permanent upgrade shop. Instantiates a ShopItemSlotUI per catalog item
// into contentParent and calls Refresh() on all slots whenever a purchase is made.
// Slots are rebuilt on each Show/Enable to pick up any purchase state changes.
public class ShopUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ShopCatalog catalog;

    [Header("UI")]
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private Transform contentParent;
    [SerializeField] private ShopItemSlotUI slotPrefab;

    [Header("Gold Display")]
    [SerializeField] private TMP_Text goldText;

    private readonly List<ShopItemSlotUI> spawnedSlots = new();
    private GoldManager gold;

    private void Awake()
    {
        gold = FindObjectOfType<GoldManager>();
        if (shopRoot != null) shopRoot.SetActive(false);
    }

    private void OnEnable()
    {
        Build();
        RefreshAll();
    }

    public void ToggleShop()
    {
        if (shopRoot == null) return;
        shopRoot.SetActive(!shopRoot.activeSelf);

        if (shopRoot.activeSelf)
        {
            Build();
            RefreshAll();
        }
    }

    private void Build()
    {
        if (catalog == null || slotPrefab == null || contentParent == null) return;

        foreach (var s in spawnedSlots)
            if (s != null) Destroy(s.gameObject);
        spawnedSlots.Clear();

        foreach (var item in catalog.items)
        {
            if (item == null) continue;

            var slot = Instantiate(slotPrefab, contentParent);
            slot.Init(item, gold, RefreshAll);
            spawnedSlots.Add(slot);
        }
    }

    private void RefreshAll()
    {
        foreach (var slot in spawnedSlots)
            if (slot != null) slot.Refresh();

        UpdateGoldText();
    }

    public void ResetUpgradesForTesting()
    {
        UpgradeSave.ClearAllUpgrades();
        RefreshAll();
    }

    private void UpdateGoldText()
    {
        if (goldText == null || gold == null) return;

        goldText.text = $"{gold.Gold}";
    }
}
