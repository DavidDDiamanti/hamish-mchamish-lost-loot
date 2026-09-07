using UnityEngine;
using UnityEngine.SceneManagement;

// GoldManager: singleton managing gold currency with a run-commit model.
// Gold earned during a level accumulates in runDelta (not yet saved). On CommitRun()
// (called by GameManager.FinishLevel), runDelta folds into bankedGold and persists to PlayerPrefs.
// On RollbackRun() (quit or game over), runDelta is discarded: the player keeps nothing from that run.
// All PlayerPrefs keys are namespaced via GameProfileContext to support multiple profiles.
public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    private const string GoldKeyRaw = "GOLD_BANKED";
    private string GoldKey => GameProfileContext.GetSaveKey(GoldKeyRaw);

    [Header("Scene Names")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("State")]
    [SerializeField] private int bankedGold;
    [SerializeField] private int runDelta;
    [SerializeField] private bool inLobby;

    private PlayerUI playerUI;

    public int BankedGold => bankedGold;
    public int RunDelta => runDelta;
    public int Gold => Mathf.Max(0, bankedGold + runDelta);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bankedGold = PlayerPrefs.GetInt(GoldKey, 0);
        runDelta = 0;
    }

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        inLobby = scene.name == lobbySceneName;

        if (inLobby)
            runDelta = 0;

        playerUI = FindObjectOfType<PlayerUI>(true);
        UpdateUI(0);
    }

    private void UpdateUI(int change)
    {
        if (playerUI == null) return;
        playerUI.UpdateGold(Gold, change);
    }

    public void BeginRun()
    {
        runDelta = 0;
        UpdateUI(0);
    }

    // Discards all gold earned this run. Called on quit or game over.
    public void RollbackRun()
    {
        int old = Gold;
        runDelta = 0;
        UpdateUI(Gold - old);
    }

    // Permanently saves all gold earned this run. Called on level complete.
    public void CommitRun()
    {
        bankedGold = Gold;
        runDelta = 0;
        SaveBanked();
        UpdateUI(0);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        int old = Gold;

        if (inLobby)
        {
            bankedGold += amount;
            SaveBanked();
        }
        else
        {
            runDelta += amount;
        }

        GameEvents.RaiseGoldGained(amount);
        UpdateUI(Gold - old);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;

        if (Gold < amount) return false;

        int old = Gold;

        if (inLobby)
        {
            bankedGold -= amount;
            if (bankedGold < 0) bankedGold = 0;
            SaveBanked();
        }
        else
        {
            runDelta -= amount;
        }

        UpdateUI(Gold - old);
        return true;
    }

    private void SaveBanked()
    {
        PlayerPrefs.SetInt(GoldKey, bankedGold);
        PlayerPrefs.Save();
    }

    public void ClearBankedForTesting()
    {
        bankedGold = 0;
        SaveBanked();
        UpdateUI(0);
    }

    public void ReloadForActiveProfile()
    {
        bankedGold = PlayerPrefs.GetInt(GoldKey, 0);
        runDelta = 0;
        UpdateUI(0);
    }
}
