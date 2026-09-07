using System;
using System.Collections.Generic;
using UnityEngine;

// LootTable: ScriptableObject defining what items an enemy or chest can drop.
// chanceToDropAny gates overall drop probability. If a drop occurs, a prefab is selected
// uniformly at random from the entries list (weight field reserved for future use).
// Used by AntEnemy, FlyEnemy, and ChestBehaviour via their respective DropLoot() methods.
[CreateAssetMenu(menuName = "Loot Table")]
public class LootTable : ScriptableObject
{
    [Range(0f, 1f)]
    public float chanceToDropAny = 1f;

    [Serializable]
    public class Entry
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }

    public List<Entry> entries = new();

    // Returns false if the chance roll fails or the pool is empty.
    // Returns true and outputs a randomly selected prefab.
    public bool TryGetDrop(out GameObject prefab)
    {
        prefab = null;

        if (entries == null || entries.Count == 0)
            return false;

        if (UnityEngine.Random.value > chanceToDropAny)
            return false;

        int index = UnityEngine.Random.Range(0, entries.Count);
        prefab = entries[index].prefab;

        return prefab != null;
    }
}
