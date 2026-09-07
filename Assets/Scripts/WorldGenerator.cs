using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// WorldGenerator: procedurally fills a Tilemap with ground tiles at level load.
// Applies a level-specific colour tint to the tilemap (level1=white, level2=blue, level3=purple).
// PickValidVariant() prevents identical tile+rotation combinations from appearing in adjacent
// 3x3 areas, producing visual variety. Falls back to a random variant if no valid candidate exists.
public class WorldGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private TileBase[] groundTiles;

    [Header("World Size")]
    [SerializeField] private int worldSizeUnits = 40;
    [SerializeField] private int tileSizeUnits = 5;

    [Header("Generation")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private int seed = 0;
    [SerializeField] private bool useRandomSeed = true;

    [Header("Random Rotation")]
    [SerializeField] private bool randomRotateTiles = true;

    [Header("Level Colors")]
    [SerializeField] private Color level1Color = Color.white;
    [SerializeField] private Color level2Color = Color.blue;
    [SerializeField] private Color level3Color = new Color(0.6f, 0f, 0.8f);
    private int tilesPerAxis;

    private readonly Dictionary<Vector3Int, TileVariant> placedVariants = new();

    private void Start()
    {
        if (generateOnStart)
            GenerateWorld();
    }

    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        if (groundTilemap == null)
        {
            Debug.LogError("WorldGenerator: Ground Tilemap is not assigned.");
            return;
        }

        if (groundTiles == null || groundTiles.Length == 0)
        {
            Debug.LogError("WorldGenerator: No ground tiles assigned.");
            return;
        }

        groundTilemap.ClearAllTiles();
        placedVariants.Clear();

        GameManager gm = GameManager.Instance;
        if (gm != null && groundTilemap != null)
        {
            int level = gm.GetCurrentLevel();

            switch (level)
            {
                case 1:
                    groundTilemap.color = level1Color;
                    break;

                case 2:
                    groundTilemap.color = level2Color;
                    break;

                case 3:
                    groundTilemap.color = level3Color;
                    break;

                default:
                    groundTilemap.color = level1Color;
                    break;
            }
        }

        if (useRandomSeed)
            Random.InitState(System.Environment.TickCount);
        else
            Random.InitState(seed);

        tilesPerAxis = worldSizeUnits / tileSizeUnits;
        int halfTiles = tilesPerAxis / 2;

        int minX = -halfTiles;
        int maxX = minX + tilesPerAxis - 1;
        int minY = -halfTiles;
        int maxY = minY + tilesPerAxis - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);

                TileVariant chosen = PickValidVariant(cellPos);

                groundTilemap.SetTile(cellPos, groundTiles[chosen.tileIndex]);
                groundTilemap.SetTransformMatrix(cellPos, CreateMatrix(chosen.rotation));

                placedVariants[cellPos] = chosen;
            }
        }

        Debug.Log($"World generated: {tilesPerAxis} x {tilesPerAxis} tiles.");
    }

    // Picks a tile+rotation variant not already present in the 3x3 neighbourhood.
    // Falls back to a random variant if all combinations are already used nearby.
    private TileVariant PickValidVariant(Vector3Int cellPos)
    {
        List<TileVariant> candidates = new List<TileVariant>();

        int rotationCount = randomRotateTiles ? 4 : 1;

        for (int tileIndex = 0; tileIndex < groundTiles.Length; tileIndex++)
        {
            for (int r = 0; r < rotationCount; r++)
            {
                int angle = r * 90;
                TileVariant variant = new TileVariant(tileIndex, angle);

                if (!ExistsIn3x3Area(cellPos, variant))
                    candidates.Add(variant);
            }
        }

        if (candidates.Count == 0)
        {
            int randomTile = Random.Range(0, groundTiles.Length);
            int randomAngle = randomRotateTiles ? GetRandomRightAngle() : 0;
            return new TileVariant(randomTile, randomAngle);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool ExistsIn3x3Area(Vector3Int center, TileVariant variant)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                Vector3Int neighborPos = new Vector3Int(center.x + dx, center.y + dy, 0);

                if (placedVariants.TryGetValue(neighborPos, out TileVariant existing))
                {
                    if (existing.tileIndex == variant.tileIndex &&
                        existing.rotation == variant.rotation)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private Matrix4x4 CreateMatrix(int angle)
    {
        return Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.Euler(0f, 0f, angle),
            Vector3.one
        );
    }

    private int GetRandomRightAngle()
    {
        return Random.Range(0, 4) * 90;
    }

    [ContextMenu("Clear World")]
    public void ClearWorld()
    {
        if (groundTilemap != null)
            groundTilemap.ClearAllTiles();

        placedVariants.Clear();
    }

    private struct TileVariant
    {
        public int tileIndex;
        public int rotation;

        public TileVariant(int tileIndex, int rotation)
        {
            this.tileIndex = tileIndex;
            this.rotation = rotation;
        }
    }
}
