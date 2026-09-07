using UnityEngine;

// AntPathing: AI movement for AntEnemy.
// Alternates between idle wandering (random points around a home position) and chasing
// the player when within detectRadius. Uses hysteresis (loseRadius > detectRadius) to
// prevent flickering. Idle spot selection avoids positions already occupied by other ants.
// SetPaused() is called by AntEnemy during knockback and by GameManager during level pauses.
[RequireComponent(typeof(Rigidbody2D))]
public class AntPathing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField] private float detectRadius = 5f;
    [SerializeField] private float loseRadius = 7f;

    [Header("Idle Wander")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;
    [SerializeField] private float arriveDistance = 0.2f;
    [SerializeField] private int idlePointAttempts = 10;

    [Header("Idle Spot Avoidance")]
    [SerializeField] private LayerMask antLayer;
    [SerializeField] private float idleSpotCheckRadius = 0.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;

    private Vector2 homePos;
    private Vector2 idleTarget;

    private bool chasing;
    private bool paused;
    private bool waitingAtIdle;
    private float idleWaitUntil;

    private readonly Collider2D[] overlapHits = new Collider2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        homePos = rb.position;
        PickNewIdleTarget(immediate: true);
    }

    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }

    private void Update()
    {
        if (player == null || paused) return;

        float d = Vector2.Distance(rb.position, player.position);

        if (!chasing && d <= detectRadius)
        {
            chasing = true;
            waitingAtIdle = false;
        }
        else if (chasing && d >= loseRadius)
        {
            chasing = false;
            waitingAtIdle = false;
            homePos = rb.position;
            PickNewIdleTarget(immediate: true);
        }

        if (chasing) return;

        float distToIdle = Vector2.Distance(rb.position, idleTarget);

        if (!waitingAtIdle && distToIdle <= arriveDistance)
        {
            waitingAtIdle = true;
            rb.velocity = Vector2.zero;
            idleWaitUntil = Time.time + Random.Range(minIdleTime, maxIdleTime);
        }

        if (waitingAtIdle && Time.time >= idleWaitUntil)
        {
            PickNewIdleTarget(immediate: true);
            waitingAtIdle = false;
        }
    }

    private void FixedUpdate()
    {
        if (player == null || paused) return;

        if (!chasing && waitingAtIdle)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 target = chasing ? (Vector2)player.position : idleTarget;
        Vector2 toTarget = target - rb.position;

        if (!chasing && toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 dir = toTarget.normalized;
        rb.velocity = dir * moveSpeed;

        RotateToVelocity();
    }

    // Tries up to idlePointAttempts random positions within wanderRadius of homePos.
    // Prefers spots not already occupied by other ants. Falls back to an unchecked random if none found.
    private void PickNewIdleTarget(bool immediate)
    {
        Vector2 bestCandidate = rb.position;
        bool found = false;

        for (int i = 0; i < idlePointAttempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector2 candidate = homePos + offset;

            if (IsIdleSpotFree(candidate))
            {
                bestCandidate = candidate;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            bestCandidate = homePos + offset;
        }

        idleTarget = bestCandidate;

        if (!immediate)
        {
            waitingAtIdle = true;
            rb.velocity = Vector2.zero;
            idleWaitUntil = Time.time + Random.Range(minIdleTime, maxIdleTime);
        }
    }

    private bool IsIdleSpotFree(Vector2 point)
    {
        if (antLayer.value == 0)
            return true;

        int count = Physics2D.OverlapCircleNonAlloc(point, idleSpotCheckRadius, overlapHits, antLayer);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapHits[i];
            if (hit == null) continue;

            if (hit.attachedRigidbody != rb)
                return false;
        }

        return true;
    }

    public void SetPaused(bool p)
    {
        paused = p;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void RotateToVelocity()
    {
        if (rb.velocity.sqrMagnitude < 0.01f) return;

        float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)homePos : transform.position, wanderRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)idleTarget : transform.position, idleSpotCheckRadius);
    }
}
