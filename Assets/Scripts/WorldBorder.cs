using UnityEngine;

// WorldBorder: procedurally creates four BoxCollider2D walls around the play area at runtime.
// Each wall is placed on the WorldBorder layer and given a semi-transparent white SpriteRenderer.
// GenerateBorders() is also exposed as a ContextMenu action for editor use.
public class WorldBorder : MonoBehaviour
{
    [Header("World Size")]
    [SerializeField] private float worldWidth = 40f;
    [SerializeField] private float worldHeight = 40f;

    [Header("Border Settings")]
    [SerializeField] private float wallThickness = 2f;
    [SerializeField] private bool generateOnAwake = true;

    private void Awake()
    {
        if (generateOnAwake)
            GenerateBorders();
    }

    [ContextMenu("Generate Borders")]
    public void GenerateBorders()
    {
        ClearChildren();

        float halfW = worldWidth * 0.5f;
        float halfH = worldHeight * 0.5f;
        float t = wallThickness;

        CreateWall("LeftWall",
            new Vector2(-halfW - t * 0.5f, 0f),
            new Vector2(t, worldHeight + 2f * t));

        CreateWall("RightWall",
            new Vector2(halfW + t * 0.5f, 0f),
            new Vector2(t, worldHeight + 2f * t));

        CreateWall("TopWall",
            new Vector2(0f, halfH + t * 0.5f),
            new Vector2(worldWidth, t));

        CreateWall("BottomWall",
            new Vector2(0f, -halfH - t * 0.5f),
            new Vector2(worldWidth, t));
    }

    private void CreateWall(string wallName, Vector2 localPos, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(transform, false);
        wall.transform.localPosition = localPos;

        wall.layer = LayerMask.NameToLayer("WorldBorder");

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = false;

        SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f)
        );

        sr.color = new Color(0.5f, 0.5f, 0.5f, 150f / 255f);

        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;

        sr.sortingOrder = -100;
    }

    [ContextMenu("Clear Borders")]
    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
