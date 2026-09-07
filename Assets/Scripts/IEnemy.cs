using UnityEngine;

// IEnemy: implemented by AntEnemy and FlyEnemy.
// TakeDamage() is called by PlayerActionArea when the player clicks on an enemy.
// SetPaused() is called by GameManager.PauseMovement() to freeze enemies during UI states.
public interface IEnemy
{
    void TakeDamage(float damage);

    void SetPaused(bool paused);
}
