using UnityEngine;
using System.Collections;

// Pickup: base component for all droppable items. Handles bobbing animation, trigger detection,
// particle effects, and calling Collect() on all ILoot components attached to the same GameObject.
// Multiple ILoot components can be stacked on one prefab (e.g. gold + health together).
// pickupParticles are detached from the GameObject before it is destroyed so the effect
// finishes playing after the pickup disappears.
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    [Header("Loot")]
    private ILoot[] loots;
    private bool collected;

    [Header("Bobbing")]
    [SerializeField] private bool bob = true;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobFrequency = 1.5f;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Particles")]
    [SerializeField] private ParticleSystem idleParticles;
    [SerializeField] private ParticleSystem pickupParticles;

    private Collider2D col;
    private Vector3 startLocalPos;

    private void Awake()
    {
        loots = GetComponents<ILoot>();

        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        if (collected) return;
        if (!bob) return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float y = Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;

        transform.localPosition = startLocalPos + new Vector3(0f, y, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<Player>();
        if (player == null) return;

        collected = true;

        for (int i = 0; i < loots.Length; i++)
            loots[i]?.Collect(player);

        if (idleParticles != null)
            idleParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (pickupParticles != null)
        {
            pickupParticles.transform.SetParent(null, true);
            pickupParticles.Play();
            Destroy(pickupParticles.gameObject, pickupParticles.main.duration);
        }

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (!collected)
            transform.localPosition = startLocalPos;
    }
}
