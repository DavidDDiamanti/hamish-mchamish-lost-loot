using UnityEngine;

// IInteractable: implemented by ChestBehaviour, AntEnemy, and FlyEnemy.
// PlayerActionArea detects objects on the Interactable layer and calls Interact() on click.
// The interactor parameter is the player's GameObject, used by enemies to apply knockback direction.
public interface IInteractable
{
    void Interact(GameObject interactor);
}
