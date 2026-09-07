using UnityEngine;

// GemPickUp: ILoot that adds gems to the run delta via GemsManager.AddGems().
public class GemPickUp : MonoBehaviour, ILoot
{
    [SerializeField] private int gemAmount = 1;

    public void Collect(Player player)
    {
        if (GemsManager.Instance != null)
            GemsManager.Instance.AddGems(gemAmount);
    }
}
