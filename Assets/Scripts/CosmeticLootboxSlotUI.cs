using TMPro;
using UnityEngine;
using UnityEngine.UI;

// CosmeticLootboxSlotUI: one lootbox in the CosmeticLootboxUI panel.
// Shows a closed state (open button) and switches to an opened state displaying the reward.
// OpenLootbox() calls GameManager.OpenCosmeticLootbox() which picks a random eligible
// lootboxOnly cosmetic, marks it owned, and decrements the pending count.
// If no eligible cosmetics remain (all already owned), unlocked is null and a "no reward" state shows.
public class CosmeticLootboxSlotUI : MonoBehaviour
{
    [Header("Closed State")]
    [SerializeField] private GameObject closedGroup;
    [SerializeField] private Button openButton;
    [SerializeField] private TMP_Text openButtonText;

    [Header("Opened State")]
    [SerializeField] private GameObject openedGroup;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardNameText;
    [SerializeField] private TMP_Text rewardDescText;

    private CosmeticLootboxUI owner;
    private bool opened;

    public void Init(CosmeticLootboxUI ownerUI)
    {
        owner = ownerUI;
        opened = false;

        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(OpenLootbox);
        }

        ShowClosedState();
    }

    private void ShowClosedState()
    {
        if (closedGroup != null) closedGroup.SetActive(true);
        if (openedGroup != null) openedGroup.SetActive(false);

        if (openButtonText != null)
            openButtonText.text = "Open";
    }

    private void ShowOpenedState(CosmeticItemDefinition unlocked)
    {
        if (closedGroup != null) closedGroup.SetActive(false);
        if (openedGroup != null) openedGroup.SetActive(true);

        if (unlocked != null)
        {
            if (rewardIcon != null)
            {
                rewardIcon.sprite = unlocked.icon;
                rewardIcon.color = unlocked.tint;
                rewardIcon.enabled = unlocked.icon != null;
            }

            if (rewardNameText != null)
                rewardNameText.text = unlocked.displayName;

            if (rewardDescText != null)
                rewardDescText.text = "Unlocked and added to the cosmetic shop";
        }
        else
        {
            if (rewardIcon != null)
                rewardIcon.enabled = false;

            if (rewardNameText != null)
                rewardNameText.text = "No reward";

            if (rewardDescText != null)
                rewardDescText.text = "No eligible lootbox cosmetics remain";
        }
    }

    public void OpenLootbox()
    {
        if (opened) return;
        if (GameManager.Instance == null) return;

        opened = true;

        CosmeticItemDefinition unlocked = GameManager.Instance.OpenCosmeticLootbox();
        ShowOpenedState(unlocked);

        if (openButton != null)
            openButton.interactable = false;

        owner?.NotifyLootboxOpened();
    }
}
