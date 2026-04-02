using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridUnit enemyUnitPrefab;
    [SerializeField] private Transform enemyParent;

    [Header("Spawn Position")]
    [SerializeField] private Vector2Int spawnGridPosition = new Vector2Int(5, 5);

    private GridUnit spawnedEnemy;

    public GridUnit SpawnedEnemy => spawnedEnemy;

    private void Start()
    {
        SpawnEnemyUnit();
    }

    private void SpawnEnemyUnit()
    {
        if (gridManager == null || enemyUnitPrefab == null)
        {
            Debug.LogError("EnemySpawner: Missing GridManager or EnemyUnitPrefab reference.");
            return;
        }

        GridTile spawnTile = gridManager.GetTileAt(spawnGridPosition);

        if (spawnTile == null)
        {
            Debug.LogError($"EnemySpawner: No tile found at {spawnGridPosition}.");
            return;
        }

        if (!spawnTile.isWalkable || spawnTile.isOccupied)
        {
            Debug.LogError($"EnemySpawner: Invalid spawn tile at {spawnGridPosition}.");
            return;
        }

        spawnedEnemy = Instantiate(enemyUnitPrefab, enemyParent);
        spawnedEnemy.PlaceOnTile(spawnTile);
    }
}