using UnityEngine;

// SpeedUpPickup: ILoot that temporarily increases the player's movement speed for the current run.
public class SpeedUpPickup : MonoBehaviour, ILoot
{
    [SerializeField] private float speedAmount = 5f;

    public void Collect(Player player)
    {
        player.IncreaseMovementSpeed(speedAmount);
    }
}
