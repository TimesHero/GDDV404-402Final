using UnityEngine;
using UnityEngine.InputSystem;

public class TileSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tileLayerMask;
    
    [Header("Hover Colors")]
    [SerializeField] public Color hoverColor = Color.darkMagenta;
    
    
    private InputSystem_Actions inputActions;
    
    private Vector2 pointerPosition;
    private GridTile currentHoveredTile;
    private GridTile previousHoveredTile;
    
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
        if (currentHoveredTile != null)
        {
            Debug.Log($"Clicked tile: {currentHoveredTile.X}, {currentHoveredTile.Y}");
        }
    }
    
    private void HandleTileHover()
    {
        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile != currentHoveredTile)
            {
                if (previousHoveredTile != null)
                    previousHoveredTile.ResetHighlight();
                previousHoveredTile = tile;
                currentHoveredTile = tile;
                
                currentHoveredTile.SetHighlight(hoverColor);
                Debug.Log($"Hovering tile: {tile.X}, {tile.Y}");;
            }
        }
        else
        {
            if (previousHoveredTile != null)
            {
                currentHoveredTile.ResetHighlight();
                currentHoveredTile = null;
                previousHoveredTile = null;
            }
        }
    }
}
