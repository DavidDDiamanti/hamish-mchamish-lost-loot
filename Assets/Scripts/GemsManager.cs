using UnityEngine;
using UnityEngine.SceneManagement;

// GemsManager: singleton managing gem currency with a run-commit model.
// Gems earned during a level accumulate in runDelta (not yet saved). On CommitRun() (level
// complete via FinishLevel), runDelta folds into bankedGems and persists to PlayerPrefs.
// On RollbackRun() (quit or game over), runDelta is discarded. Mirrors GoldManager's pattern.
// All PlayerPrefs keys are namespaced via GameProfileContext to support multiple profiles.
public class GemsManager : MonoBehaviour
{
    public static GemsManager Instance { get; private set; }

    private const string GemsKeyRaw = "GEMS_BANKED";
    private string GemsKey => GameProfileContext.GetSaveKey(GemsKeyRaw);

    [Header("Scene Names")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("State")]
    [SerializeField] private int bankedGems;
    [SerializeField] private int runDelta;
    [SerializeField] private bool inLobby;

    private PlayerUI playerUI;

    public int BankedGems => bankedGems;
    public int RunDelta => runDelta;
    public int Gems => Mathf.Max(0, bankedGems + runDelta);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bankedGems = PlayerPrefs.GetInt(GemsKey, 0);
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
        playerUI.UpdateGems(Gems, change);
    }

    public void BeginRun()
    {
        runDelta = 0;
        UpdateUI(0);
    }

    public void RollbackRun()
    {
        int old = Gems;
        runDelta = 0;
        UpdateUI(Gems - old);
    }

    // Permanently saves all gems earned this run to PlayerPrefs.
    public void CommitRun()
    {
        bankedGems = Gems;
        runDelta = 0;
        SaveBanked();
        UpdateUI(0);
    }

    public void AddGems(int amount)
    {
        if (amount <= 0) return;

        int old = Gems;

        if (inLobby)
        {
            bankedGems += amount;
            SaveBanked();
        }
        else
        {
            runDelta += amount;
        }

        UpdateUI(Gems - old);
    }

    public bool TrySpendGems(int amount)
    {
        if (amount <= 0) return true;

        if (Gems < amount) return false;

        int old = Gems;

        if (inLobby)
        {
            bankedGems -= amount;
            if (bankedGems < 0) bankedGems = 0;
            SaveBanked();
        }
        else
        {
            runDelta -= amount;
        }

        UpdateUI(Gems - old);
        return true;
    }

    private void SaveBanked()
    {
        PlayerPrefs.SetInt(GemsKey, bankedGems);
        PlayerPrefs.Save();
    }

    public void ClearBankedForTesting()
    {
        bankedGems = 0;
        SaveBanked();
        UpdateUI(0);
    }

    public void ReloadForActiveProfile()
    {
        bankedGems = PlayerPrefs.GetInt(GemsKey, 0);
        runDelta = 0;
        UpdateUI(0);
    }
}
