using System.Collections;
using UnityEngine;

// FlyEnemy: airborne enemy implementing IEnemy and IInteractable.
// Behaves identically to AntEnemy but uses FlyPathing (jittery pursuit AI) and includes
// a red hit-flash effect across all child SpriteRenderers on TakeDamage().
// SetDifficulty() is called by EntitySpawner and scales health, damage, and speed.
public class FlyEnemy : MonoBehaviour, IEnemy, IInteractable
{
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private TMPro.TMP_Text healthText;
    private float health;
    [SerializeField] private float healthFlashDuration = 0.35f;
    private Coroutine healthFlashRoutine;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float speed = 3.5f;
    [SerializeField] private ParticleSystem deathEffect;

    [Header("Contact Damage")]
    [SerializeField] private float damageInterval = 0.75f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.12f;

    [Header("Hit Flash")]
    [SerializeField] private bool flashOnHit = true;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private bool includeInactiveRenderers = true;
    [SerializeField] private float restoreDuration = 0.05f;

    [Header("Loot Table")]
    [SerializeField] private LootTable lootTable;

    private bool paused = false;
    private bool inKnockback = false;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;

    private Player player;
    private FlyPathing flyPathing;
    private GameManager gameManager;
    private Rigidbody2D rb;
    private Coroutine knockbackRoutine;
    private float lastDamageTime = -Mathf.Infinity;

    void Awake()
    {
        player = FindObjectOfType<Player>();
        flyPathing = GetComponent<FlyPathing>();
        gameManager = FindObjectOfType<GameManager>();
        rb = GetComponent<Rigidbody2D>();
        health = maxHealth;

        if (rb == null) Debug.LogWarning($"Enemy '{name}' has no Rigidbody2D. Knockback will not work.");

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveRenderers);

        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
    }

    public void SetDifficulty(int difficulty)
    {
        maxHealth *= difficulty;
        health = maxHealth;
        damage *= difficulty;
        speed *= difficulty;

        if (flyPathing != null)
            flyPathing.SetSpeed(speed);
        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        if (healthText == null) return;

        healthText.text = $"{Mathf.CeilToInt(health)}/{Mathf.CeilToInt(maxHealth)}";
    }

    public void TakeDamage(float damage)
    {
        if (flashOnHit)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HitFlash());
        }

        if (healthFlashRoutine != null)
            StopCoroutine(healthFlashRoutine);

        healthFlashRoutine = StartCoroutine(FlashHealthText());

        ApplyKnockback();

        health -= damage;
        if (health <= 0)
        {
            Die();
            return;
        }
        UpdateHealthText();
    }

    // Flashes all child renderers to red then smoothly restores original colours.
    private IEnumerator HitFlash()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;

            Color c = spriteRenderers[i].color;
            c.r = 1f;
            c.g = 0f;
            c.b = 0f;
            spriteRenderers[i].color = c;
        }

        yield return new WaitForSeconds(flashDuration);

        float t = 0f;

        Color[] startColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            startColors[i] = spriteRenderers[i].color;
        }

        while (t < restoreDuration)
        {
            t += Time.deltaTime;
            float lerpT = Mathf.Clamp01(t / restoreDuration);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null) continue;

                spriteRenderers[i].color =
                    Color.Lerp(startColors[i], originalColors[i], lerpT);
            }

            yield return null;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].color = originalColors[i];
        }

        flashRoutine = null;
    }

    public void Interact(GameObject playerInteractor)
    {
        if (player != null)
            TakeDamage(player.GetDamage());
    }

    private void ApplyKnockback()
    {
        if (paused) return;
        if (rb == null) return;
        if (player == null) return;

        Vector2 dir = (Vector2)(transform.position - player.transform.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(KnockbackRoutine(dir));
    }

    private IEnumerator KnockbackRoutine(Vector2 dir)
    {
        inKnockback = true;

        if (flyPathing != null) flyPathing.SetPaused(true);

        rb.velocity = Vector2.zero;
        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        rb.velocity = Vector2.zero;

        if (!paused && flyPathing != null) flyPathing.SetPaused(false);
        inKnockback = false;
        knockbackRoutine = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            TryDamagePlayer();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            TryDamagePlayer();
    }

    private void TryDamagePlayer()
    {
        if (paused || player == null) return;
        if (Time.time - lastDamageTime < damageInterval) return;

        lastDamageTime = Time.time;
        player.DamageHealth(damage);
    }

    private void Die()
    {
        GameEvents.RaiseEnemyDefeated();
        gameManager.EnemyDied();

        DropLoot();

        if (deathEffect != null)
        {
            deathEffect.transform.SetParent(null);
            deathEffect.Play();
            Destroy(deathEffect.gameObject, deathEffect.main.duration);
        }

        Destroy(gameObject);
    }

    private void DropLoot()
    {
        if (lootTable == null) return;

        if (lootTable.TryGetDrop(out GameObject dropPrefab) && dropPrefab != null)
        {
            Instantiate(dropPrefab, transform.position, Quaternion.identity);
        }
    }

    // Pauses or resumes this enemy. Knockback state is preserved so resuming after
    // a game unpause does not interrupt an in-flight knockback.
    public void SetPaused(bool isPaused)
    {
        paused = isPaused;

        if (flyPathing != null) flyPathing.SetPaused(isPaused || inKnockback);

        if (rb != null)
        {
            if (isPaused) rb.velocity = Vector2.zero;
            rb.simulated = !isPaused;
        }
    }

    private IEnumerator FlashHealthText()
    {
        if (healthText == null) yield break;

        Color start = Color.red;
        Color end = Color.white;

        float t = 0f;

        healthText.color = start;

        while (t < healthFlashDuration)
        {
            t += Time.deltaTime;
            float lerp = t / healthFlashDuration;

            healthText.color = Color.Lerp(start, end, lerp);

            yield return null;
        }

        healthText.color = end;
        healthFlashRoutine = null;
    }
}
