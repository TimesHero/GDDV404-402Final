using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuilderInputController : MonoBehaviour
{
    [SerializeField] private GameObject blockingUIPanel;
    
    private readonly List<GridTile> currentBrushHoverTiles = new List<GridTile>();
    
    private Vector3 placementRotationAnchorWorld;
    private bool isDraggingPlacementRotation;
    private GridTile placementRotationAnchorTile;
    
    [Header("Hover")]
    [SerializeField] private Color hoverColor = Color.magenta;
    [SerializeField] private Color objectiveHoverColor = Color.cyan;
    [SerializeField] private Color objectiveSelectedColor = Color.green;
    [SerializeField] private Color patrolEndHoverColor = new Color(1f, 0.55f, 0.1f, 0.55f);
    [SerializeField] private Color patrolStartSelectedColor = Color.green;

    private GridTile previousHoveredTile;
    private bool isPickingObjectiveTile;
    private GridTile selectedObjectiveTile;
    private bool isPickingEnemyPatrolEndTile;
    private GridTile selectedEnemyPatrolStartTile;
    private PlacedBuilderUnit pendingEnemyPatrolUnit;
    
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
    [SerializeField] private InteractablePlacementService interactablePlacementService;
    [SerializeField] private UnitPlacementService unitPlacementService;
    [SerializeField] private BuilderUIController builderUIController;

    private InputSystem_Actions inputActions;
    private Vector2 pointerPosition;
    private GridTile currentHoveredTile;

    public GridTile CurrentHoveredTile => currentHoveredTile;
    
    public GridTile SelectedObjectiveTile => selectedObjectiveTile;
    public bool IsPickingObjectiveTile => isPickingObjectiveTile;
    public bool IsPickingEnemyPatrolEndTile => isPickingEnemyPatrolEndTile;

    public void SetObjectiveTilePickMode(bool isEnabled)
    {
        isPickingObjectiveTile = isEnabled;

        if (isPickingObjectiveTile)
            CancelEnemyPatrolEndSelection();

        if (!isPickingObjectiveTile)
        {
            selectedObjectiveTile = null;
            ClearBrushHoverHighlight();

            if (currentHoveredTile != null)
                ApplyBrushHoverHighlight(currentHoveredTile);
        }
    }

    public void ClearSelectedObjectiveTile()
    {
        selectedObjectiveTile = null;

        ClearBrushHoverHighlight();

        if (currentHoveredTile != null)
            ApplyBrushHoverHighlight(currentHoveredTile);
    }

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }
    
    private bool IsUIBlockingSceneInteraction()
    {
        bool explicitBlockingPanelOpen = blockingUIPanel != null && blockingUIPanel.activeInHierarchy;
        bool loadLevelPanelOpen = builderUIController != null && builderUIController.IsLoadLevelPanelOpen;

        return explicitBlockingPanelOpen || loadLevelPanelOpen;
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
        if (IsUIBlockingSceneInteraction())
        {
            ClearBrushHoverHighlight();
            previousHoveredTile = currentHoveredTile;
            currentHoveredTile = null;
            return;
        }
        if (isDraggingPlacementRotation && placementRotationAnchorTile != null)
        {
            LockHoverToPlacementAnchor();
        }
        else
        {
            UpdateHoveredTile();
        }

        if (isPickingObjectiveTile)
        {
            RefreshObjectiveTileSelectionVisuals();
        }
        else if (isPickingEnemyPatrolEndTile)
        {
            RefreshEnemyPatrolSelectionVisuals();
        }

        UpdatePlacementKeyboardRotation();
    }
    
    private void LockHoverToPlacementAnchor()
    {
        if (placementRotationAnchorTile == null)
            return;

        if (currentHoveredTile != placementRotationAnchorTile)
            currentHoveredTile = placementRotationAnchorTile;

        ApplyBrushHoverHighlight(placementRotationAnchorTile);
    }
    
    private void RefreshObjectiveTileSelectionVisuals()
    {
        if (currentHoveredTile != null)
        {
            currentHoveredTile.SetHoverHighlight(objectiveHoverColor);

            if (!currentBrushHoverTiles.Contains(currentHoveredTile))
                currentBrushHoverTiles.Add(currentHoveredTile);
        }

        if (selectedObjectiveTile != null)
        {
            selectedObjectiveTile.SetHoverHighlight(objectiveSelectedColor);

            if (!currentBrushHoverTiles.Contains(selectedObjectiveTile))
                currentBrushHoverTiles.Add(selectedObjectiveTile);
        }
    }

    private void RefreshEnemyPatrolSelectionVisuals()
    {
        if (currentHoveredTile != null)
        {
            currentHoveredTile.SetHoverHighlight(patrolEndHoverColor);

            if (!currentBrushHoverTiles.Contains(currentHoveredTile))
                currentBrushHoverTiles.Add(currentHoveredTile);
        }

        if (selectedEnemyPatrolStartTile != null)
        {
            selectedEnemyPatrolStartTile.SetHoverHighlight(patrolStartSelectedColor);

            if (!currentBrushHoverTiles.Contains(selectedEnemyPatrolStartTile))
                currentBrushHoverTiles.Add(selectedEnemyPatrolStartTile);
        }
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
    
    private void PlaceInteractable(GridTile tile)
    {
        if (tile == null)
            return;

        if (builderStateController == null)
        {
            Debug.LogError("BuilderInputController: BuilderStateController is missing.");
            return;
        }

        if (interactablePlacementService == null)
        {
            Debug.LogError("BuilderInputController: InteractablePlacementService is missing.");
            return;
        }

        InteractableData selectedInteractable = builderStateController.SelectedInteractableData;

        if (selectedInteractable == null)
        {
            Debug.LogWarning("No interactable selected.");
            return;
        }

        bool placed = interactablePlacementService.TryPlaceInteractable(
            selectedInteractable,
            tile,
            builderStateController.SelectedInteractableRotationY
        );

        if (!placed)
        {
            Debug.Log($"Failed to place interactable {selectedInteractable.displayName} at {tile.GridPosition}");
            return;
        }

        Debug.Log($"Placed interactable {selectedInteractable.displayName} at {tile.GridPosition}");
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
                ClearBrushHoverHighlight();

                previousHoveredTile = currentHoveredTile;
                currentHoveredTile = tile;

                if (currentHoveredTile != null)
                    ApplyBrushHoverHighlight(currentHoveredTile);
            }
        }
        else
        {
            ClearBrushHoverHighlight();

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
        
        if (interactablePlacementService != null)
        {
            interactablePlacementService.RemoveInteractableAtTile(tile);
        }
        
        tile.terrainType = TerrainType.Ground;
        tile.ApplyTerrainSettings();

        Debug.Log($"Erased tile {tile.GridPosition} to Ground");
    }
    private void OnEraseClick(InputAction.CallbackContext context)
    {
        if (isPickingObjectiveTile || isPickingEnemyPatrolEndTile)
            return;

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

        if (TryRemovePatrolEndMarkerAtTile(tile))
            return;
        
        if (interactablePlacementService != null)
        {
            bool removedInteractable = interactablePlacementService.RemoveInteractableAtTile(tile);

            if (removedInteractable)
            {
                Debug.Log($"Removed interactable at {tile.GridPosition}");
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

        if (unitPlacementService == null)
        {
            Debug.LogError("BuilderInputController: UnitPlacementService is missing.");
            return;
        }

        UnitData selectedUnitData = builderStateController.SelectedUnitData;

        if (selectedUnitData == null)
        {
            Debug.LogWarning("BuilderInputController: No unit data selected.");
            return;
        }

        bool placed = unitPlacementService.TryPlaceUnit(
            selectedUnitData,
            tile,
            builderStateController.SelectedUnitRotationY,
            builderStateController.SelectedUnitPaintTeam,
            builderStateController.SelectedUnitUsesCardinalFacing,
            out PlacedBuilderUnit placedUnit
        );

        if (!placed)
        {
            Debug.Log($"Failed to place unit {selectedUnitData.unitName} at {tile.GridPosition}");
            return;
        }

        ConfigurePlacedUnitAI(placedUnit, tile);

        Debug.Log($"Placed unit {selectedUnitData.unitName} at {tile.GridPosition}");
    }

    private void ConfigurePlacedUnitAI(PlacedBuilderUnit placedUnit, GridTile startTile)
    {
        if (placedUnit == null || builderStateController == null || startTile == null)
            return;

        if (builderStateController.SelectedUnitPaintTeam != BuilderUnitPaintTeam.Enemy)
        {
            placedUnit.EnemyBehavior = EnemyAIBehavior.Static;
            placedUnit.HasPatrolRoute = false;
            return;
        }

        placedUnit.EnemyBehavior = builderStateController.SelectedEnemyBehavior;
        placedUnit.PatrolStart = startTile.GridPosition;

        if (placedUnit.EnemyBehavior == EnemyAIBehavior.Patrol)
            BeginEnemyPatrolEndSelection(placedUnit, startTile);
    }

    private void BeginEnemyPatrolEndSelection(PlacedBuilderUnit placedUnit, GridTile startTile)
    {
        pendingEnemyPatrolUnit = placedUnit;
        selectedEnemyPatrolStartTile = startTile;
        isPickingEnemyPatrolEndTile = true;

        ClearBrushHoverHighlight();
        RefreshEnemyPatrolSelectionVisuals();

        Debug.Log($"Patrol start selected: {startTile.GridPosition}. Select the patrol end tile.");
    }

    private void CompleteEnemyPatrolEndSelection(GridTile endTile)
    {
        if (pendingEnemyPatrolUnit == null || selectedEnemyPatrolStartTile == null || endTile == null)
            return;

        if (!endTile.isWalkable)
        {
            Debug.Log("Patrol end tile must be walkable.");
            return;
        }

        if (endTile == selectedEnemyPatrolStartTile)
        {
            Debug.Log("Patrol end tile must be different from the patrol start tile.");
            return;
        }

        if (endTile.isOccupied && endTile.OccupyingUnit != null)
        {
            Debug.Log("Patrol end tile cannot be occupied.");
            return;
        }

        pendingEnemyPatrolUnit.EnemyBehavior = EnemyAIBehavior.Patrol;
        pendingEnemyPatrolUnit.HasPatrolRoute = true;
        pendingEnemyPatrolUnit.PatrolStart = selectedEnemyPatrolStartTile.GridPosition;
        pendingEnemyPatrolUnit.PatrolEnd = endTile.GridPosition;

        if (unitPlacementService != null)
            unitPlacementService.CreatePatrolEndMarker(pendingEnemyPatrolUnit, endTile);

        Debug.Log($"Patrol route set: {pendingEnemyPatrolUnit.PatrolStart} -> {pendingEnemyPatrolUnit.PatrolEnd}");

        isPickingEnemyPatrolEndTile = false;
        pendingEnemyPatrolUnit = null;
        selectedEnemyPatrolStartTile = null;

        ClearBrushHoverHighlight();
        if (endTile.OccupyingUnit == null)
        {
            endTile.SetHoverHighlight(patrolStartSelectedColor);
            currentBrushHoverTiles.Add(endTile);
        }
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
                if (unitPlacementService != null)
                    unitPlacementService.DestroyPatrolEndMarker(placedUnit);

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

    private bool TryRemovePatrolEndMarkerAtTile(GridTile tile)
    {
        if (tile == null || tile.OccupyingUnit == null || builderUnitRegistry == null)
            return false;

        IReadOnlyList<PlacedBuilderUnit> placedUnits = builderUnitRegistry.GetPlacedUnits();
        foreach (PlacedBuilderUnit placedUnit in placedUnits)
        {
            if (placedUnit == null || placedUnit.PatrolEndMarker != tile.OccupyingUnit)
                continue;

            if (unitPlacementService != null)
                unitPlacementService.DestroyPatrolEndMarker(placedUnit);
            else
                tile.SetOccupant(null);

            placedUnit.HasPatrolRoute = false;
            placedUnit.EnemyBehavior = EnemyAIBehavior.Static;

            Debug.Log($"Removed patrol end marker at {tile.GridPosition}.");
            return true;
        }

        return false;
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
        HashSet<PlacedInteractable> affectedInteractables = new HashSet<PlacedInteractable>();
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

            if (interactablePlacementService != null)
            {
                PlacedInteractable placedInteractable = interactablePlacementService.GetPlacedInteractableAtTile(tile);
                if (placedInteractable != null)
                {
                    affectedInteractables.Add(placedInteractable);
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

        foreach (PlacedInteractable placedInteractable in affectedInteractables)
        {
            if (interactablePlacementService != null)
                interactablePlacementService.SetInteractableElevation(placedInteractable, selectedElevation);
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

        if (unitPlacementService != null)
            unitPlacementService.RefreshPlacedUnitTransform(placedUnit);
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
                    builderStateController.SetSelectedObstacleRotationY(270);
                else if (rotateRight)
                    builderStateController.SetSelectedObstacleRotationY(90);
                else if (rotateUp)
                    builderStateController.SetSelectedObstacleRotationY(0);
                else if (rotateDown)
                    builderStateController.SetSelectedObstacleRotationY(180);
                break;

            case BuilderToolMode.InteractablePaint:
                if (rotateLeft)
                    builderStateController.SetSelectedInteractableRotationY(270);
                else if (rotateRight)
                    builderStateController.SetSelectedInteractableRotationY(90);
                else if (rotateUp)
                    builderStateController.SetSelectedInteractableRotationY(0);
                else if (rotateDown)
                    builderStateController.SetSelectedInteractableRotationY(180);
                break;

            case BuilderToolMode.UnitPaint:
                if (rotateLeft)
                    builderStateController.SetSelectedUnitRotationY(270);
                else if (rotateRight)
                    builderStateController.SetSelectedUnitRotationY(90);
                else if (rotateUp)
                    builderStateController.SetSelectedUnitRotationY(0);
                else if (rotateDown)
                    builderStateController.SetSelectedUnitRotationY(180);
                break;
        }
    }
    
    private void OnLeftClickStarted(InputAction.CallbackContext context)
    {
        if (IsUIBlockingSceneInteraction())
            return;

        if (isPickingEnemyPatrolEndTile)
        {
            if (currentHoveredTile != null)
                CompleteEnemyPatrolEndSelection(currentHoveredTile);

            return;
        }

        if (isPickingObjectiveTile)
        {
            if (currentHoveredTile != null)
            {
                selectedObjectiveTile = currentHoveredTile;
                RefreshObjectiveTileSelectionVisuals();
                Debug.Log($"Objective tile selected: {selectedObjectiveTile.GridPosition}");
            }

            return;
        }
    
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
            case BuilderToolMode.InteractablePaint:
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
        if (IsUIBlockingSceneInteraction())
            return;
        if (builderStateController == null || placementRotationAnchorTile == null)
            return;

        switch (builderStateController.CurrentToolMode)
        {
            case BuilderToolMode.ObstaclePaint:
                PlaceObstacle(placementRotationAnchorTile);
                break;

            case BuilderToolMode.InteractablePaint:
                PlaceInteractable(placementRotationAnchorTile);
                break;

            case BuilderToolMode.UnitPaint:
                PlaceUnit(placementRotationAnchorTile);
                break;
        }

        isDraggingPlacementRotation = false;
        placementRotationAnchorTile = null;
        placementRotationAnchorWorld = Vector3.zero;

        ClearBrushHoverHighlight();
        previousHoveredTile = null;
        currentHoveredTile = null;

        UpdateHoveredTile();
    }

    private void CancelEnemyPatrolEndSelection()
    {
        if (!isPickingEnemyPatrolEndTile)
            return;

        if (pendingEnemyPatrolUnit != null)
        {
            if (unitPlacementService != null)
                unitPlacementService.DestroyPatrolEndMarker(pendingEnemyPatrolUnit);

            pendingEnemyPatrolUnit.EnemyBehavior = EnemyAIBehavior.Static;
            pendingEnemyPatrolUnit.HasPatrolRoute = false;
        }

        isPickingEnemyPatrolEndTile = false;
        pendingEnemyPatrolUnit = null;
        selectedEnemyPatrolStartTile = null;
        ClearBrushHoverHighlight();
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
    
    private void ClearBrushHoverHighlight()
    {
        foreach (GridTile tile in currentBrushHoverTiles)
        {
            if (tile != null)
                tile.ResetHighlight();
        }

        currentBrushHoverTiles.Clear();
    }

    private void ApplyBrushHoverHighlight(GridTile centerTile)
    {
        ClearBrushHoverHighlight();

        if (centerTile == null)
            return;

        List<GridTile> hoverTiles = GetHoverTilesForCurrentTool(centerTile);

        foreach (GridTile tile in hoverTiles)
        {
            if (tile == null)
                continue;

            tile.SetHoverHighlight(hoverColor);
            currentBrushHoverTiles.Add(tile);
        }
    }
    
    private List<GridTile> GetHoverTilesForCurrentTool(GridTile centerTile)
    {
        if (centerTile == null || builderStateController == null)
            return new List<GridTile>();

        switch (builderStateController.CurrentToolMode)
        {
            case BuilderToolMode.ObstaclePaint:
            {
                ObstacleData obstacleData = builderStateController.SelectedObstacleData;
                if (obstacleData == null)
                    return new List<GridTile>();

                return GetTilesFromFootprint(
                    centerTile,
                    obstacleData.FootprintSize,
                    builderStateController.SelectedObstacleRotationY
                );
            }

            case BuilderToolMode.InteractablePaint:
            {
                InteractableData interactableData = builderStateController.SelectedInteractableData;
                if (interactableData == null)
                    return new List<GridTile>();

                return GetTilesFromFootprint(
                    centerTile,
                    interactableData.footprint,
                    builderStateController.SelectedInteractableRotationY
                );
            }

            case BuilderToolMode.UnitPaint:
            {
                UnitData unitData = builderStateController.SelectedUnitData;
                if (unitData == null)
                    return new List<GridTile>();

                return GetTilesFromFootprint(
                    centerTile,
                    unitData.footprintSize,
                    builderStateController.SelectedUnitRotationY
                );
            }

            case BuilderToolMode.TerrainPaint:
            case BuilderToolMode.ElevationPaint:
            case BuilderToolMode.Erase:
            default:
                return GetTilesInBrush(centerTile);
        }
    }

    private List<GridTile> GetTilesFromFootprint(GridTile originTile, Vector2Int footprintSize, int rotationY)
    {
        List<GridTile> result = new List<GridTile>();

        if (originTile == null || gridManager == null)
            return result;

        List<Vector2Int> offsets = GetRotatedFootprintOffsets(footprintSize, rotationY);

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int tilePos = new Vector2Int(originTile.X + offset.x, originTile.Y + offset.y);

            if (!gridManager.isInsideGrid(tilePos))
                continue;

            GridTile tile = gridManager.GetTileAt(tilePos);
            if (tile != null)
                result.Add(tile);
        }

        return result;
    }

    private List<Vector2Int> GetRotatedFootprintOffsets(Vector2Int footprintSize, int rotationY)
    {
        List<Vector2Int> offsets = new List<Vector2Int>();

        int width = Mathf.Max(1, footprintSize.x);
        int height = Mathf.Max(1, footprintSize.y);
        int normalizedRotation = NormalizeRotation(rotationY);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
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
