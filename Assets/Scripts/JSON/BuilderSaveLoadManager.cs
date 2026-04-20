using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BuilderSaveLoadManager : MonoBehaviour
{
    [SerializeField] private UnitPlacementService unitPlacementService;
    
    [SerializeField] private InteractableLibrary interactableLibrary;
    [SerializeField] private InteractablePlacementService interactablePlacementService;
    
    [SerializeField] private InteractableRegistry interactableRegistry;
    [SerializeField] private LevelObjectiveRegistry objectiveRegistry;
    
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private Transform playerUnitParent;
    [SerializeField] private Transform enemyUnitParent;

    [Header("Save Settings")]
    public string levelFileName = "TestLevel_01";

    public string SavePath => GetEditorLevelSavePath();
    
    public string LevelFileName => levelFileName;

    private string GetEditorLevelSavePath()
    {
        string folderPath = Path.Combine(Application.dataPath, "Resources", "LevelLayouts");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        return Path.Combine(folderPath, levelFileName + ".json");
    }
    
    public void SetLevelFileName(string newFileName)
    {
        if (string.IsNullOrWhiteSpace(newFileName))
            return;

        levelFileName = SanitizeFileName(newFileName);
        Debug.Log($"Level file name set to: {levelFileName}");
    }
    
    private string SanitizeFileName(string rawName)
    {
        string sanitized = rawName.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidChar.ToString(), "");
        }

        sanitized = sanitized.Replace(".json", "");

        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "TestLevel_01";

        return sanitized;
    }

    public void SaveLevel()
    {
        LevelLayoutData layoutData = BuildLevelLayoutData();

        Debug.Log($"SAVE TEST -> tiles: {layoutData.tiles.Count}, obstacles: {layoutData.obstacles.Count}, units: {layoutData.units.Count}");

        string json = JsonUtility.ToJson(layoutData, true);
        File.WriteAllText(SavePath, json);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

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

        foreach (PlacedObstacle placedObstacle in obstacleManager.GetPlacedObstacles())
        {
            if (placedObstacle == null || placedObstacle.ObstacleData == null)
                continue;

            ObstacleLayoutData obstacleData = new ObstacleLayoutData
            {
                obstacleName = placedObstacle.ObstacleData.name,
                originX = placedObstacle.Origin.x,
                originY = placedObstacle.Origin.y,
                rotationY = placedObstacle.RotationY
            };

            layoutData.obstacles.Add(obstacleData);
        }

        GridUnit[] playerUnits = playerUnitParent.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit unit in playerUnits)
        {
            if (unit == null || unit.CurrentTile == null || unit.UnitData == null)
                continue;

            int rotationY = NormalizeRotationY(Mathf.RoundToInt(unit.transform.eulerAngles.y));

            UnitLayoutData unitData = new UnitLayoutData
            {
                unitId = unit.UnitData.UnitId,
                x = unit.CurrentTile.X,
                y = unit.CurrentTile.Y,
                rotationY = rotationY,
                team = "Player"
            };

            layoutData.units.Add(unitData);
        }

        GridUnit[] enemyUnits = enemyUnitParent.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit unit in enemyUnits)
        {
            if (unit == null || unit.CurrentTile == null || unit.UnitData == null)
                continue;

            int rotationY = NormalizeRotationY(Mathf.RoundToInt(unit.transform.eulerAngles.y));

            UnitLayoutData unitData = new UnitLayoutData
            {
                unitId = unit.UnitData.UnitId,
                x = unit.CurrentTile.X,
                y = unit.CurrentTile.Y,
                rotationY = rotationY,
                team = "Enemy"
            };

            layoutData.units.Add(unitData);
        }
        layoutData.interactables = BuildInteractableLayoutData();
        layoutData.objectives = BuildObjectiveLayoutData();

        return layoutData;
    }

    private void RebuildLevel(LevelLayoutData layoutData)
    {
        ClearCurrentBuilderState();

        gridManager.RebuildGrid(layoutData.width, layoutData.height);

        ObstacleData[] obstacleAssets = Resources.LoadAll<ObstacleData>("ObstacleTypes");
        Dictionary<string, ObstacleData> obstacleMap = new Dictionary<string, ObstacleData>();

        foreach (ObstacleData obstacle in obstacleAssets)
        {
            if (obstacle != null && !obstacleMap.ContainsKey(obstacle.name))
                obstacleMap.Add(obstacle.name, obstacle);
        }

        HashSet<Vector2Int> obstacleCoveredTiles = new HashSet<Vector2Int>();

        foreach (ObstacleLayoutData obstacleData in layoutData.obstacles)
        {
            if (!obstacleMap.TryGetValue(obstacleData.obstacleName, out ObstacleData obstacleAsset))
            {
                Debug.LogWarning($"Could not find obstacle asset: {obstacleData.obstacleName}");
                continue;
            }

            List<Vector2Int> rotatedOffsets = GetRotatedFootprintOffsets(
                obstacleAsset.FootprintSize,
                obstacleData.rotationY
            );

            foreach (Vector2Int offset in rotatedOffsets)
            {
                Vector2Int tilePos = new Vector2Int(
                    obstacleData.originX + offset.x,
                    obstacleData.originY + offset.y
                );

                obstacleCoveredTiles.Add(tilePos);
            }
        }

        foreach (TileLayoutData tileData in layoutData.tiles)
        {
            GridTile tile = gridManager.GetTileAt(new Vector2Int(tileData.x, tileData.y));
            if (tile == null)
                continue;

            TileElevation tileElevation = tile.GetComponent<TileElevation>();
            if (tileElevation != null)
                tileElevation.SetElevation(tileData.elevation);
        }

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
                obstacleData.rotationY
            );

            Debug.Log($"LOAD obstacle '{obstacleData.obstacleName}' at ({obstacleData.originX}, {obstacleData.originY}), rot {obstacleData.rotationY} -> placed: {placed}");
        }
        
        LoadInteractablesFromLayout(layoutData);

        UnitData[] playerUnitAssets = Resources.LoadAll<UnitData>("UnitData/Player");
        UnitData[] enemyUnitAssets = Resources.LoadAll<UnitData>("UnitData/Enemy");

        Dictionary<string, UnitData> unitMap = new Dictionary<string, UnitData>();

        foreach (UnitData unitAsset in playerUnitAssets)
        {
            if (unitAsset != null && !string.IsNullOrEmpty(unitAsset.UnitId) && !unitMap.ContainsKey(unitAsset.UnitId))
                unitMap.Add(unitAsset.UnitId, unitAsset);
        }

        foreach (UnitData unitAsset in enemyUnitAssets)
        {
            if (unitAsset != null && !string.IsNullOrEmpty(unitAsset.UnitId) && !unitMap.ContainsKey(unitAsset.UnitId))
                unitMap.Add(unitAsset.UnitId, unitAsset);
        }

        foreach (UnitLayoutData unitData in layoutData.units)
        {
            if (!unitMap.TryGetValue(unitData.unitId, out UnitData unitAsset))
            {
                Debug.LogWarning($"Could not find unit asset: {unitData.unitId}");
                continue;
            }

            GridTile tile = gridManager.GetTileAt(new Vector2Int(unitData.x, unitData.y));
            if (tile == null)
                continue;

            if (unitPlacementService == null)
            {
                Debug.LogError("BuilderSaveLoadManager: UnitPlacementService is missing.");
                return;
            }

            BuilderUnitPaintTeam team = unitData.team == "Enemy"
                ? BuilderUnitPaintTeam.Enemy
                : BuilderUnitPaintTeam.Player;

            bool placed = unitPlacementService.TryPlaceUnit(
                unitAsset,
                tile,
                unitData.rotationY,
                team
            );

            if (!placed)
            {
                Debug.LogWarning($"Failed to load unit '{unitData.unitId}' at ({unitData.x}, {unitData.y})");
            }
        }
    }
    
    public void ClearBuilderBeforeGridResize()
    {
        ClearCurrentBuilderState();
    }

    private void ClearCurrentBuilderState()
    {
        GridUnit[] playerUnits = playerUnitParent.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit unit in playerUnits)
        {
            if (unit == null)
                continue;

            if (unit.CurrentTile != null)
                unit.CurrentTile.SetOccupant(null);

            Destroy(unit.gameObject);
        }

        GridUnit[] enemyUnits = enemyUnitParent.GetComponentsInChildren<GridUnit>();
        foreach (GridUnit unit in enemyUnits)
        {
            if (unit == null)
                continue;

            if (unit.CurrentTile != null)
                unit.CurrentTile.SetOccupant(null);

            Destroy(unit.gameObject);
        }

        obstacleManager.ClearAllObstacles();
        interactablePlacementService?.ClearAllInteractables();

        GridTile[,] grid = gridManager.Grid;
        if (grid == null)
            return;

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

    private int NormalizeRotationY(int rotationY)
    {
        rotationY %= 360;
        if (rotationY < 0)
            rotationY += 360;

        if (rotationY >= 315 || rotationY < 45) return 0;
        if (rotationY >= 45 && rotationY < 135) return 90;
        if (rotationY >= 135 && rotationY < 225) return 180;
        return 270;
    }

    private List<Vector2Int> GetRotatedFootprintOffsets(Vector2Int footprintSize, int rotationY)
    {
        List<Vector2Int> offsets = new List<Vector2Int>();

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int offset = new Vector2Int(x, y);

                switch (NormalizeRotationY(rotationY))
                {
                    case 90:
                        offset = new Vector2Int(y, -x);
                        break;
                    case 180:
                        offset = new Vector2Int(-x, -y);
                        break;
                    case 270:
                        offset = new Vector2Int(-y, x);
                        break;
                }

                offsets.Add(offset);
            }
        }

        return offsets;
    }
    
    public List<string> GetAvailableLevelFileNames()
    {
        List<string> fileNames = new List<string>();

        string folderPath = Path.Combine(Application.dataPath, "Resources", "LevelLayouts");

        if (!Directory.Exists(folderPath))
            return fileNames;

        string[] files = Directory.GetFiles(folderPath, "*.json");

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            fileNames.Add(fileName);
        }

        fileNames.Sort();
        return fileNames;
    }

    public void LoadLevelByFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        levelFileName = fileName;
        LoadLevel();
    }
    
    private List<InteractableLayoutData> BuildInteractableLayoutData()
    {
        List<InteractableLayoutData> result = new List<InteractableLayoutData>();

        if (interactableRegistry == null)
            return result;

        List<PlacedInteractable> placedInteractables = interactableRegistry.GetAllPlacedInteractables();
        if (placedInteractables == null)
            return result;

        foreach (PlacedInteractable placed in placedInteractables)
        {
            if (placed == null || placed.Data == null)
                continue;

            InteractableLayoutData data = new InteractableLayoutData
            {
                interactableId = placed.Data.interactableId,
                x = placed.Origin.x,
                y = placed.Origin.y,
                rotationY = placed.RotationY
            };

            result.Add(data);
        }

        return result;
    }

    private List<ObjectiveLayoutData> BuildObjectiveLayoutData()
    {
        List<ObjectiveLayoutData> result = new List<ObjectiveLayoutData>();

        if (objectiveRegistry == null)
            return result;

        List<LevelObjectiveData> objectives = objectiveRegistry.GetObjectives();
        if (objectives == null)
            return result;

        foreach (LevelObjectiveData objective in objectives)
        {
            if (objective == null)
                continue;

            ObjectiveLayoutData data = new ObjectiveLayoutData
            {
                winConditionType = objective.winConditionType,
                surviveTurnCount = objective.surviveTurnCount,
                targetInteractableId = objective.targetInteractableId
            };

            if (objective.targetGridPositions != null && objective.targetGridPositions.Count > 0)
            {
                foreach (Vector2Int targetPos in objective.targetGridPositions)
                {
                    data.targetTiles.Add(new ObjectiveTargetTileData
                    {
                        x = targetPos.x,
                        y = targetPos.y
                    });
                }

                // Legacy fallback fields
                data.targetX = objective.targetGridPositions[0].x;
                data.targetY = objective.targetGridPositions[0].y;
            }

            result.Add(data);
        }

        return result;
    }
    
    private void LoadInteractablesFromLayout(LevelLayoutData layoutData)
    {
        if (layoutData == null || layoutData.interactables == null)
            return;

        if (interactableLibrary == null || interactablePlacementService == null)
        {
            Debug.LogWarning("InteractableLibrary or InteractablePlacementService is missing.");
            return;
        }

        interactablePlacementService.ClearAllInteractables();

        foreach (InteractableLayoutData interactableData in layoutData.interactables)
        {
            if (interactableData == null)
                continue;

            InteractableData interactableAsset = interactableLibrary.GetById(interactableData.interactableId);
            if (interactableAsset == null)
            {
                Debug.LogWarning($"Could not find interactable asset: {interactableData.interactableId}");
                continue;
            }

            GridTile originTile = gridManager.GetTileAt(new Vector2Int(interactableData.x, interactableData.y));
            if (originTile == null)
            {
                Debug.LogWarning($"Could not find interactable tile at: ({interactableData.x}, {interactableData.y})");
                continue;
            }

            bool placed = interactablePlacementService.TryPlaceInteractable(
                interactableAsset,
                originTile,
                interactableData.rotationY
            );

            Debug.Log($"LOAD interactable '{interactableData.interactableId}' at ({interactableData.x}, {interactableData.y}), rot {interactableData.rotationY} -> placed: {placed}");
        }
    }
}