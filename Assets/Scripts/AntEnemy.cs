using System.Collections;
using UnityEngine;

// AntEnemy: ground-based enemy implementing IEnemy and IInteractable.
// Implements IInteractable so PlayerActionArea can deal damage to it on click.
// Deals contact damage to the player via trigger overlap with a rate-limit (damageInterval).
// On death: fires GameEvents.EnemyDefeated (for quests), notifies GameManager, drops loot,
// and plays a detached particle effect. SetDifficulty() scales all stats by a multiplier
// set by EntitySpawner based on the current difficulty setting.
public class AntEnemy : MonoBehaviour, IEnemy, IInteractable
{
    [SerializeField] private float maxHealth = 4f;
    [SerializeField] private TMPro.TMP_Text healthText;
    private float health;
    [SerializeField] private float healthFlashDuration = 0.35f;
    private Coroutine healthFlashRoutine;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private ParticleSystem deathEffect;

    [Header("Contact Damage")]
    [SerializeField] private float damageInterval = 0.75f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.1f;

    [Header("Loot Table")]
    [SerializeField] private LootTable lootTable;

    private Rigidbody2D rb;
    private Player player;
    private AntPathing pathing;
    private GameManager gameManager;

    private bool paused;
    private Coroutine knockbackRoutine;
    private float lastDamageTime = -Mathf.Infinity;

    void Awake()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        player = FindObjectOfType<Player>();
        gameManager = FindObjectOfType<GameManager>();
        pathing = GetComponent<AntPathing>();

        if (pathing != null)
            pathing.SetSpeed(speed);
        UpdateHealthText();
    }

    public void SetDifficulty(int difficulty)
    {
        maxHealth *= difficulty;
        health = maxHealth;
        damage *= difficulty;
        speed *= difficulty;

        if (pathing != null)
            pathing.SetSpeed(speed);
        UpdateHealthText();
    }

    public void TakeDamage(float dmg)
    {
        health -= dmg;

        if (healthFlashRoutine != null)
            StopCoroutine(healthFlashRoutine);

        healthFlashRoutine = StartCoroutine(FlashHealthText());

        ApplyKnockback();

        if (health <= 0)
            Die();

        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        if (healthText == null) return;

        healthText.text = $"{Mathf.CeilToInt(health)}/{Mathf.CeilToInt(maxHealth)}";
    }

    public void Interact(GameObject playerInteractor)
    {
        if (player != null)
            TakeDamage(player.GetDamage());
    }

    private void ApplyKnockback()
    {
        if (paused || rb == null || player == null) return;

        Vector2 dir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.up;

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(dir));
    }

    private IEnumerator KnockbackRoutine(Vector2 dir)
    {
        if (pathing != null)
            pathing.SetPaused(true);

        rb.velocity = Vector2.zero;
        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        rb.velocity = Vector2.zero;

        if (!paused && pathing != null)
            pathing.SetPaused(false);

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

    // Pauses or resumes this enemy. Also pauses AntPathing and zeroes velocity.
    // Called by GameManager.PauseEnemyMovement() and during knockback.
    public void SetPaused(bool p)
    {
        paused = p;

        if (pathing != null)
            pathing.SetPaused(p);

        if (rb != null)
            rb.velocity = Vector2.zero;
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
