using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Player: central facade for all player state and capabilities.
// Stats (health, digPower, damage, speed) are read by pickups and enemies; temporary boosts
// from pickups call the matching MarkStat methods so PlayerUI colours the relevant HUD text.
// autoClickEnabled is the shop upgrade that fires TryInteract every frame during live gameplay.
// Invulnerability flash plays across filtered spriteRenderers (excludes PlayerActionArea and
// ActionFlashEffect children to avoid interfering with those effects).
public class Player : MonoBehaviour
{
    [SerializeField] private float digPower = 0.25f;
    [SerializeField] private float maxHealth = 10;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float invulnerabilityTime = 1f;

    [Header("Visual Movement Animation")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float bobAmplitude = 0.06f;
    [SerializeField] private float bobSpeed = 7f;
    [SerializeField] private float tiltAngle = 6f;
    [SerializeField] private float tiltSmoothness = 10f;
    [SerializeField] private float idleReturnSpeed = 8f;

    private float health = 10;
    private PlayerActionArea playerActionArea;
    private ActionFlashEffect actionFlashEffect;
    private PlayerMovement playerMovement;
    private GameManager gameManager;
    private GoldManager goldManager;
    private PlayerUI playerUI;
    private float lastDamageTime = -Mathf.Infinity;
    private SpriteRenderer[] spriteRenderers;
    private Coroutine flashRoutine;
    private bool levelStarted = false;

    private Vector3 visualStartLocalPos;
    private Quaternion visualStartLocalRot;
    private bool autoClickEnabled = false;

    void Awake()
    {
        health = maxHealth;

        playerActionArea = GetComponentInChildren<PlayerActionArea>();
        gameManager = FindObjectOfType<GameManager>();
        goldManager = FindObjectOfType<GoldManager>();
        playerUI = FindObjectOfType<PlayerUI>(true);
        playerMovement = GetComponent<PlayerMovement>();
        actionFlashEffect = GetComponentInChildren<ActionFlashEffect>(true);

        if (visualRoot == null)
            visualRoot = transform;

        visualStartLocalPos = visualRoot.localPosition;
        visualStartLocalRot = visualRoot.localRotation;

        var allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        var filtered = new List<SpriteRenderer>();

        foreach (var sr in allRenderers)
        {
            if (sr == null) continue;

            if (playerActionArea != null && sr.gameObject == playerActionArea.gameObject)
                continue;
            if (actionFlashEffect != null && sr.gameObject == actionFlashEffect.gameObject)
                continue;

            filtered.Add(sr);
        }

        spriteRenderers = filtered.ToArray();
    }

    void Update()
    {
        HandleClick();
        UpdateMovementVisuals();
        HandleAutoClick();
    }

    void HandleClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            playerActionArea.TryInteract(gameObject);
        }
    }

    // Applies a vertical bob and horizontal tilt to visualRoot while moving,
    // smoothly returning to rest when idle.
    private void UpdateMovementVisuals()
    {
        if (visualRoot == null || playerMovement == null)
            return;

        bool moving = playerMovement.IsMoving();
        Vector3 targetPos = visualStartLocalPos;
        Quaternion targetRot = visualStartLocalRot;

        if (moving)
        {
            Vector2 moveDir = playerMovement.GetMoveDirection();

            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            targetPos += new Vector3(0f, bobOffset, 0f);

            float targetZ = -moveDir.x * tiltAngle;
            targetRot = Quaternion.Euler(0f, 0f, targetZ);
        }

        float posLerp = moving ? tiltSmoothness : idleReturnSpeed;
        float rotLerp = moving ? tiltSmoothness : idleReturnSpeed;

        visualRoot.localPosition = Vector3.Lerp(
            visualRoot.localPosition,
            targetPos,
            Time.deltaTime * posLerp
        );

        visualRoot.localRotation = Quaternion.Lerp(
            visualRoot.localRotation,
            targetRot,
            Time.deltaTime * rotLerp
        );
    }

    public void SetLevelStarted(bool value)
    {
        levelStarted = value;
    }

    private void NotifyPickup(string msg)
    {
        playerUI.ShowPickupNotification(msg);
    }

    public void ShowActionArea()
    {
        playerActionArea.ShowActionArea();
    }

    public void HideActionArea()
    {
        playerActionArea.HideActionArea();
    }

    public float GetDigPower()
    {
        return digPower;
    }

    public float GetHealth()
    {
        return health;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void AddGold(int amount)
    {
        goldManager.AddGold(amount);
    }

    public void IncreaseDigSpeed(float cooldownReduction)
    {
        playerActionArea.increaseDigSpeed(cooldownReduction);
        NotifyPickup("Temporary paw speed up!");
    }

    public void AddDigPower(float amount)
    {
        digPower += amount;
        playerUI.UpdateDigPower(digPower);

        MarkStatAsTemporaryBoosted(() => playerUI.MarkDigPowerTemporaryBoosted());

        if (levelStarted)
            NotifyPickup("Temporary dig power up!");
    }

    public void IncreaseDamage(float amount)
    {
        damage += amount;
        playerUI.UpdateDamage(damage);

        MarkStatAsTemporaryBoosted(() => playerUI.MarkDamageTemporaryBoosted());

        if (levelStarted)
            NotifyPickup("Temporary damage up!");
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        health += amount;
        playerUI.UpdateHealth(health);

        MarkStatAsTemporaryBoosted(() => playerUI.MarkHealthTemporaryBoosted());

        if (levelStarted)
            NotifyPickup("Temporary max health up!");
    }

    public void IncreaseMovementSpeed(float amount)
    {
        playerMovement.IncreaseMovementSpeed(amount);
        playerUI.UpdateSpeed(playerMovement.GetSpeed());

        MarkStatAsTemporaryBoosted(() => playerUI.MarkSpeedTemporaryBoosted());

        if (levelStarted)
            NotifyPickup("Temporary move speed up!");
    }

    public void ApplyUltimatePickup()
    {
        NotifyPickup("All stats temporarily increased!");
    }

    // Applies damage with invulnerability window. Triggers sprite flash and game over at zero HP.
    public void DamageHealth(float damage)
    {
        if (Time.time - lastDamageTime < invulnerabilityTime) return;
        lastDamageTime = Time.time;

        health -= damage;
        health = Mathf.Max(0f, health);

        playerUI.UpdateHealth(health);

        flashRoutine = StartCoroutine(InvulnerabilityFlash(invulnerabilityTime));

        if (health <= 0)
        {
            health = 0;
            gameManager.SetGameOver(true);
        }
    }

    private IEnumerator InvulnerabilityFlash(float duration)
    {
        float seg = duration / 6f;

        yield return LerpAlpha(1f, 0f, seg);
        yield return LerpAlpha(0f, 1f, seg);
        yield return LerpAlpha(1f, 0f, seg);
        yield return LerpAlpha(0f, 1f, seg);
        yield return LerpAlpha(1f, 0f, seg);
        yield return LerpAlpha(0f, 1f, seg);

        SetAlpha(1f);
        flashRoutine = null;
    }

    private IEnumerator LerpAlpha(float from, float to, float time)
    {
        if (time <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / time);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float a01)
    {
        if (spriteRenderers == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var sr = spriteRenderers[i];
            if (sr == null) continue;

            Color c = sr.color;
            c.a = a01;
            sr.color = c;
        }
    }

    public void HealHealth(float heal)
    {
        health += heal;

        if (health > maxHealth)
        {
            health = maxHealth;
        }

        playerUI.UpdateHealth(health);
    }

    public void SetCantMove(bool value)
    {
        playerMovement.SetCantMove(value);
    }

    public float GetDamage()
    {
        return damage;
    }

    public float GetSpeed()
    {
        return playerMovement.GetSpeed();
    }

    public void Respawn()
    {
        health = maxHealth;
        playerUI.UpdateHealth(health);
    }

    // Only triggers the UI colour mark when the level has started; prevents false boost
    // highlights when GameManager applies permanent upgrades before SetLevelStarted(true).
    private void MarkStatAsTemporaryBoosted(System.Action uiAction)
    {
        if (!levelStarted) return;
        uiAction?.Invoke();
    }

    // Fires TryInteract every frame when autoClickEnabled (shop upgrade). Guards against
    // all states where auto-clicking should not occur.
    void HandleAutoClick()
    {
        if (!autoClickEnabled) return;
        if (playerActionArea == null) return;
        if (gameManager == null) return;

        if (!gameManager.HasLevelStarted()) return;
        if (gameManager.IsGamePaused()) return;
        if (gameManager.IsPlayerInQuizUI()) return;
        if (gameManager.IsGameOver()) return;
        if (gameManager.IsLevelComplete()) return;

        playerActionArea.TryInteract(gameObject);
    }

    public void SetAutoClickEnabled(bool value)
    {
        autoClickEnabled = value;
    }

    public bool IsAutoClickEnabled()
    {
        return autoClickEnabled;
    }
}
