using System.Collections.Generic;
using UnityEngine;

// ShopCatalog: ScriptableObject holding all ShopItemDefinitions for the permanent upgrade shop.
// Referenced by ShopUI to populate the item list.
[CreateAssetMenu(menuName = "Shop/Shop Catalog", fileName = "ShopCatalog")]
public class ShopCatalog : ScriptableObject
{
    public List<ShopItemDefinition> items = new();
}
