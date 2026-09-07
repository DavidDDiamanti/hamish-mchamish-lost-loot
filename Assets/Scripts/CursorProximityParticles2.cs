using UnityEngine;

// CursorProximityParticles2: single-particle-system cursor effect that scales emission, speed,
// and color based on proximity to the nearest target. Simpler variant of CursorProximityParticles.
// Fully disabled via SetParticlesAllowed() when gameplay is paused.
public class CursorProximityParticles2 : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private float detectRadius = 5f;

    [Header("Emission")]
    [SerializeField] private float minRate = 1f;
    [SerializeField] private float maxRate = 10f;
    [SerializeField] private float fullStrengthDistance = 0.5f;

    [Header("Color")]
    [SerializeField] private Color farColor = new Color(0f, 1f, 1f);   // turquise blue
    [SerializeField] private Color nearColor = new Color(1f, 0f, 0f);    // red
    [SerializeField] private Color foundColor = new Color(1f, 0.85f, 0.2f); // gold

    [Header("Particles")]
    [SerializeField] private ParticleSystem ps;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 2f;
    [SerializeField] private float minSize = 0.8f;
    [SerializeField] private float maxSize = 1.2f;

    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MainModule main;
    private readonly Collider2D[] hits = new Collider2D[32];

    private void Awake()
    {
        if (ps == null) ps = GetComponentInChildren<ParticleSystem>(true);

        emission = ps.emission;
        main = ps.main;
    }

    // Called by GameManager whenever the player is allowed/not allowed to move.
    // When not allowed: stops and clears particles, then disables this component.
    public void SetParticlesAllowed(bool allowed)
    {
        if (allowed)
        {
            // Re-enable this script; Update() will decide whether to play based on proximity.
            enabled = true;
            return;
        }

        // Not allowed: hard stop and clear, then disable this script.
        if (ps != null)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        enabled = false;
    }

    private void Update()
    {
        Transform nearest = FindNearestTarget();
        if (nearest == null)
        {
            // No target nearby: stop emitting (but don't hard-stop existing particles)
            ps.Stop();
            return;
        }
        else
        {
            if (!ps.isPlaying) ps.Play();
        }

        float d = Vector2.Distance(transform.position, nearest.position);

        // Map distance to "closeness" (0 = far edge, 1 = very close)
        float closeness = DistanceToCloseness(d);

        // Emission: rate and speed low -> high
        float rate = Mathf.Lerp(minRate, maxRate, closeness) * Mathf.Pow(closeness, 2f);
        float finalrate = rate;
        float speed = Mathf.Lerp(minSpeed, maxSpeed, closeness) * Mathf.Pow(closeness, 2f);
        float finalspeed = speed;
        float size = Mathf.Lerp(minSize, maxSize, closeness);
        float finalSize = size;

        if (closeness >= 0.8f && closeness < 1f)
        {
            finalrate *= 2f;
            finalspeed *= 2f;
        }
        else if (closeness == 1f)
        {
            finalrate *= 3.5f;
            finalspeed *= 3.5f;
        }

        emission.rateOverTime = finalrate;
        main.startSpeed = finalspeed;
        main.startSize = finalSize;

        // Color from farColor to nearColor
        if (closeness < 1f)
        {
            Color c = Color.Lerp(farColor, nearColor, closeness);
            main.startColor = new ParticleSystem.MinMaxGradient(c);
        }
        else
        {
            main.startColor = new ParticleSystem.MinMaxGradient(nearColor, foundColor);
        }
    }

    public void SetPaused(bool isPaused)
    {
        emission.enabled = !isPaused;
    }

    private Transform FindNearestTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, detectRadius, hits, targetLayers);
        if (count <= 0) return null;

        Transform nearest = null;
        float bestSqr = float.PositiveInfinity;
        Vector2 cursorPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null) continue;

            Vector2 p = hits[i].transform.position;
            float sqr = (p - cursorPos).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = hits[i].transform;
            }
        }

        return nearest;
    }

    private float DistanceToCloseness(float distance)
    {
        if (distance <= fullStrengthDistance) return 1f;
        if (distance >= detectRadius) return 0f;

        float t = Mathf.InverseLerp(detectRadius, fullStrengthDistance, distance);
        return Mathf.Clamp01(t);
    }
}