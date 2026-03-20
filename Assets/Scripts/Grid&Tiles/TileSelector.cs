using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class TileSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private AStarPathFinder pathFinder;
    
    [Header("Hover Colors")]
    [SerializeField] public Color hoverColor = Color.darkMagenta;
    
    
    private InputSystem_Actions inputActions;
    
    private Vector2 pointerPosition;
    private GridTile currentHoveredTile;
    private GridTile previousHoveredTile;
    
    private GridTile selectedStartTile;
    private GridTile selectedTargetTile;
    
    private List<GridTile> currentPath = new List<GridTile>();
    
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
        if (selectedStartTile == null)
        {
            ClearPathPreview();
            
            selectedStartTile = currentHoveredTile;
            selectedStartTile.ShowAsStart();
            Debug.Log($"Start tile selected: {selectedStartTile.X}, {selectedStartTile.Y}");
            return;
        }
        
        selectedTargetTile = currentHoveredTile;
        List<GridTile> path = pathFinder.FindPath(selectedStartTile, selectedTargetTile);

        ClearPathPreview();
        
        selectedStartTile.ShowAsStart();
        selectedTargetTile.ShowAsTarget();
        
        if (path != null)
        {
            currentPath = path;
            foreach (GridTile tile in currentPath)
            {
                if (tile == selectedStartTile || tile == selectedTargetTile)
                    continue;
                tile.ShowAsPath();
            }
            Debug.Log($"Path found. Length: {currentPath.Count}");
        }
        else
        {
            Debug.Log("No path found.");
        }
        
        selectedStartTile = null;
        selectedTargetTile = null;
    }
    
    private void HandleTileHover()
    {
        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile != currentHoveredTile)
            {
                if (previousHoveredTile != null && !IsTileInPersistentState(previousHoveredTile))
                    previousHoveredTile.ResetHighlight();
                
                previousHoveredTile = tile;
                currentHoveredTile = tile;
                
                if (!IsTileInPersistentState(currentHoveredTile))
                    currentHoveredTile.SetHighlight(hoverColor);
                //Debug.Log($"Hovering tile: {tile.X}, {tile.Y}");;
            }
        }
        else
        {
            if (currentHoveredTile != null && !IsTileInPersistentState(currentHoveredTile))
                currentHoveredTile.ResetHighlight();
            
            currentHoveredTile = null;
            previousHoveredTile = null;
        }
    }
    private void ClearPathPreview()
    {
        foreach (GridTile tile in currentPath)
        {
            if(tile != null)
                tile.ResetHighlight();
        }
        
        if (selectedStartTile != null)
            selectedStartTile.ResetHighlight();
        if (selectedTargetTile != null)
            selectedTargetTile.ResetHighlight();
        
        currentPath.Clear();
    }

    private bool IsTileInPersistentState(GridTile tile)
    {
        if (tile == null)
            return false;
        if (tile == selectedStartTile)
            return true;
        if (tile == selectedTargetTile)
            return true;
        
        return currentPath.Contains(tile);
    }
}
