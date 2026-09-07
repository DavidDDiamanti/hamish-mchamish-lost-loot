using UnityEngine;

// PlayerMovement: handles WASD/arrow key input and Rigidbody2D velocity-based movement.
// cantMove is set by GameManager.PauseMovement() during UI states, level start gate, and pauses.
// Movement vector is normalised to prevent faster diagonal movement.
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 10f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private GameManager gameManager;
    private bool cantMove = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (cantMove)
        {
            movement = Vector2.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;
    }

    void FixedUpdate()
    {
        if (!cantMove)
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    public void IncreaseMovementSpeed(float amount)
    {
        moveSpeed += amount;
    }

    // Freezes input and immediately zeroes velocity so the player stops on the same frame.
    public void SetCantMove(bool value)
    {
        cantMove = value;
        movement = Vector2.zero;
    }

    public float GetSpeed() { return moveSpeed; }

    public bool IsMoving()
    {
        return !cantMove && movement.sqrMagnitude > 0.0001f;
    }

    public Vector2 GetMoveDirection()
    {
        return movement;
    }
}
