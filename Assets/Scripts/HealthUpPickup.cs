using UnityEngine;

// HealthUpPickup: ILoot that temporarily increases max health for the current run.
public class HealthUpPickup : MonoBehaviour, ILoot
{
    [SerializeField] private int healthUp = 5;

    public void Collect(Player player)
    {
        player.IncreaseMaxHealth(healthUp);
    }
}
