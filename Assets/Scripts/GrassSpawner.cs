using System.Collections.Generic;
using UnityEngine;

// GrassSpawner: scatters decorative grass/tree prefabs at scene start using Poisson-disc-like
// spacing. Blocks placement in a configurable rectangle below each Chest tag so grass does not
// occlude them. Each instance gets a random rotation, scale, and Y-based sorting order.
public class GrassSpawner : MonoBehaviour
{
    [Header("Tree Prefabs")]
    [SerializeField] private GameObject[] grassPrefabs;
    [SerializeField] private int grassCount = 150;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 areaSize = new Vector2(75f, 75f);
    [SerializeField] private Vector2 areaCenter = Vector2.zero;

    [Header("Spacing")]
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private int maxAttempts = 30;

    [Header("Block Trees Below Chests")]
    [SerializeField] private bool blockBelowChests = true;
    [SerializeField] private string chestTag = "Chest";
    [SerializeField] private float chestBlockWidth = 3f;
    [SerializeField] private float chestBlockHeight = 4f;
    [SerializeField] private float chestBlockYOffset = -2f;

    [Header("Randomization")]
    [SerializeField] private bool randomRotation = true;
    [SerializeField] private bool randomScale = true;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;

    [SerializeField] private Transform grassParent;

    private readonly List<Vector2> placedPoints = new List<Vector2>();

    private void Start()
    {
        SpawnGrass();
    }

    [ContextMenu("Spawn Trees")]
    public void SpawnGrass()
    {
        if (grassPrefabs == null || grassPrefabs.Length == 0)
        {
            Debug.LogError("GrassSpawner: No prefabs assigned.");
            return;
        }

        placedPoints.Clear();

        float halfW = areaSize.x * 0.5f;
        float halfH = areaSize.y * 0.5f;
        float minDistSqr = minDistance * minDistance;

        GameObject[] chests = blockBelowChests ? GameObject.FindGameObjectsWithTag(chestTag) : null;

        int spawned = 0;

        for (int i = 0; i < grassCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float x = Random.Range(areaCenter.x - halfW, areaCenter.x + halfW);
                float y = Random.Range(areaCenter.y - halfH, areaCenter.y + halfH);
                Vector2 candidate = new Vector2(x, y);

                if (IsBlockedByChest(candidate, chests))
                    continue;

                bool valid = true;
                for (int j = 0; j < placedPoints.Count; j++)
                {
                    if ((placedPoints[j] - candidate).sqrMagnitude < minDistSqr)
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                    continue;

                placedPoints.Add(candidate);

                Vector3 pos = new Vector3(candidate.x, candidate.y, 0f);
                GameObject prefab = grassPrefabs[Random.Range(0, grassPrefabs.Length)];

                Quaternion rot = Quaternion.identity;
                if (randomRotation)
                    rot = Quaternion.Euler(0f, 0f, Random.Range(-3f, 3f));

                GameObject g = Instantiate(prefab, pos, rot, grassParent);

                if (randomScale)
                {
                    float s = Random.Range(minScale, maxScale);
                    g.transform.localScale = Vector3.one * s;
                }

                SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);

                spawned++;
                placed = true;
                break;
            }

            if (!placed)
                break;
        }

        Debug.Log($"Spawned {spawned} trees.");
    }

    private bool IsBlockedByChest(Vector2 point, GameObject[] chests)
    {
        if (!blockBelowChests || chests == null) return false;

        for (int i = 0; i < chests.Length; i++)
        {
            if (chests[i] == null) continue;

            Vector2 chestPos = chests[i].transform.position;

            Vector2 boxCenter = chestPos + new Vector2(0f, chestBlockYOffset);
            Vector2 halfSize = new Vector2(chestBlockWidth * 0.5f, chestBlockHeight * 0.5f);

            bool inside =
                point.x >= boxCenter.x - halfSize.x &&
                point.x <= boxCenter.x + halfSize.x &&
                point.y >= boxCenter.y - halfSize.y &&
                point.y <= boxCenter.y + halfSize.y;

            if (inside)
                return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!blockBelowChests) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

        GameObject[] chests = GameObject.FindGameObjectsWithTag(chestTag);
        for (int i = 0; i < chests.Length; i++)
        {
            if (chests[i] == null) continue;

            Vector3 chestPos = chests[i].transform.position;
            Vector3 boxCenter = chestPos + new Vector3(0f, chestBlockYOffset, 0f);
            Vector3 boxSize = new Vector3(chestBlockWidth, chestBlockHeight, 0.1f);

            Gizmos.DrawCube(boxCenter, boxSize);
        }
    }
}