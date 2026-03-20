using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridUnit playerUnitPrefab;
    [SerializeField] private Transform unitParent;

    [Header("Spawn Position")]
    [SerializeField] private Vector2Int spawnGridPosition = new Vector2Int(0, 0);

    private GridUnit spawnedUnit;

    public GridUnit SpawnedUnit => spawnedUnit;

    private void Start()
    {
        SpawnPlayerUnit();
    }

    private void SpawnPlayerUnit()
    {
        if (gridManager == null || playerUnitPrefab == null)
        {
            Debug.LogError("UnitSpawner: Missing GridManager or PlayerUnitPrefab reference.");
            return;
        }

        GridTile spawnTile = gridManager.GetTileAt(spawnGridPosition);

        if (spawnTile == null)
        {
            Debug.LogError($"UnitSpawner: No tile found at {spawnGridPosition}.");
            return;
        }

        if (!spawnTile.isWalkable || spawnTile.isOccupied)
        {
            Debug.LogError($"UnitSpawner: Invalid spawn tile at {spawnGridPosition}.");
            return;
        }

        spawnedUnit = Instantiate(playerUnitPrefab, unitParent);
        spawnedUnit.PlaceOnTile(spawnTile);
    }
}