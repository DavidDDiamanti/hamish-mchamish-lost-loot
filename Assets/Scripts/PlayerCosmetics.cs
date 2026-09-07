using UnityEngine;

// PlayerCosmetics: reads the equipped cosmetic IDs from CosmeticSave and applies the
// corresponding sprites and tints to the shirtRenderer and kiltRenderer overlays.
// Called in both Awake and Start to handle cases where the scene loads before PlayerPrefs
// are fully ready. Equip() and Unequip() update persistence then re-apply immediately.
public class PlayerCosmetics : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private CosmeticCatalog catalog;

    [Header("Overlay Renderers")]
    [SerializeField] private SpriteRenderer shirtRenderer;
    [SerializeField] private SpriteRenderer kiltRenderer;

    private void Awake()
    {
        ApplySavedCosmetics();
    }

    private void Start()
    {
        ApplySavedCosmetics();
    }

    public void ApplySavedCosmetics()
    {
        ApplyCategory(CosmeticCategory.Shirt, shirtRenderer);
        ApplyCategory(CosmeticCategory.Kilt, kiltRenderer);
    }

    public void Equip(CosmeticItemDefinition item)
    {
        if (item == null) return;
        if (!CosmeticSave.IsOwned(item.id)) return;

        CosmeticSave.Equip(item.category, item.id);
        ApplySavedCosmetics();
    }

    public void Unequip(CosmeticCategory category)
    {
        CosmeticSave.Unequip(category);
        ApplySavedCosmetics();
    }

    private void ApplyCategory(CosmeticCategory category, SpriteRenderer targetRenderer)
    {
        if (targetRenderer == null)
            return;

        string equippedId = CosmeticSave.GetEquippedId(category);

        if (string.IsNullOrEmpty(equippedId) || catalog == null)
        {
            targetRenderer.sprite = null;
            targetRenderer.color = Color.white;
            targetRenderer.enabled = false;
            return;
        }

        CosmeticItemDefinition item = catalog.GetById(equippedId);

        if (item == null || item.cosmeticSprite == null)
        {
            targetRenderer.sprite = null;
            targetRenderer.color = Color.white;
            targetRenderer.enabled = false;
            return;
        }

        targetRenderer.sprite = item.cosmeticSprite;
        targetRenderer.color = item.tint;
        targetRenderer.enabled = true;
    }
}
