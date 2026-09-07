using System.Collections.Generic;
using UnityEngine;

// EntitySpawner: spawns chests and enemies at the start of each level.
// Called by GameManager.PrepareLevel() -> InitializeLevel().
// Chest positions are chosen randomly within areaSize, excluding a no-spawn zone around the centre
// and respecting minDistance separation between chests.
// Each chest is assigned a QuestionData drawn from the active question pool.
// Question source depends on the active profile: base profile uses the level's QuestionBank;
// custom CSV profile uses CustomCsvRunManager's pre-parsed questions.
// Enemies are spawned adjacent to each chest, scaling type and count with currentLevel/difficulty.
public class EntitySpawner : MonoBehaviour
{
    [Header("Prefab & Spawn Settings")]
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private GameObject flyPrefab;
    [SerializeField] private GameObject antPrefab;
    [SerializeField] private GameObject beetlePrefab;

    [Tooltip("Spawn area size (e.g. 100x100). Centred on Area Center.")]
    [SerializeField] private Vector2 areaSize = new Vector2(75f, 75f);

    [Tooltip("World centre of the spawn area.")]
    [SerializeField] private Vector2 areaCenter = Vector2.zero;

    [Tooltip("Minimum allowed distance between chests.")]
    [SerializeField] private float minDistance = 6f;

    [Tooltip("Area around the centre where no chests will spawn.")]
    [SerializeField] private float noSpawnZoneSize = 10f;

    [Tooltip("Max random attempts per chest before giving up.")]
    [SerializeField] private int attemptsPerChest = 40;

    [Tooltip("Spawn on XZ plane (3D top-down) instead of XY (2D).")]
    [SerializeField] private bool spawnOnXZ = false;

    [Tooltip("Fixed Y (for XZ) or Z (for XY) coordinate for spawned objects.")]
    [SerializeField] private float fixedAxisValue = 0f;

    [Tooltip("Optional parent transform for spawned chests.")]
    [SerializeField] private Transform chestParent;

    [SerializeField] private LootTable lootTable;

    private readonly List<Vector2> spawnedPoints = new List<Vector2>();
    private GameManager gameManager;
    private int currentDifficulty = 1;
    private int currentLevel = 1;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>(true);
    }

    public void InitializeLevel()
    {
        currentDifficulty = gameManager.GetCurrentDifficulty();
        currentLevel = gameManager.GetCurrentLevel();
        SpawnChests(gameManager.GetNumberOfUnopenedChests());
    }

    public void SpawnChests(int chestCount)
    {
        Debug.Log("Spawning Chests...");
        if (chestPrefab == null)
        {
            Debug.LogError("GameManager: Chest Prefab not assigned.");
            return;
        }

        spawnedPoints.Clear();

        List<QuestionData> selectedQuestions = BuildQuestionSelection(chestCount);

        int spawned = 0;
        int failed = 0;

        for (int i = 0; i < chestCount; i++)
        {
            bool placed = TryGetValidPoint(out Vector2 p);

            if (!placed)
            {
                failed++;
                continue;
            }

            spawnedPoints.Add(p);

            Vector3 worldPos = spawnOnXZ
                ? new Vector3(p.x, fixedAxisValue, p.y)
                : new Vector3(p.x, p.y, fixedAxisValue);

            GameObject chestObj = Instantiate(chestPrefab, worldPos, Quaternion.identity, chestParent);

            ChestBehaviour chest = chestObj.GetComponent<ChestBehaviour>();
            if (chest != null)
            {
                chest.SetLootTable(lootTable);
                if (i < selectedQuestions.Count)
                {
                    chest.SetQuestion(selectedQuestions[i]);
                }
            }

            if (currentDifficulty >= 1)
            {
                SpawnEnemies(worldPos);
            }

            spawned++;
        }

        if (failed > 0)
        {
            Debug.LogWarning($"GameManager: Spawned {spawned}/{chestCount} chests. {failed} failed due to spacing constraints. " +
                             $"Try lowering minDistance or increasing areaSize/attemptsPerChest.");
        }
    }

    private bool TryGetValidPoint(out Vector2 point)
    {
        float halfW = areaSize.x * 0.5f;
        float halfH = areaSize.y * 0.5f;
        float minDistSqr = minDistance * minDistance;

        float noSpawnHalfW = noSpawnZoneSize;
        float noSpawnHalfH = noSpawnZoneSize;

        float minNoX = areaCenter.x - noSpawnHalfW;
        float maxNoX = areaCenter.x + noSpawnHalfW;
        float minNoY = areaCenter.y - noSpawnHalfH;
        float maxNoY = areaCenter.y + noSpawnHalfH;

        for (int attempt = 0; attempt < attemptsPerChest; attempt++)
        {
            float x = Random.Range(areaCenter.x - halfW, areaCenter.x + halfW);
            float y = Random.Range(areaCenter.y - halfH, areaCenter.y + halfH);
            Vector2 candidate = new Vector2(x, y);

            if (candidate.x >= minNoX && candidate.x <= maxNoX &&
                candidate.y >= minNoY && candidate.y <= maxNoY)
            {
                continue;
            }

            bool ok = true;
            for (int j = 0; j < spawnedPoints.Count; j++)
            {
                if ((spawnedPoints[j] - candidate).sqrMagnitude < minDistSqr)
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                point = candidate;
                return true;
            }
        }

        point = default;
        return false;
    }

    // Spawns ant enemies at every difficulty level, beetles from level 2+, flies from level 3+.
    // Count equals currentDifficulty, so higher difficulty means more enemies per chest.
    private void SpawnEnemies(Vector3 worldPos)
    {
        for (int i = 0; i < currentDifficulty; i++)
        {
            if (antPrefab != null)
            {
                Vector3 antPos = GetRandomOffsetPosition(worldPos, 1.5f);
                GameObject ant = Instantiate(antPrefab, antPos, Quaternion.identity, null);

                AntEnemy antScript = ant.GetComponent<AntEnemy>();
                if (antScript != null)
                {
                    antScript.SetDifficulty(gameManager.GetCurrentDifficulty());
                }
            }

            if (currentLevel >= 2 && beetlePrefab != null)
            {
                Vector3 beetlePos = GetRandomOffsetPosition(worldPos, 1.5f);
                GameObject beetle = Instantiate(beetlePrefab, beetlePos, Quaternion.identity, null);

                AntEnemy beetleScript = beetle.GetComponent<AntEnemy>();
                if (beetleScript != null)
                {
                    beetleScript.SetDifficulty(gameManager.GetCurrentDifficulty());
                }
            }

            if (currentLevel >= 3 && flyPrefab != null)
            {
                Vector3 flyPos = GetRandomOffsetPosition(worldPos, 1.5f);
                GameObject fly = Instantiate(flyPrefab, flyPos, Quaternion.identity, null);

                FlyEnemy flyScript = fly.GetComponent<FlyEnemy>();
                if (flyScript != null)
                {
                    flyScript.SetDifficulty(gameManager.GetCurrentDifficulty());
                }
            }
        }
    }

    private Vector3 GetRandomOffsetPosition(Vector3 origin, float radius)
    {
        Vector2 offset2D = Random.insideUnitCircle * radius;

        if (spawnOnXZ)
            return new Vector3(origin.x + offset2D.x, origin.y, origin.z + offset2D.y);
        else
            return new Vector3(origin.x + offset2D.x, origin.y + offset2D.y, origin.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (spawnOnXZ)
        {
            Vector3 center = new Vector3(areaCenter.x, fixedAxisValue, areaCenter.y);
            Vector3 size = new Vector3(areaSize.x, 0.1f, areaSize.y);
            Gizmos.DrawWireCube(center, size);
        }
        else
        {
            Vector3 center = new Vector3(areaCenter.x, areaCenter.y, fixedAxisValue);
            Vector3 size = new Vector3(areaSize.x, areaSize.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }

    // Builds the list of QuestionData to assign to chests.
    // Source is the CustomCsvRunManager (custom profile) or the level's QuestionBank (base profile).
    // Questions are shuffled then assigned round-robin if chestCount exceeds pool size.
    private List<QuestionData> BuildQuestionSelection(int chestCount)
    {
        List<QuestionData> result = new List<QuestionData>();

        if (gameManager == null)
        {
            Debug.LogError("EntitySpawner: GameManager missing.");
            return result;
        }

        List<QuestionData> pool = new List<QuestionData>();

        if (!GameProfileContext.IsBaseProfile &&
            CustomCsvRunManager.Instance != null &&
            CustomCsvRunManager.Instance.HasCustomRun)
        {
            var runtimeQuestions = CustomCsvRunManager.Instance.GetQuestionsForLevel(currentLevel);

            if (runtimeQuestions == null || runtimeQuestions.Count == 0)
            {
                Debug.LogError($"EntitySpawner: No custom CSV questions found for level {currentLevel}.");
                return result;
            }

            foreach (var rq in runtimeQuestions)
            {
                if (rq == null) continue;
                pool.Add(RuntimeQuestionConverterX.ToQuestionData(rq));
            }
        }
        else
        {
            QuestionBank bank = gameManager.GetQuestionBankForCurrentLevel();

            if (bank == null)
            {
                Debug.LogError("EntitySpawner: No QuestionBank assigned for current level.");
                return result;
            }

            if (bank.questions == null || bank.questions.Count == 0)
            {
                Debug.LogError($"EntitySpawner: QuestionBank '{bank.bankName}' has no questions.");
                return result;
            }

            pool = new List<QuestionData>(bank.questions);
        }

        if (pool.Count == 0)
        {
            Debug.LogError("EntitySpawner: Question pool is empty.");
            return result;
        }

        for (int i = 0; i < pool.Count; i++)
        {
            int j = Random.Range(i, pool.Count);
            QuestionData temp = pool[i];
            pool[i] = pool[j];
            pool[j] = temp;
        }

        for (int i = 0; i < chestCount; i++)
        {
            result.Add(pool[i % pool.Count]);
        }

        return result;
    }
}
