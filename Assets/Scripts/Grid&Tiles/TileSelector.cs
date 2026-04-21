using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TileSelector : MonoBehaviour
{
    private enum PlayerActionMode
    {
        None,
        Move,
        Attack,
        Push
    }

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
    [SerializeField] private InteractablePlacementService interactablePlacementService;
    [SerializeField] private UnitActionMenuController actionMenu;
    [SerializeField] private GridManager gridManager;
    
    [Header("Hover Colors")]
    [SerializeField] public Color hoverColor = Color.darkMagenta;

    [Header("Debug")]
    [SerializeField] private bool logActionMenuPointerDebug = true;
    [SerializeField] private int pointerDebugMaxResults = 12;
    
    
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
    private PlayerActionMode pendingActionMode = PlayerActionMode.None;
    
    private void Awake()
    {
        inputActions = new InputSystem_Actions();

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }
    
    private void OnEnable()
    {
       inputActions.Enable();
       inputActions.Gameplay.PointerPosition.performed += OnPointerMove;
       inputActions.Gameplay.Click.performed += OnClick;
    }
    
    private void OnDisable()
    {
       if (actionMenu != null)
           actionMenu.Hide();

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
    
    private BarrelInteractable GetBarrelOnTile(GridTile tile)
    {
        if (tile == null || interactablePlacementService == null)
            return null;

        PlacedInteractable placedInteractable = interactablePlacementService.GetPlacedInteractableAtTile(tile);
        if (placedInteractable == null)
            return null;

        return placedInteractable.GetComponent<BarrelInteractable>();
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

        if (actionMenu != null && actionMenu.IsVisible)
        {
            List<RaycastResult> uiResults = GetUiRaycastResults();
            bool pointerOverActionMenu = IsPointerOverActionMenu(uiResults);

            if (logActionMenuPointerDebug)
                LogActionMenuPointerDebug(uiResults, pointerOverActionMenu);

            if (pointerOverActionMenu)
            {
                actionMenu.TryHandlePointerClick(pointerPosition, uiResults);
                return;
            }

            DeselectUnit();
            return;
        }
        
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
        BarrelInteractable clickedBarrel = GetBarrelOnTile(currentHoveredTile);

        if (selectedUnit == null)
        {
            if (IsPlayerUnit(clickedUnit))
            {
                TrySelectUnit(clickedUnit);
                ShowUnitActionMenu();
            }

            return;
        }

        if (IsPlayerUnit(clickedUnit))
        {
            if (clickedUnit == selectedUnit)
            {
                ShowUnitActionMenu();
                return;
            }

            TrySelectUnit(clickedUnit);
            ShowUnitActionMenu();
            return;
        }

        if (pendingActionMode == PlayerActionMode.Move)
        {
            if (clickedBarrel != null)
            {
                ExecuteBarrelInteraction(clickedBarrel);
                pendingActionMode = PlayerActionMode.None;
                return;
            }

            ExecuteMoveToTile(currentHoveredTile);
            pendingActionMode = PlayerActionMode.None;
            return;
        }

        if (pendingActionMode == PlayerActionMode.Attack)
        {
            if (!IsEnemyUnit(clickedUnit))
            {
                Debug.Log("Choose an enemy target to attack.");
                return;
            }

            ExecuteAttack(clickedUnit);
            pendingActionMode = PlayerActionMode.None;
            return;
        }

        if (pendingActionMode == PlayerActionMode.Push)
        {
            if (!IsEnemyUnit(clickedUnit))
            {
                Debug.Log("Choose an enemy target to push.");
                return;
            }

            ExecutePush(clickedUnit);
            return;
        }

        Debug.Log("Choose an action from the unit menu first.");
    }
    
    private void CheckIfAllPlayerUnitsAreDone()
    {
        if (TurnManager.Instance == null)
            return;

        TurnManager.Instance.HandlePlayerUnitsDoneState();
    }

    private bool IsPointerOverActionMenu()
    {
        if (actionMenu == null || !actionMenu.IsVisible || EventSystem.current == null)
            return false;

        return IsPointerOverActionMenu(GetUiRaycastResults());
    }

    private bool IsPointerOverActionMenu(List<RaycastResult> results)
    {
        if (actionMenu == null || !actionMenu.IsVisible || results == null)
            return false;

        for (int i = 0; i < results.Count; i++)
        {
            if (actionMenu.Contains(results[i].gameObject))
                return true;
        }

        return false;
    }

    private List<RaycastResult> GetUiRaycastResults()
    {
        List<RaycastResult> results = new List<RaycastResult>();

        if (EventSystem.current == null)
            return results;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition
        };

        EventSystem.current.RaycastAll(eventData, results);
        return results;
    }

    private void LogActionMenuPointerDebug(List<RaycastResult> uiResults, bool pointerOverActionMenu)
    {
        string log = $"[ActionMenu Click Debug] screen={pointerPosition}, overActionMenu={pointerOverActionMenu}\n";
        log += $"UI hits ({uiResults.Count}):\n";

        int uiCount = Mathf.Min(uiResults.Count, pointerDebugMaxResults);
        for (int i = 0; i < uiCount; i++)
        {
            RaycastResult result = uiResults[i];
            log += $"  {i}: {GetHierarchyPath(result.gameObject)} | layer={LayerMask.LayerToName(result.gameObject.layer)} | module={result.module}\n";
        }

        if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(pointerPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            log += $"Physics hits ({hits.Length}):\n";

            int physicsCount = Mathf.Min(hits.Length, pointerDebugMaxResults);
            for (int i = 0; i < physicsCount; i++)
            {
                GameObject hitObject = hits[i].collider != null ? hits[i].collider.gameObject : null;
                string layerName = hitObject != null ? LayerMask.LayerToName(hitObject.layer) : "<none>";
                log += $"  {i}: {GetHierarchyPath(hitObject)} | layer={layerName} | dist={hits[i].distance:0.00}\n";
            }
        }
        else
        {
            log += "Physics hits skipped: mainCamera is missing.\n";
        }

        Debug.Log(log);
    }

    private string GetHierarchyPath(GameObject targetObject)
    {
        if (targetObject == null)
            return "<null>";

        string path = targetObject.name;
        Transform current = targetObject.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private void ShowUnitActionMenu()
    {
        if (selectedUnit == null)
            return;

        if (actionMenu == null)
        {
            Debug.LogError("TileSelector: Action Menu reference is missing. Assign a UnitActionMenuController in the inspector.");
            return;
        }

        pendingActionMode = PlayerActionMode.None;

        HiddenStateComponent hiddenState = selectedUnit.GetComponent<HiddenStateComponent>();
        BarrelInteractable currentBarrel = hiddenState != null ? hiddenState.CurrentBarrel : null;
        bool hasBarrel = currentBarrel != null;

        actionMenu.ShowForUnit(
            selectedUnit.transform.position,
            selectedUnit.CanMoveThisTurn(),
            selectedUnit.CanAttackThisTurn(),
            selectedUnit.CanAttackThisTurn() && selectedUnit.CanPushAbility,
            hasBarrel,
            () => SetPendingActionMode(PlayerActionMode.Move),
            () => SetPendingActionMode(PlayerActionMode.Attack),
            () => SetPendingActionMode(PlayerActionMode.Push),
            () => ExecuteRemoveBarrel(currentBarrel),
            DeselectUnit
        );
    }

    private void SetPendingActionMode(PlayerActionMode actionMode)
    {
        pendingActionMode = actionMode;

        if (actionMode == PlayerActionMode.Move)
            Debug.Log("Move selected. Choose a tile or barrel.");
        else if (actionMode == PlayerActionMode.Attack)
            Debug.Log("Attack selected. Choose an enemy.");
        else if (actionMode == PlayerActionMode.Push)
            Debug.Log("Push selected. Choose a push target.");
    }

    private bool CanInteractWithBarrel(GridUnit unit, BarrelInteractable barrel)
    {
        if (unit == null || barrel == null)
            return false;

        GridTile barrelTile = barrel.GetBarrelTilePublic();
        if (barrelTile == null)
            return false;

        if (unit.CurrentTile == barrelTile || barrel.HiddenUnit == unit)
            return true;

        return unit.CanMoveThisTurn() && barrel.CanUnitHideHere(unit) && IsTileReachable(barrelTile);
    }

    private void ExecuteAttack(GridUnit target)
    {
        if (selectedUnit == null || target == null)
            return;

        if (!selectedUnit.TryAttack(target))
        {
            Debug.Log("Attack failed.");
            return;
        }

        if (selectedUnit.CanMoveThisTurn())
            RestoreSelectionVisuals();
        else
            DeselectUnit();

        CheckIfAllPlayerUnitsAreDone();
    }

    private void ExecutePush(GridUnit target)
    {
        if (selectedUnit == null || target == null)
            return;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (!selectedUnit.CanPush(target, gridManager))
        {
            Debug.Log("Push failed.");
            return;
        }

        void OnPushFinished(GridUnit pushedUnit)
        {
            pushedUnit.OnMovementFinished -= OnPushFinished;
            CheckIfAllPlayerUnitsAreDone();
        }

        target.OnMovementFinished += OnPushFinished;

        if (!selectedUnit.TryPush(target, gridManager))
        {
            target.OnMovementFinished -= OnPushFinished;
            Debug.Log("Push failed.");
            return;
        }

        pendingActionMode = PlayerActionMode.None;
        DeselectUnit();
    }

    private void ExecuteBarrelInteraction(BarrelInteractable barrel)
    {
        if (selectedUnit == null || barrel == null)
            return;

        bool interacted = TryHandleBarrelInteraction(selectedUnit, barrel);

        if (!interacted)
        {
            Debug.Log("Barrel interaction failed.");
            return;
        }

        DeselectUnit();
        CheckIfAllPlayerUnitsAreDone();
    }

    private void ExecuteExitBarrel(BarrelInteractable barrel)
    {
        if (selectedUnit == null || barrel == null)
            return;

        if (!barrel.TryInteract(selectedUnit))
        {
            Debug.Log("Exit barrel failed.");
            return;
        }

        RestoreSelectionVisuals();
        CheckIfAllPlayerUnitsAreDone();
    }

    private void ExecuteRemoveBarrel(BarrelInteractable barrel)
    {
        if (selectedUnit == null || barrel == null)
            return;

        if (!barrel.RemoveByPlayer(selectedUnit))
        {
            Debug.Log("Remove barrel failed.");
            return;
        }

        RestoreSelectionVisuals();
        CheckIfAllPlayerUnitsAreDone();
    }

    private void ExecuteMoveToTile(GridTile targetTile)
    {
        if (selectedUnit == null || targetTile == null)
            return;

        if (!selectedUnit.CanMoveThisTurn())
        {
            Debug.Log("This unit cannot move anymore this turn.");
            return;
        }

        selectedStartTile = selectedUnit.CurrentTile;
        selectedTargetTile = targetTile;

        if (!IsTileReachable(selectedTargetTile))
        {
            Debug.Log("Tile is outside movement range.");
            RestoreSelectionVisuals();
            return;
        }

        List<GridTile> path = pathFinder.FindPath(selectedStartTile, selectedTargetTile, selectedUnit);

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

    private Vector3 GetTileMenuPosition(GridTile tile)
    {
        if (tile == null)
            return Vector3.zero;

        Renderer topRenderer = tile.GetTopRenderer();
        if (topRenderer != null)
        {
            return new Vector3(
                topRenderer.bounds.center.x,
                topRenderer.bounds.max.y,
                topRenderer.bounds.center.z
            );
        }

        return tile.transform.position;
    }

    private bool TryHandleBarrelInteraction(GridUnit unit, BarrelInteractable barrel)
    {
        if (unit == null || barrel == null)
            return false;

        GridTile barrelTile = barrel.GetBarrelTilePublic();
        if (barrelTile == null)
            return false;

        if (unit.CurrentTile == barrelTile || barrel.HiddenUnit == unit)
            return barrel.TryInteract(unit);

        if (!barrel.CanUnitHideHere(unit))
            return false;

        if (!unit.CanMoveThisTurn())
            return false;

        if (!IsTileReachable(barrelTile))
            return false;

        List<GridTile> path = pathFinder.FindPath(unit.CurrentTile, barrelTile, unit);
        if (path == null || path.Count <= 1)
            return false;

        barrel.PrepareForUnitEntering();

        void OnFinished(GridUnit movedUnit)
        {
            movedUnit.OnMovementFinished -= OnFinished;

            bool wasSeenEntering =
                EnemyVisionDetector.CanAnyEnemySeeUnit(movedUnit) ||
                EnemyVisionDetector.CanAnyEnemySeeBarrel(barrel);

            bool hiddenSuccessfully = barrel.CompleteHideAfterMove(movedUnit, wasSeenEntering);
            if (!hiddenSuccessfully)
                Debug.LogWarning("Barrel hide completion failed after movement.");
        }

        unit.OnMovementFinished += OnFinished;

        if (!unit.TryMove(new List<GridTile>(path)))
        {
            unit.OnMovementFinished -= OnFinished;
            return false;
        }

        return true;
    }
    
    private void SelectUnit(GridUnit unit)
    {
        if (unit == null)
            return;

        pendingActionMode = PlayerActionMode.None;

        if (actionMenu != null)
            actionMenu.Hide();
        
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
        pendingActionMode = PlayerActionMode.None;

        if (actionMenu != null)
            actionMenu.Hide();

        selectedUnit = null;
        selectedStartTile = null;
        selectedTargetTile = null;
        pendingActionMode = PlayerActionMode.None;

        ClearPreviewPath();
        ClearPathPreview();
        ClearReachableTiles();
        
        Debug.Log("Unit deselected.");
    }

    private void HandleTileHover()
    {
        if (actionMenu != null && actionMenu.IsVisible)
        {
            if (currentHoveredTile != null)
                RestoreTileVisualState(currentHoveredTile);

            currentHoveredTile = null;
            previousHoveredTile = null;
            ClearPreviewPath();
            return;
        }

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

        reachableTiles =
            rangeFinder.GetReachableTiles(
                selectedUnit.CurrentTile,
                selectedUnit.RemainingMovementPoints,
                selectedUnit
            );
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

        if (target == null || start == null || selectedUnit == null)
            return;
    
        if (lastPreviewTarget == target)
            return;

        ClearPreviewPath();

        List<GridTile> path = pathFinder.FindPath(start, target, selectedUnit);

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
        if (actionMenu != null)
            actionMenu.Hide();

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
