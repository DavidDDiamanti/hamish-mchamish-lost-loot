using System.Collections.Generic;
using UnityEngine;

// CosmeticCatalog: ScriptableObject holding all CosmeticItemDefinitions.
// Referenced by PlayerCosmetics and CosmeticShopUI to look up items by ID.
[CreateAssetMenu(menuName = "Cosmetics/Cosmetic Catalog", fileName = "CosmeticCatalog")]
public class CosmeticCatalog : ScriptableObject
{
    public List<CosmeticItemDefinition> items = new();

    public CosmeticItemDefinition GetById(string id)
    {
        if (items == null) return null;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].id == id)
                return items[i];
        }

        return null;
    }
}
