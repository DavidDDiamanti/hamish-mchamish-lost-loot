using UnityEngine;

// ILoot: implemented by all pickup types (GoldPickUp, GemPickUp, HealthPickup, etc.).
// Collect() is called by Pickup.OnTriggerEnter2D() when the player walks over the item.
public interface ILoot
{
    void Collect(Player player);
}
