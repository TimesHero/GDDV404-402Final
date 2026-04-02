using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform enemyParent;

    [Header("Enemies To Spawn")]
    [SerializeField] private List<UnitSpawnEntry> unitsToSpawn = new List<UnitSpawnEntry>();

    private List<GridUnit> spawnedEnemies = new List<GridUnit>();

    public IReadOnlyList<GridUnit> SpawnedEnemies => spawnedEnemies;
    public GridUnit SpawnedEnemy => spawnedEnemies.Count > 0 ? spawnedEnemies[0] : null;

    private void Start()
    {
        SpawnAllEnemyUnits();
    }

    private void SpawnAllEnemyUnits()
    {
        spawnedEnemies.Clear();

        if (gridManager == null)
        {
            Debug.LogError("EnemySpawner: Missing GridManager reference.");
            return;
        }

        if (unitsToSpawn == null || unitsToSpawn.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: No enemy units configured to spawn.");
            return;
        }

        foreach (UnitSpawnEntry entry in unitsToSpawn)
        {
            SpawnSingleEnemy(entry);
        }
    }

    private void SpawnSingleEnemy(UnitSpawnEntry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("EnemySpawner: Found a null UnitSpawnEntry.");
            return;
        }

        if (entry.unitData == null)
        {
            Debug.LogWarning("EnemySpawner: Found an entry with no UnitData assigned.");
            return;
        }

        if (entry.unitData.unitPrefab == null)
        {
            Debug.LogWarning($"EnemySpawner: UnitData {entry.unitData.unitName} has no unitPrefab assigned.");
            return;
        }

        GridTile spawnTile = gridManager.GetTileAt(entry.spawnGridPosition);

        if (spawnTile == null)
        {
            Debug.LogWarning($"EnemySpawner: No tile found at {entry.spawnGridPosition} for unit {entry.unitData.unitName}.");
            return;
        }

        if (!spawnTile.isWalkable || spawnTile.isOccupied)
        {
            Debug.LogWarning($"EnemySpawner: Invalid spawn tile at {entry.spawnGridPosition} for unit {entry.unitData.unitName}.");
            return;
        }

        GameObject spawnedObject = Instantiate(entry.unitData.unitPrefab, enemyParent);
        GridUnit gridUnit = spawnedObject.GetComponent<GridUnit>();

        if (gridUnit == null)
        {
            Debug.LogError($"EnemySpawner: Spawned prefab {entry.unitData.unitPrefab.name} has no GridUnit component.");
            Destroy(spawnedObject);
            return;
        }

        gridUnit.InitializeFromData(entry.unitData);
        gridUnit.PlaceOnTile(spawnTile);
        spawnedEnemies.Add(gridUnit);
    }
}