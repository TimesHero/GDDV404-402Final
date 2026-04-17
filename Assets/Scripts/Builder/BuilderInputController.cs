using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuilderInputController : MonoBehaviour
{
    private Vector3 placementRotationAnchorWorld;
    private bool isDraggingPlacementRotation;
    private GridTile placementRotationAnchorTile;
    
    [Header("Hover")]
    [SerializeField] private Color hoverColor = Color.magenta;

    private GridTile previousHoveredTile;
    
    [Header("References")]
    [SerializeField] private BuilderStateController builderStateController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private Transform playerUnitParent;
    [SerializeField] private Transform enemyUnitParent;
    [SerializeField] private BuilderUnitRegistry builderUnitRegistry;
    [SerializeField] private GridManager gridManager;

    private InputSystem_Actions inputActions;
    private Vector2 pointerPosition;
    private GridTile currentHoveredTile;

    public GridTile CurrentHoveredTile => currentHoveredTile;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Gameplay.PointerPosition.performed += OnPointerMove;
        inputActions.Gameplay.Click.started += OnLeftClickStarted;
        inputActions.Gameplay.Click.canceled += OnLeftClickCanceled;
        inputActions.Gameplay.EraseClick.performed += OnEraseClick;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.PointerPosition.performed -= OnPointerMove;
        inputActions.Gameplay.Click.started -= OnLeftClickStarted;
        inputActions.Gameplay.Click.canceled -= OnLeftClickCanceled;
        inputActions.Gameplay.EraseClick.performed -= OnEraseClick;
        inputActions.Disable();
    }

    private void Update()
    {
        if (isDraggingPlacementRotation && placementRotationAnchorTile != null)
        {
            LockHoverToPlacementAnchor();
        }
        else
        {
            UpdateHoveredTile();
        }

        UpdatePlacementKeyboardRotation();
    }
    
    private void LockHoverToPlacementAnchor()
    {
        if (placementRotationAnchorTile == null)
            return;

        if (currentHoveredTile != placementRotationAnchorTile)
        {
            if (currentHoveredTile != null)
                currentHoveredTile.ResetHighlight();

            currentHoveredTile = placementRotationAnchorTile;
        }

        currentHoveredTile.SetHoverHighlight(hoverColor);
    }

    private void OnPointerMove(InputAction.CallbackContext context)
    {
        pointerPosition = context.ReadValue<Vector2>();
    }
    
    
    private void PlaceObstacle(GridTile tile)
    {
        if (tile == null)
            return;

        if (builderStateController == null)
        {
            Debug.LogError("BuilderInputController: BuilderStateController is missing.");
            return;
        }

        if (obstacleManager == null)
        {
            Debug.LogError("BuilderInputController: ObstacleManager is missing.");
            return;
        }

        ObstacleData selectedObstacle = builderStateController.SelectedObstacleData;

        if (selectedObstacle == null)
        {
            Debug.LogWarning("No obstacle selected.");
            return;
        }

        bool placed = obstacleManager.TryPlaceObstacle(
            selectedObstacle,
            tile.GridPosition,
            builderStateController.SelectedObstacleRotationY
        );

        if (!placed)
        {
            Debug.Log($"Failed to place obstacle {selectedObstacle.name} at {tile.GridPosition}");
            return;
        }

        Debug.Log($"Placed obstacle {selectedObstacle.name} at {tile.GridPosition}");
    }

    private void UpdateHoveredTile()
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, tileLayerMask))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();

            if (tile == null)
                tile = hit.collider.GetComponentInParent<GridTile>();

            if (tile != currentHoveredTile)
            {
                if (currentHoveredTile != null)
                    currentHoveredTile.ResetHighlight();

                previousHoveredTile = currentHoveredTile;
                currentHoveredTile = tile;

                if (currentHoveredTile != null)
                    currentHoveredTile.SetHoverHighlight(hoverColor);
            }
        }
        else
        {
            if (currentHoveredTile != null)
                currentHoveredTile.ResetHighlight();

            previousHoveredTile = currentHoveredTile;
            currentHoveredTile = null;
        }
    }

    private void PaintTerrain(GridTile centerTile)
    {
        if (centerTile == null)
            return;

        List<GridTile> brushTiles = GetTilesInBrush(centerTile);

        foreach (GridTile tile in brushTiles)
        {
            if (tile == null)
                continue;

            tile.TerrainType = builderStateController.SelectedTerrainType;
            tile.ApplyTerrainSettings();
        }

        Debug.Log($"Painted {brushTiles.Count} tile(s) with terrain {builderStateController.SelectedTerrainType}");
    }

    private void EraseTile(GridTile tile)
    {
        if (tile == null)
            return;

        if (obstacleManager != null)
        {
            bool removed = obstacleManager.TryRemoveObstacleAtTile(tile.GridPosition);

            if (removed)
            {
                Debug.Log($"Removed obstacle at {tile.GridPosition}");
                return;
            }
        }
        
        tile.terrainType = TerrainType.Ground;
        tile.ApplyTerrainSettings();

        Debug.Log($"Erased tile {tile.GridPosition} to Ground");
    }
    private void OnEraseClick(InputAction.CallbackContext context)
    {
        if (currentHoveredTile == null)
            return;

        EraseAtTile(currentHoveredTile);
    }
    
    private void EraseAtTile(GridTile tile)
    {
        if (tile == null)
            return;

        if (obstacleManager != null)
        {
            bool removedObstacle = obstacleManager.TryRemoveObstacleAtTile(tile.GridPosition);

            if (removedObstacle)
            {
                Debug.Log($"Removed obstacle at {tile.GridPosition}");
                return;
            }
        }

        GridUnit unitOnTile = GetUnitOnTile(tile);
        if (unitOnTile != null)
        {
            RemoveUnit(unitOnTile, tile);
            return;
        }

        tile.TerrainType = TerrainType.Ground;
        tile.ApplyTerrainSettings();

        Debug.Log($"Reset tile {tile.GridPosition} to Ground");
    }
    
    private void PlaceUnit(GridTile tile)
    {
        if (tile == null)
            return;

        if (builderStateController == null)
        {
            Debug.LogError("BuilderInputController: BuilderStateController is missing.");
            return;
        }

        UnitData selectedUnitData = builderStateController.SelectedUnitData;

        if (selectedUnitData == null)
        {
            Debug.LogWarning("BuilderInputController: No unit data selected.");
            return;
        }

        if (selectedUnitData.unitPrefab == null)
        {
            Debug.LogWarning($"BuilderInputController: UnitData {selectedUnitData.unitName} has no prefab assigned.");
            return;
        }

        if (!tile.isWalkable)
        {
            Debug.Log($"Cannot place unit on tile {tile.GridPosition} because it is not walkable.");
            return;
        }

        if (tile.isOccupied)
        {
            Debug.Log($"Cannot place unit on tile {tile.GridPosition} because it is already occupied.");
            return;
        }
        
        Transform targetParent = builderStateController.SelectedUnitPaintTeam == BuilderUnitPaintTeam.Player
            ? playerUnitParent
            : enemyUnitParent;

        Vector3 unitEuler = Vector3.zero;
        unitEuler.y = builderStateController.SelectedUnitRotationY;

        GameObject spawnedObject = Instantiate(
            selectedUnitData.unitPrefab,
            Vector3.zero,
            Quaternion.Euler(unitEuler),
            targetParent
        );
        GridUnit gridUnit = spawnedObject.GetComponent<GridUnit>();

        if (gridUnit == null)
        {
            Debug.LogError($"BuilderInputController: Spawned prefab {selectedUnitData.unitPrefab.name} has no GridUnit component.");
            Destroy(spawnedObject);
            return;
        }

        gridUnit.InitializeFromData(selectedUnitData);
        gridUnit.PlaceOnTile(tile);
        
        PlacedBuilderUnit placedBuilderUnit = new PlacedBuilderUnit
        {
            Unit = gridUnit,
            UnitData = selectedUnitData,
            Origin = tile.GridPosition,
            FootprintSize = Vector2Int.one
        };

        placedBuilderUnit.OccupiedTiles.Add(tile);

        if (builderUnitRegistry != null)
            builderUnitRegistry.RegisterPlacedUnit(placedBuilderUnit);

        Debug.Log($"Placed unit {selectedUnitData.unitName} at {tile.GridPosition}");
    }
    
    private GridUnit GetUnitOnTile(GridTile tile)
    {
        if (tile == null || !tile.isOccupied || tile.OccupyingUnit == null)
            return null;

        return tile.OccupyingUnit.GetComponent<GridUnit>();
    }
    
    private void RemoveUnit(GridUnit unit, GridTile tile)
    {
        if (unit == null)
            return;

        if (builderUnitRegistry != null)
        {
            PlacedBuilderUnit placedUnit = builderUnitRegistry.GetPlacedUnitAtTile(tile);
            if (placedUnit != null)
            {
                foreach (GridTile occupiedTile in placedUnit.OccupiedTiles)
                {
                    if (occupiedTile == null)
                        continue;

                    if (occupiedTile.OccupyingUnit == unit.gameObject)
                        occupiedTile.SetOccupant(null);
                }

                builderUnitRegistry.UnregisterPlacedUnit(placedUnit);
            }
        }
        else
        {
            if (tile != null && tile.OccupyingUnit == unit.gameObject)
                tile.SetOccupant(null);
        }

        Destroy(unit.gameObject);

        Debug.Log($"Removed unit from tile {tile?.GridPosition}");
    }
    
    private void PaintElevation(GridTile centerTile)
    {
        if (centerTile == null)
            return;

        if (builderStateController == null)
        {
            Debug.LogError("BuilderInputController: BuilderStateController is missing.");
            return;
        }

        int selectedElevation = builderStateController.SelectedElevationValue;
        List<GridTile> brushTiles = GetTilesInBrush(centerTile);

        HashSet<PlacedObstacle> affectedObstacles = new HashSet<PlacedObstacle>();
        HashSet<PlacedBuilderUnit> affectedUnits = new HashSet<PlacedBuilderUnit>();

        foreach (GridTile tile in brushTiles)
        {
            if (tile == null)
                continue;

            if (obstacleManager != null)
            {
                PlacedObstacle placedObstacle = obstacleManager.GetPlacedObstacleAtTile(tile.GridPosition);
                if (placedObstacle != null)
                {
                    affectedObstacles.Add(placedObstacle);
                    continue;
                }
            }

            if (builderUnitRegistry != null)
            {
                PlacedBuilderUnit placedUnit = builderUnitRegistry.GetPlacedUnitAtTile(tile);
                if (placedUnit != null)
                {
                    affectedUnits.Add(placedUnit);
                    continue;
                }
            }

            TileElevation tileElevationComponent = tile.GetComponent<TileElevation>();
            if (tileElevationComponent != null)
                tileElevationComponent.SetElevation(selectedElevation);
        }

        foreach (PlacedObstacle obstacle in affectedObstacles)
        {
            if (obstacleManager != null)
                obstacleManager.SetObstacleElevation(obstacle, selectedElevation);
        }

        foreach (PlacedBuilderUnit placedUnit in affectedUnits)
        {
            SetUnitGroupElevation(placedUnit, selectedElevation);
        }

        Debug.Log($"Set elevation of {brushTiles.Count} tile(s) to {selectedElevation}");
    }
    private void SetUnitGroupElevation(PlacedBuilderUnit placedUnit, int newElevation)
    {
        if (placedUnit == null || placedUnit.Unit == null)
            return;

        foreach (GridTile occupiedTile in placedUnit.OccupiedTiles)
        {
            if (occupiedTile == null)
                continue;

            TileElevation tileElevation = occupiedTile.GetComponent<TileElevation>();
            if (tileElevation != null)
                tileElevation.SetElevation(newElevation);
        }

        placedUnit.Unit.PlaceOnTile(placedUnit.Unit.CurrentTile);
    }
    
    private List<GridTile> GetTilesInBrush(GridTile centerTile)
    {
        List<GridTile> tiles = new List<GridTile>();

        if (centerTile == null || builderStateController == null || gridManager == null)
            return tiles;

        int brushSize = Mathf.Max(1, builderStateController.BrushSize);

        int startOffsetX = -(brushSize / 2);
        int startOffsetY = -(brushSize / 2);

        int endOffsetX = startOffsetX + brushSize - 1;
        int endOffsetY = startOffsetY + brushSize - 1;

        for (int offsetX = startOffsetX; offsetX <= endOffsetX; offsetX++)
        {
            for (int offsetY = startOffsetY; offsetY <= endOffsetY; offsetY++)
            {
                Vector2Int tilePos = new Vector2Int(centerTile.X + offsetX, centerTile.Y + offsetY);

                if (!gridManager.isInsideGrid(tilePos))
                    continue;

                GridTile tile = gridManager.GetTileAt(tilePos);
                if (tile != null)
                    tiles.Add(tile);
            }
        }

        return tiles;
    }
    
    private void UpdatePlacementKeyboardRotation()
    {
        if (!isDraggingPlacementRotation || builderStateController == null)
            return;

        if (Keyboard.current == null)
            return;

        bool rotateLeft = Keyboard.current.leftArrowKey.wasPressedThisFrame;
        bool rotateRight = Keyboard.current.rightArrowKey.wasPressedThisFrame;
        bool rotateUp = Keyboard.current.upArrowKey.wasPressedThisFrame;
        bool rotateDown = Keyboard.current.downArrowKey.wasPressedThisFrame;

        switch (builderStateController.CurrentToolMode)
        {
            case BuilderToolMode.ObstaclePaint:
                if (rotateLeft)
                    builderStateController.SetSelectedObstacleRotationY(builderStateController.SelectedObstacleRotationY - 90);
                else if (rotateRight)
                    builderStateController.SetSelectedObstacleRotationY(builderStateController.SelectedObstacleRotationY + 90);
                else if (rotateUp)
                    builderStateController.SetSelectedObstacleRotationY(0);
                else if (rotateDown)
                    builderStateController.SetSelectedObstacleRotationY(180);
                break;

            case BuilderToolMode.UnitPaint:
                if (rotateLeft)
                    builderStateController.SetSelectedUnitRotationY(builderStateController.SelectedUnitRotationY - 90);
                else if (rotateRight)
                    builderStateController.SetSelectedUnitRotationY(builderStateController.SelectedUnitRotationY + 90);
                else if (rotateUp)
                    builderStateController.SetSelectedUnitRotationY(0);
                else if (rotateDown)
                    builderStateController.SetSelectedUnitRotationY(180);
                break;
        }
    }
    
    private void OnLeftClickStarted(InputAction.CallbackContext context)
    {
        if (builderStateController == null || currentHoveredTile == null)
            return;

        switch (builderStateController.CurrentToolMode)
        {
            case BuilderToolMode.TerrainPaint:
                PaintTerrain(currentHoveredTile);
                break;

            case BuilderToolMode.ElevationPaint:
                PaintElevation(currentHoveredTile);
                break;

            case BuilderToolMode.ObstaclePaint:
            case BuilderToolMode.UnitPaint:
                placementRotationAnchorTile = currentHoveredTile;
                placementRotationAnchorWorld = GetTileTopCenter(currentHoveredTile);
                isDraggingPlacementRotation = true;
                LockHoverToPlacementAnchor();
                break;

            case BuilderToolMode.Erase:
                EraseAtTile(currentHoveredTile);
                break;
        }
    }
    
    private void OnLeftClickCanceled(InputAction.CallbackContext context)
    {
        if (builderStateController == null || placementRotationAnchorTile == null)
            return;

        switch (builderStateController.CurrentToolMode)
        {
            case BuilderToolMode.ObstaclePaint:
                PlaceObstacle(placementRotationAnchorTile);
                break;

            case BuilderToolMode.UnitPaint:
                PlaceUnit(placementRotationAnchorTile);
                break;
        }

        isDraggingPlacementRotation = false;
        placementRotationAnchorTile = null;
        placementRotationAnchorWorld = Vector3.zero;
    }
    
    private Vector3 GetTileTopCenter(GridTile tile)
    {
        if (tile == null)
            return Vector3.zero;

        Renderer topRenderer = tile.GetTopRenderer();
        if (topRenderer != null)
            return topRenderer.bounds.center + Vector3.up * (topRenderer.bounds.extents.y);

        return tile.transform.position;
    }
}