using UnityEngine;
using System.Collections;
using TMPro;

// PlayerUI: in-game HUD displayed during active gameplay.
// Shows health, gold, gems, speed, digPower, damage, remaining chests, and up to 4 quest rows.
// Stat text flashes green on increase and red on decrease; temporary-boost tint overrides
// the default white colour until ResetTemporaryBoostColors() is called at level end.
// Floating notifications (gold, gems, pickups, chest results) each run independent coroutines
// so simultaneous events don't cancel each other.
// Streak panel shows a sway + scale + RGB cycling animation that intensifies with streak count.
// Subscribes to GameEvents.QuestProgressChanged/Completed/Collected to refresh quest display.
public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject playerStatsPanel;
    [SerializeField] private GameObject remainingPanel;
    [SerializeField] private GameObject streakPanel;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject questNotificationPanel;
    [SerializeField] private GameObject playerStatsBottomPanel;

    [Header("Stats Text")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text goldNotificationText;
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private TMP_Text gemNotificationText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text digPowerText;
    [SerializeField] private TMP_Text damageText;

    [Header("Other UI Elements")]
    [SerializeField] private TMP_Text remainingChestsText;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private TMP_Text streakBonusTally;

    [Header("Pickup Notification")]
    [SerializeField] private TMP_Text pickupNotificationText;

    [SerializeField] private float pickupPopMultiplier = 1.25f;
    [SerializeField] private float pickupPopDuration = 0.10f;
    [SerializeField] private float pickupHoldTime = 0.65f;
    [SerializeField] private float pickupFadeDuration = 0.25f;

    private Coroutine pickupNotifyRoutine;
    private Vector3 pickupNotifyOriginalScale;

    private GameManager gameManager;
    private Player player;
    private GoldManager goldManager;
    private GemsManager gemsManager;
    private float currentHealth = 0f;
    private int currentGold = 0;
    private int currentGems = 0;
    private int currentStreak = 0;

    private float currentSpeed = 0f;
    private float currentDigPower = 0f;
    private float currentDamage = 0f;

    [Header("Text Flash Settings")]
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private float popSize = 2f;

    private Coroutine healthFlashRoutine;
    private Coroutine goldFlashRoutine;
    private Coroutine gemFlashRoutine;
    private Coroutine speedFlashRoutine;
    private Coroutine digPowerFlashRoutine;
    private Coroutine damageFlashRoutine;

    [Header("Streak Animation Settings")]
    [SerializeField] private float swayAngleDegrees = 4f;
    [SerializeField] private float baseSwaySpeed = 1.5f;
    [SerializeField] private float swaySpeedPerStreak = 0.2f;
    [SerializeField] private float maxStreakSwaySpeed = 3.5f;
    [SerializeField] private float streakPopMultiplier = 1.12f;
    [SerializeField] private float streakPopDuration = 0.12f;
    [SerializeField] private float streakSizePerStreak = 0.08f;
    [SerializeField] private float streakLoseDuration = 0.22f;
    [SerializeField] private float streakLoseShrinkTo = 0.65f;
    [SerializeField] private float maxStreakScale = 2f;

    private Coroutine streakSwayRoutine;
    private Coroutine streakLoseRoutine;
    private Coroutine streakPopRoutine;

    [Header("Streak RGB Settings")]
    [SerializeField] private float rgbMinBrightness = 0.35f;
    [SerializeField] private float rgbMaxBrightness = 1.0f;
    [SerializeField] private float rgbMinSpeed = 0.10f;
    [SerializeField] private float rgbSpeedAt10 = 1.0f;

    [Header("Gold Notification Settings")]
    [SerializeField] private float goldNotifyPopMultiplier = 1.25f;
    [SerializeField] private float goldNotifyPopDuration = 0.10f;
    [SerializeField] private float goldNotifyHoldTime = 0.50f;
    [SerializeField] private float goldNotifyFadeDuration = 0.35f;
    [SerializeField] private Vector3 goldNotifyLocalOffset = Vector3.zero;

    [Header("Gem Notification Settings")]
    [SerializeField] private float gemNotifyPopMultiplier = 1.25f;
    [SerializeField] private float gemNotifyPopDuration = 0.10f;
    [SerializeField] private float gemNotifyHoldTime = 0.50f;
    [SerializeField] private float gemNotifyFadeDuration = 0.35f;
    [SerializeField] private Vector3 gemNotifyLocalOffset = Vector3.zero;

    [Header("Temporary Boost Colors")]
    [SerializeField] private Color temporaryBoostColor = Color.blue;

    [Header("Quest UI")]
    [SerializeField] private TMP_Text[] questTitleTexts;    // size 4
    [SerializeField] private TMP_Text[] questProgressTexts; // size 4
    [SerializeField] private TMP_Text allQuestsCollectedText;

    [Header("Quest Colors")]
    [SerializeField] private Color questIncompleteColor = Color.white;
    [SerializeField] private Color questProgressColor = Color.gray;
    [SerializeField] private Color questCompleteColor = Color.green;

    [Header("Chest Result Notification")]
    [SerializeField] private TMP_Text chestResultNotificationText;

    [SerializeField] private float chestResultPopMultiplier = 1.25f;
    [SerializeField] private float chestResultPopDuration = 0.10f;
    [SerializeField] private float chestResultHoldTime = 0.65f;
    [SerializeField] private float chestResultFadeDuration = 0.25f;

    private Coroutine chestResultNotifyRoutine;
    private Vector3 chestResultNotifyOriginalScale;

    private Color defaultHealthColor = Color.white;
    private Color defaultSpeedColor = Color.white;
    private Color defaultDigPowerColor = Color.white;
    private Color defaultDamageColor = Color.white;

    private bool healthTemporarilyBoosted = false;
    private bool speedTemporarilyBoosted = false;
    private bool digPowerTemporarilyBoosted = false;
    private bool damageTemporarilyBoosted = false;

    private Coroutine goldNotifyRoutine;
    private Vector3 goldNotifyOriginalScale;
    private Coroutine gemNotifyRoutine;
    private Vector3 gemNotifyOriginalScale;

    private Coroutine streakRgbRoutine;

    private Vector3 streakOriginalScale = Vector3.one;
    private Quaternion streakOriginalRot = Quaternion.identity;
    private Vector3 textOriginalScale = Vector3.one;
    private bool levelStarted = false;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>(true);
        player = FindObjectOfType<Player>(true);
        goldManager = FindObjectOfType<GoldManager>(true);
        gemsManager = FindObjectOfType<GemsManager>(true);

        pickupNotifyOriginalScale = pickupNotificationText.transform.localScale;
        pickupNotificationText.gameObject.SetActive(false);

        streakOriginalScale = streakPanel.transform.localScale;
        streakOriginalRot = streakPanel.transform.localRotation;
        streakPanel.SetActive(false);

        goldNotifyOriginalScale = goldNotificationText.transform.localScale;
        goldNotificationText.gameObject.SetActive(false);

        gemNotifyOriginalScale = gemNotificationText.transform.localScale;
        gemNotificationText.gameObject.SetActive(false);

        if (chestResultNotificationText != null)
        {
            chestResultNotifyOriginalScale = chestResultNotificationText.transform.localScale;
            chestResultNotificationText.gameObject.SetActive(false);
        }

        SetUI();
    }

    public void SetUI()
    {
        if (player != null)
        {
            UpdateHealth(player.GetHealth());
            UpdateSpeed(player.GetSpeed());
            UpdateDigPower(player.GetDigPower());
            UpdateDamage(player.GetDamage());
        }

        if (goldManager != null)
            UpdateGold(goldManager.Gold, 0);

        if (gemsManager != null)
            UpdateGems(gemsManager.Gems, 0);

        if (gameManager != null)
            UpdateRemainingChests(gameManager.GetNumberOfUnopenedChests());

        RefreshQuestDisplay();
    }

    private void OnEnable()
    {
        GameEvents.QuestProgressChanged += RefreshQuestDisplay;
        GameEvents.QuestCompleted += RefreshQuestDisplay;
        GameEvents.QuestCollected += RefreshQuestDisplay;
    }

    private void OnDisable()
    {
        GameEvents.QuestProgressChanged -= RefreshQuestDisplay;
        GameEvents.QuestCompleted -= RefreshQuestDisplay;
        GameEvents.QuestCollected -= RefreshQuestDisplay;
    }

    public void setLevelStarted(bool started)
    {
        levelStarted = started;
    }

    private void OnStreakChangedRefresh(int _) => RefreshQuestDisplay();
    private void OnGoldGainedRefresh(int _) => RefreshQuestDisplay();

    // Renders up to 4 active quests in the quest panel. Skips already-collected quests.
    // Colours complete quests green and incomplete quests white/gray.
    public void RefreshQuestDisplay()
    {
        if (QuestManager.Instance == null) return;

        var quests = QuestManager.Instance.GetActiveQuests();

        int row = 0;
        for (int i = 0; i < quests.Count && row < 4; i++)
        {
            QuestDefinition q = quests[i];
            if (q == null) continue;

            if (QuestManager.Instance.IsRewardCollected(q)) continue;

            int current = QuestManager.Instance.GetProgressForQuest(q);
            int target = Mathf.Max(1, q.targetAmount);
            bool completed = QuestManager.Instance.IsCompleted(q);

            TMP_Text titleText = questTitleTexts[row];
            TMP_Text progressText = questProgressTexts[row];

            titleText.text = q.title;
            progressText.text = $"{Mathf.Min(current, target)}/{target}";

            if (completed)
            {
                titleText.color = questCompleteColor;
                progressText.color = questCompleteColor;
            }
            else
            {
                titleText.color = questIncompleteColor;
                progressText.color = questProgressColor;
            }

            row++;
        }

        for (int i = row; i < 4; i++)
        {
            questTitleTexts[i].text = "";
            questProgressTexts[i].text = "";
        }

        bool allCollected = (row == 0);
        if (allCollected)
            allQuestsCollectedText.text = "All quests collected!";
    }

    public void ShowPickupNotification(string message)
    {
        if (ShouldBlockUIEffects()) return;
        if (pickupNotificationText == null) return;
        if (string.IsNullOrWhiteSpace(message)) return;
        if (!levelStarted) return;

        if (pickupNotifyRoutine != null) StopCoroutine(pickupNotifyRoutine);
        pickupNotifyRoutine = StartCoroutine(PickupNotificationRoutine(message));
    }

    private IEnumerator PickupNotificationRoutine(string message)
    {
        pickupNotificationText.gameObject.SetActive(true);
        pickupNotificationText.text = message;

        Color c = pickupNotificationText.color;
        c.a = 1f;
        pickupNotificationText.color = c;

        Vector3 start = pickupNotifyOriginalScale;
        Vector3 peak  = pickupNotifyOriginalScale * pickupPopMultiplier;

        float t = 0f;
        while (t < pickupPopDuration)
        {
            t += Time.deltaTime;
            pickupNotificationText.transform.localScale = Vector3.Lerp(start, peak, t / pickupPopDuration);
            yield return null;
        }

        t = 0f;
        while (t < pickupPopDuration)
        {
            t += Time.deltaTime;
            pickupNotificationText.transform.localScale = Vector3.Lerp(peak, start, t / pickupPopDuration);
            yield return null;
        }
        pickupNotificationText.transform.localScale = start;

        yield return new WaitForSeconds(pickupHoldTime);

        float fadeT = 0f;
        while (fadeT < pickupFadeDuration)
        {
            fadeT += Time.deltaTime;
            float lerp = fadeT / pickupFadeDuration;

            Color fc = pickupNotificationText.color;
            fc.a = Mathf.Lerp(1f, 0f, lerp);
            pickupNotificationText.color = fc;

            yield return null;
        }

        pickupNotificationText.gameObject.SetActive(false);
        pickupNotificationText.transform.localScale = pickupNotifyOriginalScale;

        Color reset = pickupNotificationText.color;
        reset.a = 1f;
        pickupNotificationText.color = reset;

        pickupNotifyRoutine = null;
    }

    public void UpdateHealth(float health)
    {
        if (healthText == null) return;

        float maxHealth = health;

        if (player != null)
            maxHealth = player.GetMaxHealth();

        healthText.text = $"{health}/{maxHealth}";

        if (currentHealth > health)
        {
            if (healthFlashRoutine != null) StopCoroutine(healthFlashRoutine);
            healthFlashRoutine = StartCoroutine(FlashTextColor(healthText, Color.red));
        }
        else if (currentHealth < health)
        {
            if (healthFlashRoutine != null) StopCoroutine(healthFlashRoutine);
            healthFlashRoutine = StartCoroutine(FlashTextColor(healthText, Color.green));
        }

        currentHealth = health;
    }

    public void UpdateSpeed(float speed)
    {
        if (speedText == null) return;

        speedText.text = $"{speed}";

        if (currentSpeed > speed)
        {
            if (speedFlashRoutine != null) StopCoroutine(speedFlashRoutine);
            speedFlashRoutine = StartCoroutine(FlashTextColor(speedText, Color.red));
        }
        else if (currentSpeed < speed)
        {
            if (speedFlashRoutine != null) StopCoroutine(speedFlashRoutine);
            speedFlashRoutine = StartCoroutine(FlashTextColor(speedText, Color.green));
        }

        currentSpeed = speed;
    }

    public void UpdateDigPower(float digPower)
    {
        if (digPowerText == null) return;

        digPowerText.text = $"{digPower}";

        if (currentDigPower > digPower)
        {
            if (digPowerFlashRoutine != null) StopCoroutine(digPowerFlashRoutine);
            digPowerFlashRoutine = StartCoroutine(FlashTextColor(digPowerText, Color.red));
        }
        else if (currentDigPower < digPower)
        {
            if (digPowerFlashRoutine != null) StopCoroutine(digPowerFlashRoutine);
            digPowerFlashRoutine = StartCoroutine(FlashTextColor(digPowerText, Color.green));
        }

        currentDigPower = digPower;
    }

    public void UpdateDamage(float damage)
    {
        if (damageText == null) return;

        damageText.text = $"{damage}";

        if (currentDamage > damage)
        {
            if (damageFlashRoutine != null) StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = StartCoroutine(FlashTextColor(damageText, Color.red));
        }
        else if (currentDamage < damage)
        {
            if (damageFlashRoutine != null) StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = StartCoroutine(FlashTextColor(damageText, Color.green));
        }

        currentDamage = damage;
    }

    public void UpdateGold(int gold, int goldChange)
    {
        if (goldText == null) return;

        goldText.text = $"{gold}";

        if (currentGold < gold)
        {
            if (goldFlashRoutine != null) StopCoroutine(goldFlashRoutine);
            goldFlashRoutine = StartCoroutine(FlashTextColor(goldText, Color.green));
        }
        else if (currentGold > gold)
        {
            if (goldFlashRoutine != null) StopCoroutine(goldFlashRoutine);
            goldFlashRoutine = StartCoroutine(FlashTextColor(goldText, Color.red));
        }

        if (ShouldBlockUIEffects()) return;

        currentGold = gold;
        if (goldChange != 0) ShowGoldNotification(goldChange);
    }

    public void UpdateGems(int gems, int gemChange)
    {
        if (gemsText == null) return;

        gemsText.text = $"{gems}";

        if (currentGems < gems)
        {
            if (gemFlashRoutine != null) StopCoroutine(gemFlashRoutine);
            gemFlashRoutine = StartCoroutine(FlashTextColor(gemsText, Color.green));
        }
        else if (currentGems > gems)
        {
            if (gemFlashRoutine != null) StopCoroutine(gemFlashRoutine);
            gemFlashRoutine = StartCoroutine(FlashTextColor(gemsText, Color.red));
        }

        currentGems = gems;

        if (ShouldBlockUIEffects()) return;

        if (gemChange != 0)
            ShowGemNotification(gemChange);
    }

    private void ShowGemNotification(int gemChange)
    {
        if (gemNotificationText == null) return;
        if (gemChange == 0) return;

        if (gemNotifyRoutine != null) StopCoroutine(gemNotifyRoutine);
        gemNotifyRoutine = StartCoroutine(GemNotificationRoutine(gemChange));
    }

    private IEnumerator GemNotificationRoutine(int gemChange)
    {
        gemNotificationText.gameObject.SetActive(true);

        string sign = gemChange > 0 ? "+" : "";
        gemNotificationText.text = $"{sign}{gemChange}";
        gemNotificationText.color = gemChange > 0 ? Color.green : Color.red;

        Color baseColor = gemNotificationText.color;
        baseColor.a = 1f;
        gemNotificationText.color = baseColor;

        gemNotificationText.transform.localScale = gemNotifyOriginalScale;

        Vector3 start = gemNotifyOriginalScale;
        Vector3 peak = gemNotifyOriginalScale * gemNotifyPopMultiplier;

        float t = 0f;
        while (t < gemNotifyPopDuration)
        {
            t += Time.deltaTime;
            gemNotificationText.transform.localScale = Vector3.Lerp(start, peak, t / gemNotifyPopDuration);
            yield return null;
        }

        t = 0f;
        while (t < gemNotifyPopDuration)
        {
            t += Time.deltaTime;
            gemNotificationText.transform.localScale = Vector3.Lerp(peak, start, t / gemNotifyPopDuration);
            yield return null;
        }

        gemNotificationText.transform.localScale = start;

        yield return new WaitForSeconds(gemNotifyHoldTime);

        float fadeT = 0f;
        while (fadeT < gemNotifyFadeDuration)
        {
            fadeT += Time.deltaTime;
            float lerp = fadeT / gemNotifyFadeDuration;

            Color c = gemNotificationText.color;
            c.a = Mathf.Lerp(1f, 0f, lerp);
            gemNotificationText.color = c;

            yield return null;
        }

        gemNotificationText.gameObject.SetActive(false);
        gemNotificationText.transform.localScale = gemNotifyOriginalScale;

        Color reset = gemNotificationText.color;
        reset.a = 1f;
        gemNotificationText.color = reset;

        gemNotifyRoutine = null;
    }

    private void ShowGoldNotification(int goldChange)
    {
        if (goldNotificationText == null) return;
        if (goldChange == 0) return;

        if (goldNotifyRoutine != null) StopCoroutine(goldNotifyRoutine);
        goldNotifyRoutine = StartCoroutine(GoldNotificationRoutine(goldChange));
    }

    private IEnumerator GoldNotificationRoutine(int goldChange)
    {
        goldNotificationText.gameObject.SetActive(true);

        string sign = goldChange > 0 ? "+" : "";
        goldNotificationText.text = $"{sign}{goldChange}";
        goldNotificationText.color = goldChange > 0 ? Color.green : Color.red;

        Color baseColor = goldNotificationText.color;
        baseColor.a = 1f;
        goldNotificationText.color = baseColor;

        goldNotificationText.transform.localScale = goldNotifyOriginalScale;

        Vector3 start = goldNotifyOriginalScale;
        Vector3 peak = goldNotifyOriginalScale * goldNotifyPopMultiplier;

        float t = 0f;
        while (t < goldNotifyPopDuration)
        {
            t += Time.deltaTime;
            goldNotificationText.transform.localScale = Vector3.Lerp(start, peak, t / goldNotifyPopDuration);
            yield return null;
        }

        t = 0f;
        while (t < goldNotifyPopDuration)
        {
            t += Time.deltaTime;
            goldNotificationText.transform.localScale = Vector3.Lerp(peak, start, t / goldNotifyPopDuration);
            yield return null;
        }

        goldNotificationText.transform.localScale = start;

        yield return new WaitForSeconds(goldNotifyHoldTime);

        float fadeT = 0f;
        while (fadeT < goldNotifyFadeDuration)
        {
            fadeT += Time.deltaTime;
            float lerp = fadeT / goldNotifyFadeDuration;

            Color c = goldNotificationText.color;
            c.a = Mathf.Lerp(1f, 0f, lerp);
            goldNotificationText.color = c;

            yield return null;
        }

        goldNotificationText.gameObject.SetActive(false);
        goldNotificationText.transform.localScale = goldNotifyOriginalScale;

        Color reset = goldNotificationText.color;
        reset.a = 1f;
        goldNotificationText.color = reset;

        goldNotifyRoutine = null;
    }

    public void UpdateRemainingChests(int remaining)
    {
        if (remainingChestsText == null) return;
        remainingChestsText.text = $"Remaining chests: {remaining}";
    }

    public void UpdateStreak(int streak)
    {
        if (streakPanel == null) return;

        if (streak <= 0)
        {
            if (currentStreak > 0)
            {
                if (streakLoseRoutine != null) StopCoroutine(streakLoseRoutine);
                streakLoseRoutine = StartCoroutine(StreakLoseAndHide());
            }

            currentStreak = 0;
            StopStreakRGB();
            return;
        }

        bool wasInactive = currentStreak <= 0;
        bool increased = streak > currentStreak;

        currentStreak = streak;

        if (!streakPanel.activeSelf) streakPanel.SetActive(true);

        if (streakBonusTally != null) streakBonusTally.text = $"x{streak}";

        StartStreakRGB();

        if (streakSwayRoutine != null) StopCoroutine(streakSwayRoutine);
        streakSwayRoutine = StartCoroutine(StreakSwayRoutine(streak));

        if (wasInactive || increased)
        {
            if (streakPopRoutine != null) StopCoroutine(streakPopRoutine);
            streakPopRoutine = StartCoroutine(StreakPop());
        }
    }

    private IEnumerator StreakSwayRoutine(int streak)
    {
        float speed = baseSwaySpeed;
        if (streak >= 10)
        {
            speed = maxStreakSwaySpeed;
        }
        else
        {
            speed = baseSwaySpeed + (streak - 1) * swaySpeedPerStreak;
        }
        streakPanel.transform.localRotation = streakOriginalRot;

        while (streakPanel != null && streakPanel.activeSelf && currentStreak > 0)
        {
            float angle = Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * swayAngleDegrees;
            streakPanel.transform.localRotation = streakOriginalRot * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        if (streakPanel != null)
            streakPanel.transform.localRotation = streakOriginalRot;
    }

    // Grows the streak panel scale with streak count (capped at maxStreakScale), then pops.
    private IEnumerator StreakPop()
    {
        Vector3 start = streakOriginalScale * (1f + (currentStreak - 1) * streakSizePerStreak);
        Vector3 maxScale = streakOriginalScale * maxStreakScale;
        if (start.magnitude > maxScale.magnitude)
        {
            float maxMult = maxStreakScale;
            start = streakOriginalScale * maxMult;
        }
        Vector3 peak = streakOriginalScale * streakPopMultiplier;

        float t = 0f;
        while (t < streakPopDuration)
        {
            t += Time.deltaTime;
            streakPanel.transform.localScale = Vector3.Lerp(start, peak, t / streakPopDuration);
            yield return null;
        }

        t = 0f;
        while (t < streakPopDuration)
        {
            t += Time.deltaTime;
            streakPanel.transform.localScale = Vector3.Lerp(peak, start, t / streakPopDuration);
            yield return null;
        }

        streakPanel.transform.localScale = start;
    }

    private IEnumerator StreakLoseAndHide()
    {
        if (streakSwayRoutine != null) { StopCoroutine(streakSwayRoutine); streakSwayRoutine = null; }
        if (streakPopRoutine != null) { StopCoroutine(streakPopRoutine); streakPopRoutine = null; }

        StopStreakRGB();
        SetStreakColors(Color.red);

        streakPanel.transform.localRotation = streakOriginalRot;

        Vector3 startScale = streakPanel.transform.localScale;
        Vector3 endScale = streakOriginalScale * streakLoseShrinkTo;

        float t = 0f;
        while (t < streakLoseDuration)
        {
            t += Time.deltaTime;
            streakPanel.transform.localScale = Vector3.Lerp(startScale, endScale, t / streakLoseDuration);
            yield return null;
        }

        streakPanel.SetActive(false);
        streakPanel.transform.localScale = streakOriginalScale;
        streakPanel.transform.localRotation = streakOriginalRot;
        SetStreakColors(Color.white);
    }

    private void SetStreakColors(Color c)
    {
        if (streakText != null) streakText.color = c;
        if (streakBonusTally != null) streakBonusTally.color = c;
    }

    private void StopStreakRGB()
    {
        if (streakRgbRoutine != null)
        {
            StopCoroutine(streakRgbRoutine);
            streakRgbRoutine = null;
        }
    }

    // RGB only starts from streak 2+; streak 1 stays white to avoid overwhelming the player.
    private void StartStreakRGB()
    {
        if (currentStreak <= 1)
        {
            SetStreakColors(Color.white);
        }
        else
        {
            StopStreakRGB();
            streakRgbRoutine = StartCoroutine(StreakRGBRoutine());
        }
    }

    // Cycles hue continuously. Brightness and speed both ramp up as streak increases to 10.
    private IEnumerator StreakRGBRoutine()
    {
        while (streakPanel != null && streakPanel.activeSelf && currentStreak > 0)
        {
            float brightnessT = Mathf.InverseLerp(1f, 10f, currentStreak);
            float brightness = Mathf.Lerp(rgbMinBrightness, rgbMaxBrightness, brightnessT);

            float speed;
            if (currentStreak <= 10)
            {
                float speedT = Mathf.InverseLerp(1f, 10f, currentStreak);
                speed = Mathf.Lerp(rgbMinSpeed, rgbSpeedAt10, speedT);
            }
            else
            {
                speed = rgbSpeedAt10;
            }

            float hue = Mathf.Repeat(Time.time * speed, 1f);
            Color c = Color.HSVToRGB(hue, 1f, brightness);

            if (streakText != null) streakText.color = c;
            if (streakBonusTally != null) streakBonusTally.color = c;

            yield return null;
        }
    }

    public void Show()
    {
        playerStatsPanel?.SetActive(true);
        remainingPanel?.SetActive(true);
        if (currentStreak > 0) streakPanel.SetActive(true);
        questPanel?.SetActive(true);
        playerStatsBottomPanel?.SetActive(true);

        RefreshQuestDisplay();
    }

    // Returns true and hides the HUD when the level has completed, suppressing further effects.
    private bool ShouldBlockUIEffects()
    {
        if (gameManager != null && gameManager.IsLevelComplete())
        {
            Hide();
            return true;
        }
        return false;
    }

    public void Hide()
    {
        playerStatsPanel?.SetActive(false);
        remainingPanel?.SetActive(false);
        streakPanel?.SetActive(false);
        questPanel?.SetActive(false);
        playerStatsBottomPanel?.SetActive(false);
        questNotificationPanel?.SetActive(false);

        if (goldNotificationText != null)
            goldNotificationText.gameObject.SetActive(false);

        if (gemNotificationText != null)
            gemNotificationText.gameObject.SetActive(false);

        if (pickupNotificationText != null)
            pickupNotificationText.gameObject.SetActive(false);

        if (chestResultNotificationText != null)
            chestResultNotificationText.gameObject.SetActive(false);
    }

    // Flashes text to flashColor then lerps back to the stat's rest colour (white or boost colour).
    // Uses a fixed textOriginalScale (Vector3.one) rather than per-text cached scales.
    private IEnumerator FlashTextColor(TMP_Text text, Color flashColor)
    {
        if (text == null) yield break;

        Color startColor = flashColor;
        Color endColor = Color.white;

        Vector3 originalScale = textOriginalScale;
        Vector3 popScale = originalScale * popSize;

        text.color = startColor;
        text.transform.localScale = popScale;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float lerp = t / flashDuration;

            text.color = Color.Lerp(startColor, endColor, lerp);
            text.transform.localScale = Vector3.Lerp(popScale, originalScale, lerp);

            yield return null;
        }

        if (text == healthText)
            text.color = healthTemporarilyBoosted ? temporaryBoostColor : defaultHealthColor;
        else if (text == speedText)
            text.color = speedTemporarilyBoosted ? temporaryBoostColor : defaultSpeedColor;
        else if (text == digPowerText)
            text.color = digPowerTemporarilyBoosted ? temporaryBoostColor : defaultDigPowerColor;
        else if (text == damageText)
            text.color = damageTemporarilyBoosted ? temporaryBoostColor : defaultDamageColor;
        else
            text.color = endColor;

        text.transform.localScale = originalScale;
    }

    public void MarkHealthTemporaryBoosted()
    {
        healthTemporarilyBoosted = true;
        if (healthText != null) healthText.color = temporaryBoostColor;
    }

    public void MarkSpeedTemporaryBoosted()
    {
        speedTemporarilyBoosted = true;
        if (speedText != null) speedText.color = temporaryBoostColor;
    }

    public void MarkDigPowerTemporaryBoosted()
    {
        digPowerTemporarilyBoosted = true;
        if (digPowerText != null) digPowerText.color = temporaryBoostColor;
    }

    public void MarkDamageTemporaryBoosted()
    {
        damageTemporarilyBoosted = true;
        if (damageText != null) damageText.color = temporaryBoostColor;
    }

    public void ResetTemporaryBoostColors()
    {
        healthTemporarilyBoosted = false;
        speedTemporarilyBoosted = false;
        digPowerTemporarilyBoosted = false;
        damageTemporarilyBoosted = false;

        if (healthText != null) healthText.color = defaultHealthColor;
        if (speedText != null) speedText.color = defaultSpeedColor;
        if (digPowerText != null) digPowerText.color = defaultDigPowerColor;
        if (damageText != null) damageText.color = defaultDamageColor;
    }

    public void HideStreakPanel()
    {
        if (streakPanel != null)
            streakPanel.SetActive(false);
    }

    public void ShowChestResultNotification(bool answeredCorrectly)
    {
        if (chestResultNotificationText == null) return;
        if (ShouldBlockUIEffects()) return;

        string message = answeredCorrectly ? "Correct, streak increased!" : "Incorrect, streak lost!";
        if (gameManager.GetStreak() <= 0)
        {
            message = answeredCorrectly ? "Correct, streak started!" : "Incorrect!";
        }
        Color color = answeredCorrectly ? Color.green : Color.red;

        if (chestResultNotifyRoutine != null)
            StopCoroutine(chestResultNotifyRoutine);

        chestResultNotifyRoutine = StartCoroutine(ChestResultNotificationRoutine(message, color));
    }

    private IEnumerator ChestResultNotificationRoutine(string message, Color color)
    {
        chestResultNotificationText.gameObject.SetActive(true);
        chestResultNotificationText.text = message;

        Color c = color;
        c.a = 1f;
        chestResultNotificationText.color = c;

        Vector3 start = chestResultNotifyOriginalScale;
        Vector3 peak = chestResultNotifyOriginalScale * chestResultPopMultiplier;

        float t = 0f;
        while (t < chestResultPopDuration)
        {
            t += Time.deltaTime;
            chestResultNotificationText.transform.localScale =
                Vector3.Lerp(start, peak, t / chestResultPopDuration);
            yield return null;
        }

        t = 0f;
        while (t < chestResultPopDuration)
        {
            t += Time.deltaTime;
            chestResultNotificationText.transform.localScale =
                Vector3.Lerp(peak, start, t / chestResultPopDuration);
            yield return null;
        }

        chestResultNotificationText.transform.localScale = start;

        yield return new WaitForSeconds(chestResultHoldTime);

        float fadeT = 0f;
        while (fadeT < chestResultFadeDuration)
        {
            fadeT += Time.deltaTime;
            float lerp = fadeT / chestResultFadeDuration;

            Color fc = chestResultNotificationText.color;
            fc.a = Mathf.Lerp(1f, 0f, lerp);
            chestResultNotificationText.color = fc;

            yield return null;
        }

        chestResultNotificationText.gameObject.SetActive(false);
        chestResultNotificationText.transform.localScale = chestResultNotifyOriginalScale;

        Color reset = chestResultNotificationText.color;
        reset.a = 1f;
        chestResultNotificationText.color = reset;

        chestResultNotifyRoutine = null;
    }
}
