using UnityEngine;
using UnityEngine.InputSystem;

public class BuilderInputController : MonoBehaviour
{
    [Header("Hover")]
    [SerializeField] private Color hoverColor = Color.magenta;

    private GridTile previousHoveredTile;
    
    [Header("References")]
    [SerializeField] private BuilderStateController builderStateController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private Transform unitParent;

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
        inputActions.Gameplay.Click.performed += OnLeftClick;
        inputActions.Gameplay.EraseClick.performed += OnEraseClick;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.PointerPosition.performed -= OnPointerMove;
        inputActions.Gameplay.Click.performed -= OnLeftClick;
        inputActions.Gameplay.EraseClick.performed -= OnEraseClick;
        inputActions.Disable();
    }

    private void Update()
    {
        UpdateHoveredTile();
    }

    private void OnPointerMove(InputAction.CallbackContext context)
    {
        pointerPosition = context.ReadValue<Vector2>();
    }

    private void OnLeftClick(InputAction.CallbackContext context)
    {
        if (builderStateController == null)
        {
            Debug.LogError("BuilderInputController: BuilderStateController is missing.");
            return;
        }

        if (currentHoveredTile == null)
            return;

        switch (builderStateController.CurrentToolMode)
        {
            case BuilderToolMode.TerrainPaint:
                PaintTerrain(currentHoveredTile);
                break;

            case BuilderToolMode.ObstaclePaint:
                PlaceObstacle(currentHoveredTile);
                break;
            
            case BuilderToolMode.UnitPaint:
                PlaceUnit(currentHoveredTile);
                break;

            case BuilderToolMode.ElevationPaint:
                Debug.Log("ElevationPaint mode selected. Hook not implemented yet.");
                break;
        }
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
            tile.GridPosition   
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

    private void PaintTerrain(GridTile tile)
    {
        if (tile == null)
            return;

        tile.terrainType = builderStateController.SelectedTerrainType;
        tile.ApplyTerrainSettings();

        Debug.Log($"Painted tile ({tile.X}, {tile.Y}) with terrain {tile.terrainType}");
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

        GameObject spawnedObject = Instantiate(selectedUnitData.unitPrefab, Vector3.zero, Quaternion.identity, unitParent);
        GridUnit gridUnit = spawnedObject.GetComponent<GridUnit>();

        if (gridUnit == null)
        {
            Debug.LogError($"BuilderInputController: Spawned prefab {selectedUnitData.unitPrefab.name} has no GridUnit component.");
            Destroy(spawnedObject);
            return;
        }

        gridUnit.InitializeFromData(selectedUnitData);
        gridUnit.PlaceOnTile(tile);

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

        if (tile != null && tile.OccupyingUnit == unit.gameObject)
            tile.SetOccupant(null);

        Destroy(unit.gameObject);

        Debug.Log($"Removed unit from tile {tile?.GridPosition}");
    }
}