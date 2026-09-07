using UnityEngine;

// HealthPickup: ILoot that restores health up to the player's current max health.
public class HealthPickup : MonoBehaviour, ILoot
{
    [SerializeField] private float healAmount = 10f;

    public void Collect(Player player)
    {
        player.HealHealth(healAmount);
    }
}
