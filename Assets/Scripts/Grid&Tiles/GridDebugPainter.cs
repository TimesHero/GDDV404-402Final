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

    [Header("Debug UI")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private int fontSize = 22;
    [SerializeField] private Vector2 uiPosition = new Vector2(20f, 20f);
    [SerializeField] private Vector2 uiSize = new Vector2(460f, 120f);

    private InputSystem_Actions inputActions;
    private Vector2 pointerPosition;

    private GUIStyle labelStyle;
    private Texture2D backgroundTexture;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();

        terrainTypes = Resources.LoadAll<TerrainTypeData>("TerrainTypes");

        if (terrainTypes.Length == 0)
            Debug.LogError("No TerrainTypeData found in Resources/TerrainTypes");

        CreateGUIResources();
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
        HandleTerrainSelectionInput();
    }

    private void OnPointerMove(InputAction.CallbackContext context)
    {
        pointerPosition = context.ReadValue<Vector2>();
    }

    private void OnPaintClick(InputAction.CallbackContext context)
    {
        if (!enabled)
            return;

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

    private void HandleTerrainSelectionInput()
    {
        if (terrainTypes == null || terrainTypes.Length == 0)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame) TrySetSelectedIndex(0);
        if (keyboard.digit2Key.wasPressedThisFrame) TrySetSelectedIndex(1);
        if (keyboard.digit3Key.wasPressedThisFrame) TrySetSelectedIndex(2);
        if (keyboard.digit4Key.wasPressedThisFrame) TrySetSelectedIndex(3);
        if (keyboard.digit5Key.wasPressedThisFrame) TrySetSelectedIndex(4);
        if (keyboard.digit6Key.wasPressedThisFrame) TrySetSelectedIndex(5);
        if (keyboard.digit7Key.wasPressedThisFrame) TrySetSelectedIndex(6);
        if (keyboard.digit8Key.wasPressedThisFrame) TrySetSelectedIndex(7);
        if (keyboard.digit9Key.wasPressedThisFrame) TrySetSelectedIndex(8);
        if (keyboard.digit0Key.wasPressedThisFrame) TrySetSelectedIndex(9);

        if (keyboard.qKey.wasPressedThisFrame)
            CycleSelection(-1);

        if (keyboard.eKey.wasPressedThisFrame)
            CycleSelection(1);

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;

            if (scrollY > 0.01f)
                CycleSelection(-1);
            else if (scrollY < -0.01f)
                CycleSelection(1);
        }
    }

    private void TrySetSelectedIndex(int index)
    {
        if (terrainTypes == null || terrainTypes.Length == 0)
            return;

        if (index < 0 || index >= terrainTypes.Length)
            return;

        selectedIndex = index;
        Debug.Log($"Selected terrain: {terrainTypes[selectedIndex].name}");
    }

    private void CycleSelection(int direction)
    {
        if (terrainTypes == null || terrainTypes.Length == 0)
            return;

        selectedIndex += direction;

        if (selectedIndex < 0)
            selectedIndex = terrainTypes.Length - 1;
        else if (selectedIndex >= terrainTypes.Length)
            selectedIndex = 0;

        Debug.Log($"Selected terrain: {terrainTypes[selectedIndex].name}");
    }

    private void CreateGUIResources()
    {
        labelStyle = new GUIStyle
        {
            fontSize = fontSize,
            normal = { textColor = Color.black },
            richText = true,
            alignment = TextAnchor.UpperLeft
        };

        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.85f));
        backgroundTexture.Apply();
    }

    private void OnGUI()
    {
        if (!showDebugUI || !enabled)
            return;

        if (terrainTypes == null || terrainTypes.Length == 0)
            return;

        Rect boxRect = new Rect(uiPosition.x, uiPosition.y, uiSize.x, uiSize.y);
        GUI.DrawTexture(boxRect, backgroundTexture);

        TerrainTypeData selected = terrainTypes[selectedIndex];

        string hotkeyHint = terrainTypes.Length <= 10
            ? "Hotkeys: 1-9, 0"
            : "Hotkeys: 1-9, 0 | Q/E or Mouse Wheel for more";

        string text =
            $"<b>Terrain Painter</b>\n" +
            $"Selected: {selected.name} ({selectedIndex + 1}/{terrainTypes.Length})\n" +
            $"Type: {selected.TerrainType} | Cost: {selected.MovementCost} | Walkable: {selected.IsWalkable}\n" +
            $"{hotkeyHint} | Right Click: Paint";

        GUI.Label(
            new Rect(boxRect.x + 12, boxRect.y + 10, boxRect.width - 24, boxRect.height - 20),
            text,
            labelStyle
        );
    }
}