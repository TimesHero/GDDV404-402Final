using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class TileSelector : MonoBehaviour
{
    [Header("Highlight Colors")]
    [SerializeField] private Color reachableColor = new Color(0f, 1f, 1f, 0.35f);
    [SerializeField] private Color previewPathColor = new Color(0f, 0.5f, 1f, 0.45f);
    [SerializeField] private Color finalPathColor = new Color(0f, 0f, 1f, 0.55f);
    [SerializeField] private Color startColor = new Color(0f, 1f, 0f, 0.45f);
    [SerializeField] private Color targetColor = new Color(1f, 0f, 0f, 0.45f);
    
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
    
    private GridUnit GetUnitOnTile(GridTile tile)
    {
        if (tile == null || !tile.isOccupied || tile.OccupyingUnit == null)
            return null;

        return tile.OccupyingUnit.GetComponent<GridUnit>();
    }

    private bool IsPlayerUnit(GridUnit unit)
    {
        return unit != null && unit.Team == UnitTeam.Player;
    }

    private bool IsEnemyUnit(GridUnit unit)
    {
        return unit != null && unit.Team == UnitTeam.Enemy;
    }

    private bool HasActionsRemaining(GridUnit unit)
    {
        if (unit == null)
            return false;

        return unit.CanMoveThisTurn() || unit.CanAttackThisTurn();
    }

    private void TrySelectUnit(GridUnit unit)
    {
        if (unit == null)
            return;

        if (!IsPlayerUnit(unit))
            return;

        if (!HasActionsRemaining(unit))
        {
            Debug.Log($"{unit.name} has no actions left this turn.");
            return;
        }

        SelectUnit(unit);
    }
    
    private void RestoreSelectionVisuals()
    {
        ClearPreviewPath();
        ClearPathPreview();
        selectedTargetTile = null;

        if (selectedUnit != null && selectedUnit.CurrentTile != null)
        {
            selectedStartTile = selectedUnit.CurrentTile;
            selectedStartTile.ShowOverlayColor(startColor);
            ShowMovementRange(selectedUnit);
        }
    }
    
    private void OnClick(InputAction.CallbackContext context)
    {
        if (BattleStateManager.Instance != null && BattleStateManager.Instance.BattleEnded)
            return;
        
        if (currentHoveredTile == null)
            return;

        if (TurnManager.Instance == null)
        {
            Debug.LogError("TileSelector: TurnManager instance is missing.");
            return;
        }

        if (!TurnManager.Instance.IsPlayerTurn() || TurnManager.Instance.IsBusy())
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

        GridUnit clickedUnit = GetUnitOnTile(currentHoveredTile);

        if (selectedUnit == null)
        {
            if (IsPlayerUnit(clickedUnit))
                TrySelectUnit(clickedUnit);

            return;
        }

        if (IsPlayerUnit(clickedUnit))
        {
            if (clickedUnit == selectedUnit)
            {
                DeselectUnit();
                return;
            }

            TrySelectUnit(clickedUnit);
            return;
        }

        if (!HasActionsRemaining(selectedUnit))
        {
            Debug.Log("This unit has no actions left this turn.");
            DeselectUnit();
            return;
        }

        if (IsEnemyUnit(clickedUnit))
        {
            if (!selectedUnit.TryAttack(clickedUnit))
            {
                Debug.Log("Attack failed.");
                return;
            }

            if (selectedUnit.CanMoveThisTurn())
                RestoreSelectionVisuals();
            else
                DeselectUnit();
            
            CheckIfAllPlayerUnitsAreDone();

            return;
        }

        if (!selectedUnit.CanMoveThisTurn())
        {
            Debug.Log("This unit cannot move anymore this turn.");
            return;
        }

        selectedStartTile = selectedUnit.CurrentTile;
        selectedTargetTile = currentHoveredTile;

        if (!IsTileReachable(selectedTargetTile))
        {
            Debug.Log("Tile is outside movement range.");
            RestoreSelectionVisuals();
            return;
        }

        List<GridTile> path = pathFinder.FindPath(selectedStartTile, selectedTargetTile);

        if (path == null)
        {
            Debug.Log("No path found.");
            RestoreSelectionVisuals();
            return;
        }

        ClearPreviewPath();
        ClearPathPreview();

        currentPath = new List<GridTile>(path);

        selectedStartTile.ShowOverlayColor(startColor);
        selectedTargetTile.ShowOverlayColor(targetColor);

        foreach (GridTile tile in currentPath)
        {
            if (tile == selectedStartTile || tile == selectedTargetTile)
                continue;

            tile.ShowOverlayColor(finalPathColor);
        }

        if (!selectedUnit.TryMove(new List<GridTile>(path)))
        {
            Debug.Log("Move failed.");
            RestoreSelectionVisuals();
            return;
        }

        DeselectUnit();
        CheckIfAllPlayerUnitsAreDone();
    }
    
    private void CheckIfAllPlayerUnitsAreDone()
    {
        if (TurnManager.Instance == null)
            return;

        TurnManager.Instance.HandlePlayerUnitsDoneState();
    }
    
    private void SelectUnit(GridUnit unit)
    {
        if (unit == null)
            return;
        
        if (TurnManager.Instance != null)
            TurnManager.Instance.ClearPlayerHint();

        ClearPathPreview();
        ClearPreviewPath();
        ClearReachableTiles();

        selectedUnit = unit;
        selectedStartTile = unit.CurrentTile;

        if (selectedStartTile != null)
            selectedStartTile.ShowOverlayColor(startColor);

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

            tile.ShowOverlayColor(reachableColor);
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
        
        start.ShowOverlayColor(startColor);
        target.ShowOverlayColor(targetColor);

        foreach (GridTile tile in previewPath)
        {
            if (tile == start || tile == target)
                continue;

            tile.ShowOverlayColor(previewPathColor);
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
            tile.ShowOverlayColor(startColor);
            return;
        }

        if (tile == selectedStartTile)
        {
            tile.ShowOverlayColor(startColor);
            return;
        }

        if (tile == selectedTargetTile)
        {
            tile.ShowOverlayColor(targetColor);
            return;
        }

        if (currentPath.Contains(tile))
        {
            tile.ShowOverlayColor(finalPathColor);
            return;
        }

        if (previewPath.Contains(tile))
        {
            tile.ShowOverlayColor(previewPathColor);
            return;
        }

        if (reachableTiles.ContainsKey(tile))
        {
            tile.ShowOverlayColor(reachableColor);
            return;
        }

        tile.ResetHighlight();
    }
    
    private void HandlePostActionSelection(GridUnit unit)
    {
        if (unit == null)
        {
            DeselectUnit();
            return;
        }

        bool canStillMove = unit.CanMoveThisTurn();
        bool canStillAttack = unit.CanAttackThisTurn();

        if (!canStillMove && !canStillAttack)
        {
            DeselectUnit();
            return;
        }

        if (unit.TurnRules != null && unit.TurnRules.AutoDeselectWhenOutOfActions)
        {
            RestoreSelectionVisuals();
        }
        else
        {
            RestoreSelectionVisuals();
        }
    }
    public void ForceClearSelectionAndHighlights()
    {
        if (currentHoveredTile != null)
            currentHoveredTile.ResetHighlight();

        currentHoveredTile = null;
        previousHoveredTile = null;

        selectedUnit = null;
        selectedStartTile = null;
        selectedTargetTile = null;

        ClearPreviewPath();
        ClearPathPreview();
        ClearReachableTiles();
    }
}
