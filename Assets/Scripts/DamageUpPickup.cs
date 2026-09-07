using UnityEngine;

// DamageUpPickup: ILoot that temporarily increases the player's attack damage for the current run.
public class DamageUpPickup : MonoBehaviour, ILoot
{
    [SerializeField] private float damageAmount = 1;

    public void Collect(Player player)
    {
        player.IncreaseDamage(damageAmount);
    }
}
