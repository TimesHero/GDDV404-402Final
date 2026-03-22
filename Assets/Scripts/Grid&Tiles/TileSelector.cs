using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class TileSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private AStarPathFinder pathFinder;
    [SerializeField] private GridRangeFinder rangeFinder;
    
    [Header("Hover Colors")]
    [SerializeField] public Color hoverColor = Color.darkMagenta;
    
    
    private InputSystem_Actions inputActions;
    
    private Vector2 pointerPosition;
    private GridTile currentHoveredTile;
    private GridTile previousHoveredTile;
    
    private GridUnit selectedUnit;
    
    private GridTile selectedStartTile;
    private GridTile selectedTargetTile;
    
    private List<GridTile> currentPath = new List<GridTile>();
    private Dictionary<GridTile, int> reachableTiles = new Dictionary<GridTile, int>();
    
    private List<GridTile> previewPath = new List<GridTile>();
    private GridTile lastPreviewTarget;
    
    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }
    
    private void OnEnable()
    {
       inputActions.Enable();
       inputActions.Gameplay.PointerPosition.performed += OnPointerMove;
       inputActions.Gameplay.Click.performed += OnClick;
    }
    
    private void OnDisable()
    {
       inputActions.Gameplay.PointerPosition.performed -= OnPointerMove;
       inputActions.Gameplay.Click.performed -= OnClick;
       inputActions.Disable();
    }
    
    private void Update()
    {
        HandleTileHover();
    }
    private void OnPointerMove(InputAction.CallbackContext context)
    {
        pointerPosition = context.ReadValue<Vector2>();
    }
    
    private void OnClick(InputAction.CallbackContext context)
    {
         if (currentHoveredTile == null)
            return;

        if (pathFinder == null)
        {
            Debug.LogError("TileSelector: PathFinder reference is missing.");
            return;
        }

        if (rangeFinder == null)
        {
            Debug.LogError("TileSelector: RangeFinder reference is missing.");
            return;
        }

        if (unitSpawner == null || unitSpawner.SpawnedUnit == null)
        {
            Debug.LogError("TileSelector: UnitSpawner or SpawnedUnit reference is missing.");
            return;
        }

        GridUnit activeUnit = unitSpawner.SpawnedUnit;

        if (activeUnit.IsMoving)
            return;
        
        if (selectedUnit == null)
        {
            if (currentHoveredTile != activeUnit.CurrentTile)
                return;

            SelectUnit(activeUnit);
            return;
        }

        if (currentHoveredTile == selectedUnit.CurrentTile)
        {
            DeselectUnit();
            return;
        }

        selectedStartTile = selectedUnit.CurrentTile;
        selectedTargetTile = currentHoveredTile;

        if (!IsTileReachable(selectedTargetTile))
        {
            Debug.Log("Tile is outside movement range.");
            return;
        }

        List<GridTile> path = pathFinder.FindPath(selectedStartTile, selectedTargetTile);

        if (path == null)
        {
            Debug.Log("No path found.");
            return;
        }

        ClearPathPreview();

        currentPath = new List<GridTile>(path);

        selectedStartTile.ShowAsStart();
        selectedTargetTile.ShowAsTarget();

        foreach (GridTile tile in currentPath)
        {
            if (tile == selectedStartTile || tile == selectedTargetTile)
                continue;

            tile.ShowAsPath();
        }

        selectedUnit.MoveAlongPath(new List<GridTile>(path));

        DeselectUnit();
    }
    
    private void SelectUnit(GridUnit unit)
    {
        if (unit == null)
            return;

        ClearPathPreview();
        ClearPreviewPath();
        ClearReachableTiles();

        selectedUnit = unit;
        selectedStartTile = unit.CurrentTile;

        if (selectedStartTile != null)
            selectedStartTile.ShowAsStart();

        ShowMovementRange(unit);

        Debug.Log("Unit selected.");
    }

    private void DeselectUnit()
    {
        selectedUnit = null;
        selectedStartTile = null;
        selectedTargetTile = null;
        
        ClearPreviewPath();
        ClearPathPreview();
        ClearReachableTiles();
        
        Debug.Log("Unit deselected.");
    }

    private void HandleTileHover()
    {
        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();

            if (tile != currentHoveredTile)
            {
                if (currentHoveredTile != null)
                    RestoreTileVisualState(currentHoveredTile);

                previousHoveredTile = currentHoveredTile;
                currentHoveredTile = tile;
                
                if (selectedUnit != null)
                {
                    if (IsTileReachable(currentHoveredTile))
                        ShowPreviewPath(selectedUnit.CurrentTile, currentHoveredTile);
                    else
                        ClearPreviewPath();
                }
                
                if (!IsTileInPersistentState(currentHoveredTile))
                    currentHoveredTile.SetHoverHighlight(hoverColor);
            }
        }
        else
        {
            if (currentHoveredTile != null)
                RestoreTileVisualState(currentHoveredTile);
            
            currentHoveredTile = null;
            previousHoveredTile = null;
            
            ClearPreviewPath();
        }
    }
    private void ClearPathPreview()
    {
        foreach (GridTile tile in currentPath)
        {
            if (tile != null)
                RestoreTileVisualState(tile);
        }

        currentPath.Clear();
    }

    private bool IsTileInPersistentState(GridTile tile)
    {
        if (tile == null)
            return false;
        if (selectedUnit != null && tile == selectedUnit.CurrentTile)
            return true;
        if (tile == selectedStartTile)
            return true;
        if (tile == selectedTargetTile)
            return true;
        if (reachableTiles.ContainsKey(tile))
            return true;
        if (previewPath.Contains(tile))
            return true;
        
        return currentPath.Contains(tile);
    }
    
    private void ShowMovementRange(GridUnit unit)
    {
        if (rangeFinder == null || unit == null || unit.CurrentTile == null)
        {
            Debug.LogError("TileSelector: RangeFinder, unit, or unit current tile is missing.");
            return;
        }

        ClearReachableTiles();

        reachableTiles = rangeFinder.GetReachableTiles(unit.CurrentTile, unit.MaxMovementPoints);

        foreach (KeyValuePair<GridTile, int> pair in reachableTiles)
        {
            GridTile tile = pair.Key;

            if (tile == null)
                continue;

            if (tile == unit.CurrentTile)
                continue;

            tile.ShowAsReachable();
        }
    }
    
    private void ClearReachableTiles()
    {
        foreach (KeyValuePair<GridTile, int> pair in reachableTiles)
        {
            if (pair.Key != null)
                pair.Key.ResetHighlight();
        }

        reachableTiles.Clear();
    }
    
    private bool IsTileReachable(GridTile tile)
    {
        if (tile == null)
            return false;

        return reachableTiles.ContainsKey(tile);
    }
    
    private void ShowPreviewPath(GridTile start, GridTile target)
    {
        if (pathFinder == null)
            return;

        if (target == null || start == null)
            return;
        
        if (lastPreviewTarget == target)
            return;

        ClearPreviewPath();

        List<GridTile> path = pathFinder.FindPath(start, target);

        if (path == null)
            return;

        previewPath = new List<GridTile>(path);
        lastPreviewTarget = target;
        
        start.ShowAsStart();
        target.ShowAsTarget();

        foreach (GridTile tile in previewPath)
        {
            if (tile == start || tile == target)
                continue;

            tile.ShowAsPreview();
        }
    }

    private void ClearPreviewPath()
    {
        List<GridTile> oldPreviewTiles = new List<GridTile>(previewPath);
        
        previewPath.Clear();
        lastPreviewTarget = null;
        
        foreach (GridTile tile in oldPreviewTiles)
        {
            if (tile != null)
                RestoreTileVisualState(tile);
        }    
    }
    
    private void RestoreTileVisualState(GridTile tile)
    {
        if (tile == null)
            return;
        
        if (selectedUnit != null && tile == selectedUnit.CurrentTile)
        {
            tile.ShowAsStart();
            return;
        }

        if (tile == selectedStartTile)
        {
            tile.ShowAsStart();
            return;
        }

        if (tile == selectedTargetTile)
        {
            tile.ShowAsTarget();
            return;
        }

        if (currentPath.Contains(tile))
        {
            tile.ShowAsPath();
            return;
        }

        if (previewPath.Contains(tile))
        {
            tile.ShowAsPreview();
            return;
        }

        if (reachableTiles.ContainsKey(tile))
        {
            tile.ShowAsReachable();
            return;
        }

        tile.ResetHighlight();
    }
}
