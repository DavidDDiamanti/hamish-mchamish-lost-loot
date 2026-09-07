using UnityEngine;

// CursorProximityParticles: two-particle-system cursor effect that reacts to nearby IInteractable
// targets. psPoint emits scaled particles while approaching; psBurst fires when on the target.
// Emission rate, speed, and color all scale with proximity to the nearest detected collider.
public class CursorProximityParticles : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private float detectRadius = 5f;

    [Header("Emission")]
    [SerializeField] private float minRate = 1f;
    [SerializeField] private float maxRate = 10f;
    [SerializeField] private float fullStrengthDistance = 0.5f;

    [Header("Color")]
    [SerializeField] private Color farColor = new Color(0f, 1f, 1f);          // turquoise blue
    [SerializeField] private Color nearColor = new Color(1f, 0f, 0f);         // red
    [SerializeField] private Color foundColor = new Color(1f, 0.85f, 0.2f);   // gold

    [Header("Particles")]
    [SerializeField] private ParticleSystem psPoint;   // ON when closeness < 1
    [SerializeField] private ParticleSystem psBurst;   // ON when closeness == 1 (approx)
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 2f;
    [SerializeField] private float minSize = 0.8f;
    [SerializeField] private float maxSize = 1.2f;

    private ParticleSystem.EmissionModule pointEmission;
    private ParticleSystem.MainModule pointMain;

    private ParticleSystem.EmissionModule burstEmission;
    private ParticleSystem.MainModule burstMain;

    private readonly Collider2D[] hits = new Collider2D[32];

    private bool paused;

    private void Awake()
    {
        // NOTE: don't auto-assign both with GetComponentInChildren or they'll become the same system.
        // Only auto-fill if missing, but prefer explicit assignment in the Inspector.
        if (psPoint == null || psBurst == null)
        {
            var all = GetComponentsInChildren<ParticleSystem>(true);
            if (psPoint == null && all.Length > 0) psPoint = all[0];
            if (psBurst == null && all.Length > 1) psBurst = all[1];
        }

        if (psPoint != null)
        {
            pointEmission = psPoint.emission;
            pointMain = psPoint.main;
            psPoint.Play();
        }

        if (psBurst != null)
        {
            burstEmission = psBurst.emission;
            burstMain = psBurst.main;
            psBurst.Play();
        }

        // Start with no emission until we detect something
        SetEmit(point: false, burst: false);
    }

    public void SetPaused(bool isPaused)
    {
        paused = isPaused;

        // Turn off emission so particles fade out naturally (no freeze).
        if (paused)
        {
            SetEmit(point: false, burst: false);
        }
    }

    private void Update()
    {
        if (paused) return;

        Transform nearest = FindNearestTarget();
        if (nearest == null)
        {
            SetEmit(point: false, burst: false);
            return;
        }

        float d = Vector2.Distance(transform.position, nearest.position);
        float closeness = DistanceToCloseness(d);

        // float safety around 1
        bool found = closeness >= 0.999f;

        SetEmit(point: !found, burst: found);

        // Shared strength scaling
        float rate = Mathf.Lerp(minRate, maxRate, closeness) * Mathf.Pow(closeness, 2f);
        float speed = Mathf.Lerp(minSpeed, maxSpeed, closeness) * Mathf.Pow(closeness, 2f);
        float size = Mathf.Lerp(minSize, maxSize, closeness);

        float finalRate = rate;
        float finalSpeed = speed;

        if (closeness >= 0.8f && closeness < 1f)
        {
            finalRate *= 2f;
            finalSpeed *= 2f;
        }
        else if (found)
        {
            finalRate *= 2f;
            finalSpeed *= 2f;
        }

        // Apply settings to whichever one is active (safe to set both too)
        if (psPoint != null)
        {
            pointEmission.rateOverTime = finalRate;
            pointMain.startSpeed = finalSpeed;
            pointMain.startSize = size;

            if (!found)
            {
                Color c = Color.Lerp(farColor, nearColor, closeness);
                pointMain.startColor = new ParticleSystem.MinMaxGradient(c);
            }
        }

        if (psBurst != null)
        {
            burstEmission.rateOverTime = finalRate;
            burstMain.startSpeed = finalSpeed;
            burstMain.startSize = size;

            if (found)
            {
                burstMain.startColor = new ParticleSystem.MinMaxGradient(nearColor, foundColor);
            }
        }
    }

    private void SetEmit(bool point, bool burst)
    {
        if (psPoint != null) pointEmission.enabled = point;
        if (psBurst != null) burstEmission.enabled = burst;
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
            var h = hits[i];
            if (h == null) continue;

            Vector2 p = h.transform.position;
            float sqr = (p - cursorPos).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = h.transform;
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