using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform unitParent;

    [Header("Units To Spawn")]
    [SerializeField] private List<UnitSpawnEntry> unitsToSpawn = new List<UnitSpawnEntry>();

    private List<GridUnit> spawnedUnits = new List<GridUnit>();

    public IReadOnlyList<GridUnit> SpawnedUnits => spawnedUnits;
    public GridUnit SpawnedUnit => spawnedUnits.Count > 0 ? spawnedUnits[0] : null;

    private void Start()
    {
        SpawnAllPlayerUnits();
    }

    private void SpawnAllPlayerUnits()
    {
        spawnedUnits.Clear();

        if (gridManager == null)
        {
            Debug.LogError("UnitSpawner: Missing GridManager reference.");
            return;
        }

        if (unitsToSpawn == null || unitsToSpawn.Count == 0)
        {
            Debug.LogWarning("UnitSpawner: No player units configured to spawn.");
            return;
        }

        foreach (UnitSpawnEntry entry in unitsToSpawn)
        {
            SpawnSingleUnit(entry);
        }
    }

    private void SpawnSingleUnit(UnitSpawnEntry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("UnitSpawner: Found a null UnitSpawnEntry.");
            return;
        }

        if (entry.unitData == null)
        {
            Debug.LogWarning("UnitSpawner: Found an entry with no UnitData assigned.");
            return;
        }

        if (entry.unitData.unitPrefab == null)
        {
            Debug.LogWarning($"UnitSpawner: UnitData {entry.unitData.unitName} has no unitPrefab assigned.");
            return;
        }

        GridTile spawnTile = gridManager.GetTileAt(entry.spawnGridPosition);

        if (spawnTile == null)
        {
            Debug.LogWarning($"UnitSpawner: No tile found at {entry.spawnGridPosition} for unit {entry.unitData.unitName}.");
            return;
        }

        if (!spawnTile.isWalkable || spawnTile.isOccupied)
        {
            Debug.LogWarning($"UnitSpawner: Invalid spawn tile at {entry.spawnGridPosition} for unit {entry.unitData.unitName}.");
            return;
        }

        GameObject spawnedObject = Instantiate(entry.unitData.unitPrefab, unitParent);
        GridUnit gridUnit = spawnedObject.GetComponent<GridUnit>();

        if (gridUnit == null)
        {
            Debug.LogError($"UnitSpawner: Spawned prefab {entry.unitData.unitPrefab.name} has no GridUnit component.");
            Destroy(spawnedObject);
            return;
        }

        gridUnit.InitializeFromData(entry.unitData);
        gridUnit.PlaceOnTile(spawnTile);
        spawnedUnits.Add(gridUnit);
    }
}