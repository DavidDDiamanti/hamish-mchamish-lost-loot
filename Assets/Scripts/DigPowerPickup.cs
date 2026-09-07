using UnityEngine;

// DigPowerPickup: ILoot that temporarily increases dig power for the current run.
// Higher dig power advances ChestBehaviour.amountDug faster per interact.
public class DigPowerPickup : MonoBehaviour, ILoot
{
    [SerializeField] private float digPowerAmount = 1f;

    public void Collect(Player player)
    {
        player.AddDigPower(digPowerAmount);
        Debug.Log($"Player dig power increased by {digPowerAmount}");
    }
}
