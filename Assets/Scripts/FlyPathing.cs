using UnityEngine;

// FlyPathing: AI movement for FlyEnemy with three states: Idle, Chase, and HoneIn.
// Idle: wanders randomly around a home position with random wait times between targets.
// Chase: pursues the player with Perlin-noise jitter applied to the target position,
//        giving an erratic "fly" movement feel.
// HoneIn: activated when very close to the player; moves directly without jitter.
// Hysteresis between detectRadius and loseRadius prevents state flicker.
// SetPaused() is called by FlyEnemy during knockback and by GameManager during level pauses.
[RequireComponent(typeof(Rigidbody2D))]
public class FlyPathing : MonoBehaviour
{
    public enum State { Idle, Chase, HoneIn }

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField] private float detectRadius = 6f;
    [SerializeField] private float loseRadius = 7.5f;
    [SerializeField] private float honeInRadius = 3f;

    [Header("Idle Wander")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float minWanderWait = 0.5f;
    [SerializeField] private float maxWanderWait = 1.5f;
    [SerializeField] private float arriveDistance = 0.2f;

    [Header("Jitter")]
    [SerializeField] private float jitterAmplitudeMin = 15f;
    [SerializeField] private float jitterAmplitudeMax = 30f;
    [SerializeField] private float jitterFrequency = 1f;
    [SerializeField] private float jitterLerp = 0.6f;

    [Header("Rotation")]
    [SerializeField] private bool rotateToMovement = true;
    [SerializeField] private float rotationLerpSpeed = 12f;
    [SerializeField] private float minSpeedToRotate = 0.05f;
    [SerializeField] private float spriteForwardOffset = -90f;

    private float moveSpeed = 3.5f;
    private float jitterAmplitude;
    private Vector2 jitterOffset;

    private Rigidbody2D rb;
    private State state = State.Idle;

    private Vector2 homePos;
    private Vector2 idleTarget;
    private float nextIdlePickTime;
    private float backOffTimer = 0.5f;
    private float timeSinceLastBackOff = 0f;
    private float perlinOffsetX;
    private float perlinOffsetY;
    private bool paused = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        perlinOffsetX = Random.Range(-1000f, 1000f);
        perlinOffsetY = Random.Range(-1000f, 1000f);

        jitterAmplitude = Random.Range(jitterAmplitudeMin, jitterAmplitudeMax);

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        homePos = rb.position;
        PickNewIdleTarget(immediate: true);
    }

    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    private void Update()
    {
        if (player == null) return;

        if (!paused)
        {
            float d = Vector2.Distance(rb.position, player.position);

            if (state == State.HoneIn && rb.position == (Vector2)player.position)
            {
                timeSinceLastBackOff = Time.deltaTime;
                state = State.Idle;
            }

            if (state != State.Chase && d <= detectRadius && d >= honeInRadius)
            {
                state = State.Chase;
            }
            else if (d < honeInRadius && timeSinceLastBackOff >= (backOffTimer + Time.deltaTime))
            {
                state = State.HoneIn;
            }
            else if (state == State.Chase && d >= loseRadius)
            {
                state = State.Idle;
                homePos = rb.position;
                PickNewIdleTarget(immediate: true);
            }

            if (state == State.Idle)
            {
                if (Time.time >= nextIdlePickTime)
                {
                    if (Vector2.Distance(rb.position, idleTarget) <= arriveDistance)
                    {
                        PickNewIdleTarget(immediate: false);
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;
        if (!paused)
        {
            if (state != State.HoneIn)
            {
                Vector2 baseTarget = (state == State.Chase)
                    ? (Vector2)player.position
                    : idleTarget;

                Vector2 jitteredTarget = baseTarget + GetJitterOffset();

                MoveTowards(jitteredTarget);
            }
            else
            {
                MoveTowards(player.position);
            }

            if (rotateToMovement && state == State.Idle) RotateToVelocity();
            else if (rotateToMovement && state != State.Idle) RotateToPlayer();
        }
    }

    // Smooth jitter using Perlin noise so each fly has unique, non-repeating movement.
    private Vector2 GetJitterOffset()
    {
        float t = Time.time * jitterFrequency;

        float nx = Mathf.PerlinNoise(perlinOffsetX + t, perlinOffsetX) * 2f - 1f;
        float ny = Mathf.PerlinNoise(perlinOffsetY, perlinOffsetY + t) * 2f - 1f;

        Vector2 target = new Vector2(nx, ny) * jitterAmplitude;

        jitterOffset = Vector2.Lerp(jitterOffset, target, jitterLerp);
        return jitterOffset;
    }

    private void MoveTowards(Vector2 targetPos)
    {
        Vector2 toTarget = targetPos - rb.position;

        if (state == State.Idle && toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 dir = toTarget.normalized;
        rb.velocity = dir * moveSpeed;
    }

    private void PickNewIdleTarget(bool immediate)
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        idleTarget = homePos + offset;

        float wait = immediate ? 0f : Random.Range(minWanderWait, maxWanderWait);
        nextIdlePickTime = Time.time + wait;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)homePos : transform.position, wanderRadius);
    }

    public void SetPaused(bool isPaused)
    {
        paused = isPaused;
        rb.velocity = Vector2.zero;
    }

    private void RotateToVelocity()
    {
        Vector2 v = rb.velocity;

        if (v.sqrMagnitude < (minSpeedToRotate * minSpeedToRotate))
            return;

        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + spriteForwardOffset;

        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationLerpSpeed * Time.fixedDeltaTime);
    }

    private void RotateToPlayer()
    {
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (toPlayer.sqrMagnitude < (minSpeedToRotate * minSpeedToRotate))
            return;

        float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg + spriteForwardOffset;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationLerpSpeed * Time.fixedDeltaTime);
    }
}
