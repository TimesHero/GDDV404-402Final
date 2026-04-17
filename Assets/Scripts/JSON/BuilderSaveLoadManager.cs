using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BuilderSaveLoadManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private Transform playerUnitParent;
    [SerializeField] private Transform enemyUnitParent;

    [Header("Save Settings")]
    [SerializeField] private string fileName = "level_layout.json";

    public string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    public void SaveLevel()
    {
        LevelLayoutData layoutData = BuildLevelLayoutData();

        Debug.Log($"SAVE TEST -> tiles: {layoutData.tiles.Count}, obstacles: {layoutData.obstacles.Count}, units: {layoutData.units.Count}");

        string json = JsonUtility.ToJson(layoutData, true);
        File.WriteAllText(SavePath, json);

        Debug.Log($"Saved level JSON to: {SavePath}");
    }

    public void LoadLevel()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning($"No save file found at: {SavePath}");
            return;
        }

        string json = File.ReadAllText(SavePath);
        LevelLayoutData layoutData = JsonUtility.FromJson<LevelLayoutData>(json);

        if (layoutData == null)
        {
            Debug.LogError("Failed to parse level JSON.");
            return;
        }

        Debug.Log($"LOAD TEST -> tiles: {layoutData.tiles.Count}, obstacles: {layoutData.obstacles.Count}, units: {layoutData.units.Count}");
        Debug.Log($"Loaded level JSON from: {SavePath}");

        RebuildLevel(layoutData);
    }

    private LevelLayoutData BuildLevelLayoutData()
    {
        LevelLayoutData layoutData = new LevelLayoutData();

        layoutData.width = gridManager.Width;
        layoutData.height = gridManager.Height;

        GridTile[,] grid = gridManager.Grid;

        // Save tiles
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                GridTile tile = grid[x, y];
                if (tile == null)
                    continue;

                TileElevation tileElevation = tile.GetComponent<TileElevation>();

                TileLayoutData tileData = new TileLayoutData
                {
                    x = tile.X,
                    y = tile.Y,
                    terrainType = tile.TerrainType.ToString(),
                    elevation = tileElevation != null ? tileElevation.Elevation : 0
                };

                layoutData.tiles.Add(tileData);
            }
        }

        // Save obstacles
        foreach (PlacedObstacle placedObstacle in obstacleManager.GetPlacedObstacles())
        {
            if (placedObstacle == null || placedObstacle.ObstacleData == null)
                continue;

            ObstacleLayoutData obstacleData = new ObstacleLayoutData
            {
                obstacleName = placedObstacle.ObstacleData.name,
                originX = placedObstacle.Origin.x,
                originY = placedObstacle.Origin.y
            };

            layoutData.obstacles.Add(obstacleData);
        }

        // Save player units
        GridUnit[] playerUnits = playerUnitParent.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit unit in playerUnits)
        {
            if (unit == null || unit.CurrentTile == null || unit.UnitData == null)
                continue;

            UnitLayoutData unitData = new UnitLayoutData
            {
                unitName = unit.UnitData.unitName,
                x = unit.CurrentTile.X,
                y = unit.CurrentTile.Y,
                team = "Player"
            };

            layoutData.units.Add(unitData);
        }

        // Save enemy units
        GridUnit[] enemyUnits = enemyUnitParent.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit unit in enemyUnits)
        {
            if (unit == null || unit.CurrentTile == null || unit.UnitData == null)
                continue;

            UnitLayoutData unitData = new UnitLayoutData
            {
                unitName = unit.UnitData.unitName,
                x = unit.CurrentTile.X,
                y = unit.CurrentTile.Y,
                team = "Enemy"
            };

            layoutData.units.Add(unitData);
        }

        return layoutData;
    }

    private void RebuildLevel(LevelLayoutData layoutData)
    {
        ClearCurrentBuilderState();

        // Load obstacle assets first
        ObstacleData[] obstacleAssets = Resources.LoadAll<ObstacleData>("ObstacleTypes");
        Dictionary<string, ObstacleData> obstacleMap = new Dictionary<string, ObstacleData>();

        foreach (ObstacleData obstacle in obstacleAssets)
        {
            if (obstacle != null && !obstacleMap.ContainsKey(obstacle.name))
                obstacleMap.Add(obstacle.name, obstacle);
        }

        // Build a set of all tiles covered by obstacles
        HashSet<Vector2Int> obstacleCoveredTiles = new HashSet<Vector2Int>();

        foreach (ObstacleLayoutData obstacleData in layoutData.obstacles)
        {
            if (!obstacleMap.TryGetValue(obstacleData.obstacleName, out ObstacleData obstacleAsset))
            {
                Debug.LogWarning($"Could not find obstacle asset: {obstacleData.obstacleName}");
                continue;
            }

            for (int x = 0; x < obstacleAsset.FootprintSize.x; x++)
            {
                for (int y = 0; y < obstacleAsset.FootprintSize.y; y++)
                {
                    Vector2Int tilePos = new Vector2Int(
                        obstacleData.originX + x,
                        obstacleData.originY + y
                    );

                    obstacleCoveredTiles.Add(tilePos);
                }
            }
        }

        // First pass: elevation for all tiles
        foreach (TileLayoutData tileData in layoutData.tiles)
        {
            GridTile tile = gridManager.GetTileAt(new Vector2Int(tileData.x, tileData.y));
            if (tile == null)
                continue;

            TileElevation tileElevation = tile.GetComponent<TileElevation>();
            if (tileElevation != null)
                tileElevation.SetElevation(tileData.elevation);
        }

        // Second pass: terrain only for tiles NOT covered by obstacles
        foreach (TileLayoutData tileData in layoutData.tiles)
        {
            Vector2Int tilePos = new Vector2Int(tileData.x, tileData.y);

            if (obstacleCoveredTiles.Contains(tilePos))
                continue;

            GridTile tile = gridManager.GetTileAt(tilePos);
            if (tile == null)
                continue;

            if (System.Enum.TryParse(tileData.terrainType, out TerrainType parsedTerrain))
            {
                tile.TerrainType = parsedTerrain;
                tile.ApplyTerrainSettings();
            }
        }

        // Rebuild obstacles
        foreach (ObstacleLayoutData obstacleData in layoutData.obstacles)
        {
            if (!obstacleMap.TryGetValue(obstacleData.obstacleName, out ObstacleData obstacleAsset))
            {
                Debug.LogWarning($"Could not find obstacle asset: {obstacleData.obstacleName}");
                continue;
            }

            bool placed = obstacleManager.TryPlaceObstacle(
                obstacleAsset,
                new Vector2Int(obstacleData.originX, obstacleData.originY),
                0
            );

            Debug.Log($"LOAD obstacle '{obstacleData.obstacleName}' at ({obstacleData.originX}, {obstacleData.originY}) -> placed: {placed}");
        }

        // Load unit assets
        UnitData[] unitAssets = Resources.LoadAll<UnitData>("UnitData");
        Dictionary<string, UnitData> unitMap = new Dictionary<string, UnitData>();

        foreach (UnitData unitAsset in unitAssets)
        {
            if (unitAsset != null && !unitMap.ContainsKey(unitAsset.unitName))
                unitMap.Add(unitAsset.unitName, unitAsset);
        }

        // Rebuild units
        foreach (UnitLayoutData unitData in layoutData.units)
        {
            if (!unitMap.TryGetValue(unitData.unitName, out UnitData unitAsset))
            {
                Debug.LogWarning($"Could not find unit asset: {unitData.unitName}");
                continue;
            }

            GridTile tile = gridManager.GetTileAt(new Vector2Int(unitData.x, unitData.y));
            if (tile == null || tile.isOccupied || !tile.isWalkable)
                continue;

            Transform targetParent = unitData.team == "Enemy" ? enemyUnitParent : playerUnitParent;

            GameObject spawnedObject = Instantiate(
                unitAsset.unitPrefab,
                Vector3.zero,
                Quaternion.identity,
                targetParent
            );

            GridUnit gridUnit = spawnedObject.GetComponent<GridUnit>();

            if (gridUnit == null)
            {
                Destroy(spawnedObject);
                continue;
            }

            gridUnit.InitializeFromData(unitAsset);
            gridUnit.PlaceOnTile(tile);
        }
    }

    private void ClearCurrentBuilderState()
    {
        // Remove player units
        GridUnit[] playerUnits = playerUnitParent.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit unit in playerUnits)
        {
            if (unit == null)
                continue;

            if (unit.CurrentTile != null)
                unit.CurrentTile.SetOccupant(null);

            Destroy(unit.gameObject);
        }

        // Remove enemy units
        GridUnit[] enemyUnits = enemyUnitParent.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit unit in enemyUnits)
        {
            if (unit == null)
                continue;

            if (unit.CurrentTile != null)
                unit.CurrentTile.SetOccupant(null);

            Destroy(unit.gameObject);
        }

        // Remove obstacles
        obstacleManager.ClearAllObstacles();

        // Reset tiles
        GridTile[,] grid = gridManager.Grid;
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                GridTile tile = grid[x, y];
                if (tile == null)
                    continue;

                tile.TerrainType = TerrainType.Ground;
                tile.ApplyTerrainSettings();

                TileElevation tileElevation = tile.GetComponent<TileElevation>();
                if (tileElevation != null)
                    tileElevation.SetElevation(0);
            }
        }
    }
}