using System.Collections.Generic;
using UnityEngine;

public class BattleSceneLevelLoader : MonoBehaviour
{
    [Header("Level Selection")]
    [SerializeField] private string fallbackLevelFileName = "TestLevel_01";
    [SerializeField] private bool clearSelectedLevelAfterLoad = false;

    [Header("Core References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private Transform playerUnitParent;
    [SerializeField] private Transform enemyUnitParent;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private BattleStateManager battleStateManager;
    [SerializeField] private LevelObjectiveRuntimeManager objectiveRuntimeManager;

    [Header("Optional Runtime Systems")]
    [SerializeField] private InteractableLibrary interactableLibrary;
    [SerializeField] private InteractablePlacementService interactablePlacementService;

    [Header("Disable Old Spawners")]
    [SerializeField] private Behaviour playerSpawnerToDisable;
    [SerializeField] private Behaviour enemySpawnerToDisable;

    private void Awake()
    {
        if (playerSpawnerToDisable != null)
            playerSpawnerToDisable.enabled = false;

        if (enemySpawnerToDisable != null)
            enemySpawnerToDisable.enabled = false;
    }

    private void Start()
    {
        LoadConfiguredLevel();
    }

    [ContextMenu("Load Configured Level")]
    public void LoadConfiguredLevel()
    {
        string levelFileName = ResolveLevelFileName();

        if (string.IsNullOrWhiteSpace(levelFileName))
        {
            Debug.LogError("BattleSceneLevelLoader: No level file name was provided.");
            return;
        }

        TextAsset jsonAsset = Resources.Load<TextAsset>($"LevelLayouts/{levelFileName}");
        if (jsonAsset == null)
        {
            Debug.LogError($"BattleSceneLevelLoader: Could not load level JSON from Resources/LevelLayouts/{levelFileName}");
            return;
        }

        LevelLayoutData layoutData = JsonUtility.FromJson<LevelLayoutData>(jsonAsset.text);
        if (layoutData == null)
        {
            Debug.LogError($"BattleSceneLevelLoader: Failed to parse JSON for level '{levelFileName}'.");
            return;
        }

        RebuildBattleLevel(layoutData);

        if (battleStateManager != null)
            battleStateManager.ResetBattleState();
        
        if (objectiveRuntimeManager != null)
        {
            objectiveRuntimeManager.InitializeObjectives(layoutData.objectives);
            objectiveRuntimeManager.SetLoseWhenSeen(layoutData.loseWhenSeen);
        }

        if (turnManager != null)
            turnManager.StartPlayerTurn();

        if (battleStateManager != null)
            battleStateManager.CheckBattleState();

        if (clearSelectedLevelAfterLoad)
            SelectedBattleLevel.Clear();

        Debug.Log($"BattleSceneLevelLoader: Loaded level '{levelFileName}'.");
    }

    private string ResolveLevelFileName()
    {
        if (!string.IsNullOrWhiteSpace(SelectedBattleLevel.LevelFileName))
            return SelectedBattleLevel.LevelFileName;

        return fallbackLevelFileName;
    }

    private void RebuildBattleLevel(LevelLayoutData layoutData)
    {
        if (layoutData == null)
            return;

        ClearCurrentBattleState();

        gridManager.RebuildGrid(layoutData.width, layoutData.height);

        Dictionary<string, ObstacleData> obstacleMap = BuildObstacleMap();
        Dictionary<string, UnitData> playerUnitMap = BuildUnitMap("UnitData/Player");
        Dictionary<string, UnitData> enemyUnitMap = BuildUnitMap("UnitData/Enemy");

        HashSet<Vector2Int> obstacleCoveredTiles = CollectObstacleCoveredTiles(layoutData, obstacleMap);

        ApplyTileElevations(layoutData);
        ApplyTileTerrains(layoutData, obstacleCoveredTiles);
        PlaceObstacles(layoutData, obstacleMap);
        PlaceInteractables(layoutData);
        PlaceUnits(layoutData, playerUnitMap, enemyUnitMap);
    }

    private void ClearCurrentBattleState()
    {
        ClearUnitsUnderParent(playerUnitParent);
        ClearUnitsUnderParent(enemyUnitParent);

        if (obstacleManager != null)
            obstacleManager.ClearAllObstacles();

        if (interactablePlacementService != null)
            interactablePlacementService.ClearAllInteractables();
        
        if (objectiveRuntimeManager != null)
            objectiveRuntimeManager.ResetObjectives();

        GridTile[,] grid = gridManager != null ? gridManager.Grid : null;
        if (grid == null)
            return;

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                GridTile tile = grid[x, y];
                if (tile == null)
                    continue;

                tile.SetOccupant(null);
                tile.TerrainType = TerrainType.Ground;
                tile.ApplyTerrainSettings();

                TileElevation tileElevation = tile.GetComponent<TileElevation>();
                if (tileElevation != null)
                    tileElevation.SetElevation(0);
            }
        }
    }

    private void ClearUnitsUnderParent(Transform parent)
    {
        if (parent == null)
            return;

        GridUnit[] units = parent.GetComponentsInChildren<GridUnit>(true);
        foreach (GridUnit unit in units)
        {
            if (unit == null)
                continue;

            if (unit.CurrentTile != null)
                unit.CurrentTile.SetOccupant(null);

            Destroy(unit.gameObject);
        }
    }

    private Dictionary<string, ObstacleData> BuildObstacleMap()
    {
        Dictionary<string, ObstacleData> map = new Dictionary<string, ObstacleData>();
        ObstacleData[] obstacleAssets = Resources.LoadAll<ObstacleData>("ObstacleTypes");

        foreach (ObstacleData obstacle in obstacleAssets)
        {
            if (obstacle != null && !map.ContainsKey(obstacle.name))
                map.Add(obstacle.name, obstacle);
        }

        return map;
    }

    private Dictionary<string, UnitData> BuildUnitMap(string resourcesPath)
    {
        Dictionary<string, UnitData> map = new Dictionary<string, UnitData>();
        UnitData[] unitAssets = Resources.LoadAll<UnitData>(resourcesPath);

        foreach (UnitData unit in unitAssets)
        {
            if (unit == null || string.IsNullOrWhiteSpace(unit.UnitId))
                continue;

            if (!map.ContainsKey(unit.UnitId))
                map.Add(unit.UnitId, unit);
        }

        return map;
    }

    private HashSet<Vector2Int> CollectObstacleCoveredTiles(LevelLayoutData layoutData, Dictionary<string, ObstacleData> obstacleMap)
    {
        HashSet<Vector2Int> coveredTiles = new HashSet<Vector2Int>();

        if (layoutData == null || layoutData.obstacles == null)
            return coveredTiles;

        foreach (ObstacleLayoutData obstacleData in layoutData.obstacles)
        {
            if (obstacleData == null)
                continue;

            if (!obstacleMap.TryGetValue(obstacleData.obstacleName, out ObstacleData obstacleAsset))
                continue;

            List<Vector2Int> rotatedOffsets = GetRotatedFootprintOffsets(
                obstacleAsset.FootprintSize,
                obstacleData.rotationY
            );

            foreach (Vector2Int offset in rotatedOffsets)
            {
                coveredTiles.Add(new Vector2Int(
                    obstacleData.originX + offset.x,
                    obstacleData.originY + offset.y
                ));
            }
        }

        return coveredTiles;
    }

    private void ApplyTileElevations(LevelLayoutData layoutData)
    {
        if (layoutData == null || layoutData.tiles == null)
            return;

        foreach (TileLayoutData tileData in layoutData.tiles)
        {
            GridTile tile = gridManager.GetTileAt(new Vector2Int(tileData.x, tileData.y));
            if (tile == null)
                continue;

            TileElevation tileElevation = tile.GetComponent<TileElevation>();
            if (tileElevation != null)
                tileElevation.SetElevation(tileData.elevation);
        }
    }

    private void ApplyTileTerrains(LevelLayoutData layoutData, HashSet<Vector2Int> obstacleCoveredTiles)
    {
        if (layoutData == null || layoutData.tiles == null)
            return;

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
    }

    private void PlaceObstacles(LevelLayoutData layoutData, Dictionary<string, ObstacleData> obstacleMap)
    {
        if (layoutData == null || layoutData.obstacles == null || obstacleManager == null)
            return;

        foreach (ObstacleLayoutData obstacleData in layoutData.obstacles)
        {
            if (obstacleData == null)
                continue;

            if (!obstacleMap.TryGetValue(obstacleData.obstacleName, out ObstacleData obstacleAsset))
            {
                Debug.LogWarning($"BattleSceneLevelLoader: Could not find obstacle asset '{obstacleData.obstacleName}'.");
                continue;
            }

            bool placed = obstacleManager.TryPlaceObstacle(
                obstacleAsset,
                new Vector2Int(obstacleData.originX, obstacleData.originY),
                obstacleData.rotationY
            );

            if (!placed)
                Debug.LogWarning($"BattleSceneLevelLoader: Failed to place obstacle '{obstacleData.obstacleName}'.");
        }
    }

    private void PlaceInteractables(LevelLayoutData layoutData)
    {
        if (layoutData == null || layoutData.interactables == null)
            return;

        if (interactableLibrary == null || interactablePlacementService == null)
            return;

        foreach (InteractableLayoutData interactableData in layoutData.interactables)
        {
            if (interactableData == null)
                continue;

            InteractableData interactableAsset = interactableLibrary.GetById(interactableData.interactableId);
            if (interactableAsset == null)
            {
                Debug.LogWarning($"BattleSceneLevelLoader: Could not find interactable asset '{interactableData.interactableId}'.");
                continue;
            }

            GridTile originTile = gridManager.GetTileAt(new Vector2Int(interactableData.x, interactableData.y));
            if (originTile == null)
                continue;

            bool placed = interactablePlacementService.TryPlaceInteractable(
                interactableAsset,
                originTile,
                interactableData.rotationY
            );

            if (!placed)
                Debug.LogWarning($"BattleSceneLevelLoader: Failed to place interactable '{interactableData.interactableId}'.");
        }
    }

    private void PlaceUnits(
        LevelLayoutData layoutData,
        Dictionary<string, UnitData> playerUnitMap,
        Dictionary<string, UnitData> enemyUnitMap)
    {
        if (layoutData == null || layoutData.units == null)
            return;

        foreach (UnitLayoutData unitData in layoutData.units)
        {
            if (unitData == null)
                continue;

            bool isEnemy = unitData.team == "Enemy";
            Dictionary<string, UnitData> sourceMap = isEnemy ? enemyUnitMap : playerUnitMap;

            if (!sourceMap.TryGetValue(unitData.unitId, out UnitData unitAsset))
            {
                Debug.LogWarning($"BattleSceneLevelLoader: Could not find unit asset '{unitData.unitId}' for team '{unitData.team}'.");
                continue;
            }

            GridTile originTile = gridManager.GetTileAt(new Vector2Int(unitData.x, unitData.y));
            if (originTile == null)
                continue;

            bool placed = TryPlaceBattleUnit(
                unitAsset,
                originTile,
                unitData.rotationY,
                unitData.useCardinalFacing,
                isEnemy ? enemyUnitParent : playerUnitParent
            );

            if (!placed)
                Debug.LogWarning($"BattleSceneLevelLoader: Failed to place unit '{unitData.unitId}'.");
        }
    }

    private bool TryPlaceBattleUnit(UnitData unitData, GridTile originTile, int rotationY, bool useCardinalFacing, Transform parent)
    {
        if (unitData == null || unitData.unitPrefab == null || originTile == null || parent == null)
            return false;

        List<GridTile> footprintTiles = GetFootprintTiles(originTile, unitData.footprintSize, rotationY);
        if (footprintTiles == null || footprintTiles.Count == 0)
            return false;

        foreach (GridTile tile in footprintTiles)
        {
            if (tile == null || !tile.isWalkable || tile.isOccupied)
                return false;
        }

        GameObject spawnedObject = Instantiate(unitData.unitPrefab, Vector3.zero, Quaternion.identity, parent);
        GridUnit gridUnit = spawnedObject.GetComponent<GridUnit>();

        if (gridUnit == null)
        {
            Destroy(spawnedObject);
            return false;
        }

        gridUnit.InitializeFromData(unitData);
        gridUnit.PlaceOnTile(originTile);

        int normalizedRotation = NormalizeRotation(rotationY);

        ApplyBattleUnitRotation(gridUnit, unitData, normalizedRotation, useCardinalFacing);
        gridUnit.transform.position = GetTileTopCenter(originTile) + unitData.GetVisualOffsetForRotation(normalizedRotation);
        gridUnit.transform.localScale = unitData.GetVisualScaleForRotation(normalizedRotation);

        foreach (GridTile tile in footprintTiles)
            tile.SetOccupant(gridUnit.gameObject);

        return true;
    }

    private void ApplyBattleUnitRotation(GridUnit gridUnit, UnitData unitData, int rotationY, bool useCardinalFacing)
    {
        if (gridUnit == null || unitData == null)
            return;

        int normalizedRotation = NormalizeRotation(rotationY);
        gridUnit.transform.rotation = Quaternion.Euler(
            unitData.GetVisualRotationEulerForRotation(normalizedRotation, useCardinalFacing)
        );

        if (useCardinalFacing)
            gridUnit.RestoreVisualRotation(Quaternion.Euler(0f, normalizedRotation, 0f));
    }

    private List<GridTile> GetFootprintTiles(GridTile originTile, Vector2Int footprintSize, int rotationY)
    {
        List<GridTile> result = new List<GridTile>();
        List<Vector2Int> offsets = GetRotatedFootprintOffsets(footprintSize, rotationY);

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int gridPos = new Vector2Int(originTile.X + offset.x, originTile.Y + offset.y);
            GridTile tile = gridManager.GetTileAt(gridPos);

            if (tile == null)
                return null;

            result.Add(tile);
        }

        return result;
    }

    private List<Vector2Int> GetRotatedFootprintOffsets(Vector2Int footprintSize, int rotationY)
    {
        List<Vector2Int> offsets = new List<Vector2Int>();
        int normalizedRotation = NormalizeRotation(rotationY);

        for (int x = 0; x < Mathf.Max(1, footprintSize.x); x++)
        {
            for (int y = 0; y < Mathf.Max(1, footprintSize.y); y++)
            {
                Vector2Int offset = new Vector2Int(x, y);

                switch (normalizedRotation)
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

    private Vector3 GetTileTopCenter(GridTile tile)
    {
        if (tile == null)
            return Vector3.zero;

        Renderer topRenderer = tile.GetTopRenderer();
        if (topRenderer != null)
            return topRenderer.bounds.center + Vector3.up * topRenderer.bounds.extents.y;

        return tile.transform.position;
    }

    private int NormalizeRotation(int rotationY)
    {
        rotationY %= 360;
        if (rotationY < 0)
            rotationY += 360;

        if (rotationY >= 315 || rotationY < 45) return 0;
        if (rotationY >= 45 && rotationY < 135) return 90;
        if (rotationY >= 135 && rotationY < 225) return 180;
        return 270;
    }
}
