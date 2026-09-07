using UnityEngine;

// GoldPickUp: ILoot that adds gold to the run delta via Player.AddGold() -> GoldManager.AddGold().
public class GoldPickUp : MonoBehaviour, ILoot
{
    [SerializeField] private int goldAmount = 10;

    public void Collect(Player player)
    {
        player.AddGold(goldAmount);
    }
}
