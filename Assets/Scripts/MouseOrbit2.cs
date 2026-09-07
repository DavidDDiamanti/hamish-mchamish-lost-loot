using UnityEngine;

// MouseOrbit2: positions a child object to orbit the cursor and rotate to point toward the nearest
// chest. Combines cursor-following with target-seeking so the indicator points at chests from
// the cursor's perspective rather than the player's.
public class MouseOrbit2 : MonoBehaviour
{
    public float radius;

    [Header("Chest Targeting (like CursorProximityParticles)")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private float detectRadius = 50f;

    private readonly Collider2D[] hits = new Collider2D[32];

    // Orbit direction around the cursor (where on the circle we sit)
    private Vector3 lastDir = Vector3.right;

    // What we rotate to face (nearest chest)
    private Vector3 lastAimDir = Vector3.up;

    void Awake()
    {
        radius = transform.lossyScale.x;
    }

    void Update()
    {
        Vector3 cursorWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorWorld.z = transform.position.z;

        Transform nearest = FindNearestTarget(cursorWorld);

        if (nearest != null)
        {
            Vector3 dir = nearest.position - cursorWorld;
            dir.z = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                lastDir = dir.normalized;
        }

        transform.position = cursorWorld + lastDir;

        if (nearest != null)
        {
            Vector3 aimDir = nearest.position - transform.position;
            aimDir.z = 0f;

            if (aimDir.sqrMagnitude > 0.0001f)
                lastAimDir = aimDir.normalized;
        }

        float angle = Mathf.Atan2(lastAimDir.y, lastAimDir.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private Transform FindNearestTarget(Vector2 center)
    {
        int count = Physics2D.OverlapCircleNonAlloc(center, detectRadius, hits, targetLayers);
        if (count <= 0) return null;

        Transform nearest = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            if (hits[i] == null) continue;

            Vector2 p = hits[i].transform.position;
            float sqr = (p - center).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = hits[i].transform;
            }
        }

        return nearest;
    }
}