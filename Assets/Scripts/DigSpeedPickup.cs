using UnityEngine;

// DigSpeedPickup: ILoot that temporarily reduces the PlayerActionArea interaction cooldown.
// Passed as a cooldown reduction to PlayerActionArea.increaseDigSpeed().
public class DigSpeedPickup : MonoBehaviour, ILoot
{
    [SerializeField] private float digSpeedReduction = 0.25f;

    public void Collect(Player player)
    {
        player.IncreaseDigSpeed(digSpeedReduction);
        Debug.Log($"Player paw cooldown reduced by {digSpeedReduction}");
    }
}
