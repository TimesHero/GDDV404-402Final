using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridDebugPainter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tileLayerMask;
    
    [Header("Auto Loaded Terrain Data")]
    [SerializeField] private TerrainTypeData[] terrainTypes;
    [SerializeField] private int selectedIndex = 0;
    
    private InputSystem_Actions inputActions;
    private Vector2 pointerPosition;
    
    private GUIStyle guiStyle;
    private GUIStyle helpStyle;
    
    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        
        terrainTypes = Resources.LoadAll<TerrainTypeData>("TerrainTypes");

        if (terrainTypes.Length == 0)
            Debug.LogError("No TerrainTypeData found in Resources/TerrainTypes");
        
        SetupGUIStyles();
    }
    
    private void OnEnable()
    {
       inputActions.Enable();
       inputActions.Gameplay.PointerPosition.performed += OnPointerMove;
       inputActions.Gameplay.PaintClick.performed += OnPaintClick;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.PointerPosition.performed -= OnPointerMove;
        inputActions.Gameplay.PaintClick.performed -= OnPaintClick;
        inputActions.Disable();
    }
    private void Update()
    {
        HandleTerrainSelectionHotkeys();
    }
    private void SetupGUIStyles()
    {
        guiStyle = new GUIStyle();
        guiStyle.fontSize = 24;
        guiStyle.fontStyle = FontStyle.Bold;
        guiStyle.normal.textColor = Color.black;

        helpStyle = new GUIStyle();
        helpStyle.fontSize = 18;
        helpStyle.fontStyle = FontStyle.Normal;
        helpStyle.normal.textColor = Color.black;
    }

    private void OnPointerMove(InputAction.CallbackContext context)
    {
        pointerPosition = context.ReadValue<Vector2>();
    }
    
    private void HandleTerrainSelectionHotkeys()
    {
        if (terrainTypes == null || terrainTypes.Length == 0)
            return;

        // Number keys 1-9 select indexes 0-8
        if (Keyboard.current.digit1Key.wasPressedThisFrame && terrainTypes.Length > 0)
            selectedIndex = 0;

        if (Keyboard.current.digit2Key.wasPressedThisFrame && terrainTypes.Length > 1)
            selectedIndex = 1;

        if (Keyboard.current.digit3Key.wasPressedThisFrame && terrainTypes.Length > 2)
            selectedIndex = 2;

        if (Keyboard.current.digit4Key.wasPressedThisFrame && terrainTypes.Length > 3)
            selectedIndex = 3;

        if (Keyboard.current.digit5Key.wasPressedThisFrame && terrainTypes.Length > 4)
            selectedIndex = 4;

        if (Keyboard.current.digit6Key.wasPressedThisFrame && terrainTypes.Length > 5)
            selectedIndex = 5;

        if (Keyboard.current.digit7Key.wasPressedThisFrame && terrainTypes.Length > 6)
            selectedIndex = 6;

        if (Keyboard.current.digit8Key.wasPressedThisFrame && terrainTypes.Length > 7)
            selectedIndex = 7;

        if (Keyboard.current.digit9Key.wasPressedThisFrame && terrainTypes.Length > 8)
            selectedIndex = 8;

        // 0 selects the 10th slot (index 9)
        if (Keyboard.current.digit0Key.wasPressedThisFrame && terrainTypes.Length > 9)
            selectedIndex = 9;

        // Q = previous
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            selectedIndex--;
            if (selectedIndex < 0)
                selectedIndex = terrainTypes.Length - 1;
        }

        // E = next
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            selectedIndex++;
            if (selectedIndex >= terrainTypes.Length)
                selectedIndex = 0;
        }
    }

    private void OnPaintClick(InputAction.CallbackContext context)
    {
        if (terrainTypes == null || terrainTypes.Length == 0)
            return;

        TerrainTypeData selected = terrainTypes[selectedIndex];

        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
            return;

        GridTile tile = hit.collider.GetComponent<GridTile>();

        if (tile == null)
            return;

        tile.TerrainType = selected.TerrainType;
        tile.ApplyTerrainSettings();

        Debug.Log($"Painted {tile.name} as {selected.name}");
        
    }

    private void OnGUI()
    {
        if (terrainTypes == null || terrainTypes.Length == 0)
            return;

        GUI.Label(
            new Rect(20, 20, 700, 40),
            $"Selected Terrain: {terrainTypes[selectedIndex].name}  ({selectedIndex + 1}/{terrainTypes.Length})",
            guiStyle);

        GUI.Label(
            new Rect(20, 55, 1000, 30),
            "Paint: Right Click   |   Select: 1-0   |   More Terrains: Q / E",
            helpStyle);

        GUI.Label(
            new Rect(20, 85, 1000, 30),
            "0 = slot 10",
            helpStyle);
    }
}
