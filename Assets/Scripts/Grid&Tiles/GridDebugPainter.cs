using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridDebugPainter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tileLayerMask;
    
    [Header("Paint Settings")]
    [SerializeField] private TerrainType selectedTerrain = TerrainType.Forest;
    
    private InputSystem_Actions inputActions;
    private Vector2 pointerPosition;
    
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
    private void OnPointerMove(InputAction.CallbackContext context)
    {
        pointerPosition = context.ReadValue<Vector2>();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);
        
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
        {
            return;
        }
        
        GridTile tile = hit.collider.GetComponent<GridTile>();
        
        if (tile == null)
        {
            return;
        }
        
        tile.terrainType = selectedTerrain;
        tile.ApplyTerrainSettings();
        
        Debug.Log($"Painted {tile.name} as {selectedTerrain}");
        
    }
}
