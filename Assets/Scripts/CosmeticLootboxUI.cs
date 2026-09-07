using System.Collections.Generic;
using TMPro;
using UnityEngine;

// CosmeticLootboxUI: panel showing one CosmeticLootboxSlotUI per pending lootbox.
// Pending count comes from GameManager.GetPendingCosmeticLootboxCount() which reads CosmeticSave.
// After each slot is opened, NotifyLootboxOpened() updates the header count and refreshes
// the cosmeticShopUI so newly unlocked items appear immediately.
public class CosmeticLootboxUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentParent;
    [SerializeField] private CosmeticLootboxSlotUI slotPrefab;
    [SerializeField] private TMP_Text pendingCountText;
    [SerializeField] private TMP_Text emptyText;

    [Header("Optional Refresh Targets")]
    [SerializeField] private CosmeticShopUI cosmeticShopUI;

    private readonly List<CosmeticLootboxSlotUI> spawnedSlots = new();

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Toggle()
    {
        if (root == null) return;

        bool show = !root.activeSelf;
        root.SetActive(show);

        if (show)
            RefreshUI();
    }

    public void Open()
    {
        if (root == null) return;
        root.SetActive(true);
        RefreshUI();
    }

    public void Close()
    {
        if (root == null) return;
        root.SetActive(false);
    }

    public void RefreshUI()
    {
        ClearSlots();

        if (GameManager.Instance == null || slotPrefab == null || contentParent == null)
        {
            UpdateHeader();
            return;
        }

        int pending = GameManager.Instance.GetPendingCosmeticLootboxCount();

        for (int i = 0; i < pending; i++)
        {
            CosmeticLootboxSlotUI slot = Instantiate(slotPrefab, contentParent);
            slot.Init(this);
            spawnedSlots.Add(slot);
        }

        UpdateHeader();

        if (cosmeticShopUI != null)
            cosmeticShopUI.RefreshAll();
    }

    private void ClearSlots()
    {
        foreach (var slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        spawnedSlots.Clear();
    }

    private void UpdateHeader()
    {
        int pending = 0;

        if (GameManager.Instance != null)
            pending = GameManager.Instance.GetPendingCosmeticLootboxCount();

        if (pendingCountText != null)
            pendingCountText.text = $"Lootboxes: {pending}";

        if (emptyText != null)
            emptyText.gameObject.SetActive(pending <= 0);
    }

    public void NotifyLootboxOpened()
    {
        UpdateHeader();

        if (cosmeticShopUI != null)
            cosmeticShopUI.RefreshAll();
    }
}
