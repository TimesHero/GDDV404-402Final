using UnityEngine;
using UnityEngine.InputSystem;

public class DebugModeSwitcher : MonoBehaviour
{
    public enum DebugMode
    {
        Terrain,
        Obstacle
    }

    [Header("References")]
    [SerializeField] private GridDebugPainter gridDebugPainter;
    [SerializeField] private ObstacleDebugPainter obstacleDebugPainter;

    [Header("UI")]
    [SerializeField] private bool showDebugModeUI = true;
    [SerializeField] private int fontSize = 24;
    [SerializeField] private Vector2 uiPosition = new Vector2(20f, 300f);
    [SerializeField] private Vector2 uiSize = new Vector2(420f, 70f);

    [Header("State")]
    [SerializeField] private DebugMode currentMode = DebugMode.Terrain;

    private InputSystem_Actions inputActions;

    private GUIStyle labelStyle;
    private Texture2D backgroundTexture;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        CreateGUIResources();
        ApplyMode();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.rKey.wasPressedThisFrame)
        {
            ToggleMode();
        }
    }

    private void ToggleMode()
    {
        currentMode = currentMode == DebugMode.Terrain
            ? DebugMode.Obstacle
            : DebugMode.Terrain;

        ApplyMode();
        Debug.Log($"Debug Mode: {currentMode}");
    }

    private void ApplyMode()
    {
        if (gridDebugPainter != null)
            gridDebugPainter.enabled = currentMode == DebugMode.Terrain;

        if (obstacleDebugPainter != null)
            obstacleDebugPainter.enabled = currentMode == DebugMode.Obstacle;
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
        if (!showDebugModeUI)
            return;

        Rect boxRect = new Rect(uiPosition.x, uiPosition.y, uiSize.x, uiSize.y);
        GUI.DrawTexture(boxRect, backgroundTexture);

        string text =
            $"<b>Debug Mode</b>\n" +
            $"Current: {currentMode} | Press R to switch";

        GUI.Label(
            new Rect(boxRect.x + 12, boxRect.y + 10, boxRect.width - 24, boxRect.height - 20),
            text,
            labelStyle
        );
    }
}