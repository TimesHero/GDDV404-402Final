using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleDebugPainter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private ObstacleManager obstacleManager;

    [Header("Auto Loaded Obstacle Data")]
    [SerializeField] private ObstacleData[] obstacleTypes;
    [SerializeField] private int selectedIndex = 0;

    [Header("Debug UI")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private int fontSize = 22;
    [SerializeField] private Vector2 uiPosition = new Vector2(20f, 160f);
    [SerializeField] private Vector2 uiSize = new Vector2(520f, 140f);

    private InputSystem_Actions inputActions;
    private Vector2 pointerPosition;

    private GUIStyle labelStyle;
    private Texture2D backgroundTexture;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();

        obstacleTypes = Resources.LoadAll<ObstacleData>("ObstacleTypes");

        if (obstacleTypes.Length == 0)
            Debug.LogWarning("No ObstacleData found in Resources/ObstacleTypes");

        CreateGUIResources();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Gameplay.PointerPosition.performed += OnPointerMove;
        inputActions.Gameplay.PaintClick.performed += OnPaintClick;
        inputActions.Gameplay.EraseClick.performed += OnEraseClick;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.PointerPosition.performed -= OnPointerMove;
        inputActions.Gameplay.PaintClick.performed -= OnPaintClick;
        inputActions.Gameplay.EraseClick.performed -= OnEraseClick;
        inputActions.Disable();
    }

    private void Update()
    {
        HandleObstacleSelectionInput();
    }

    private void OnPointerMove(InputAction.CallbackContext context)
    {
        pointerPosition = context.ReadValue<Vector2>();
    }

    private void OnPaintClick(InputAction.CallbackContext context)
    {
        if (!enabled)
            return;

        if (obstacleManager == null)
        {
            Debug.LogError("ObstacleDebugPainter: ObstacleManager reference is missing.");
            return;
        }

        if (obstacleTypes == null || obstacleTypes.Length == 0)
            return;

        ObstacleData selected = obstacleTypes[selectedIndex];

        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
            return;

        GridTile tile = hit.collider.GetComponent<GridTile>();

        if (tile == null)
            return;

        bool success = obstacleManager.TryPlaceObstacle(selected, tile.GridPosition);

        if (success)
            Debug.Log($"Placed obstacle: {selected.name} at {tile.GridPosition}");
        else
            Debug.Log($"Could not place obstacle: {selected.name} at {tile.GridPosition}");
    }

    private void OnEraseClick(InputAction.CallbackContext context)
    {
        if (!enabled)
            return;

        if (obstacleManager == null)
        {
            Debug.LogError("ObstacleDebugPainter: ObstacleManager reference is missing.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
            return;

        GridTile tile = hit.collider.GetComponent<GridTile>();

        if (tile == null)
            return;

        bool success = obstacleManager.TryRemoveObstacleAtTile(tile.GridPosition);

        if (success)
            Debug.Log($"Removed obstacle at {tile.GridPosition}");
        else
            Debug.Log($"No obstacle found at {tile.GridPosition}");
    }

    private void HandleObstacleSelectionInput()
    {
        if (obstacleTypes == null || obstacleTypes.Length == 0)
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
        if (obstacleTypes == null || obstacleTypes.Length == 0)
            return;

        if (index < 0 || index >= obstacleTypes.Length)
            return;

        selectedIndex = index;
        Debug.Log($"Selected obstacle: {obstacleTypes[selectedIndex].name}");
    }

    private void CycleSelection(int direction)
    {
        if (obstacleTypes == null || obstacleTypes.Length == 0)
            return;

        selectedIndex += direction;

        if (selectedIndex < 0)
            selectedIndex = obstacleTypes.Length - 1;
        else if (selectedIndex >= obstacleTypes.Length)
            selectedIndex = 0;

        Debug.Log($"Selected obstacle: {obstacleTypes[selectedIndex].name}");
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

        if (obstacleTypes == null || obstacleTypes.Length == 0)
            return;

        Rect boxRect = new Rect(uiPosition.x, uiPosition.y, uiSize.x, uiSize.y);
        GUI.DrawTexture(boxRect, backgroundTexture);

        ObstacleData selected = obstacleTypes[selectedIndex];

        string hotkeyHint = obstacleTypes.Length <= 10
            ? "Hotkeys: 1-9, 0 | Q/E or Mouse Wheel for more"
            : "Hotkeys: 1-9, 0";

        string text =
            $"<b>Obstacle Painter</b>\n" +
            $"Selected: {selected.name} ({selectedIndex + 1}/{obstacleTypes.Length})\n" +
            $"Footprint: {selected.FootprintSize.x}x{selected.FootprintSize.y}\n" +
            $"{hotkeyHint}\n" +
            $"Right Click: Place | Middle Click: Erase";

        GUI.Label(
            new Rect(boxRect.x + 12, boxRect.y + 10, boxRect.width - 24, boxRect.height - 20),
            text,
            labelStyle
        );
    }
}