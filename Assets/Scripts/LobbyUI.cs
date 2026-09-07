using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// LobbyUI: main menu UI controller for the lobby scene.
// Provides Play, PlayCustomGame, and Quit actions.
// Polls for pending cosmetic lootboxes each frame to keep the notification badge current.
public class LobbyUI : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("Lootbox Notification")]
    [SerializeField] private TMP_Text lootboxNotificationText;

    private void OnEnable()
    {
        RefreshLootboxNotification();
    }

    void Update()
    {
        RefreshLootboxNotification();
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

    public void Play()
    {
        PlayBaseGame();
    }

    public void PlayBaseGame()
    {
        GameProfileContext.SetBaseProfile();
        GameManager.Instance?.ReloadForActiveProfile();
        SceneManager.LoadScene(lobbySceneName);
    }

    public void PlayCustomGame()
    {
        if (CustomCsvRunManager.Instance == null || !CustomCsvRunManager.Instance.HasCustomRun)
        {
            Debug.LogWarning("No custom CSV run exists yet.");
            return;
        }

        GameProfileContext.SetCustomProfile("CUSTOM_CSV_RUN");
        GameManager.Instance?.ReloadForActiveProfile();
        SceneManager.LoadScene(lobbySceneName);
    }

    public void Quit()
    {
        Debug.Log("Quitting game");
        Application.Quit();
    }
}
