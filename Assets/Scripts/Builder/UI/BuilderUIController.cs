using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuilderUIController : MonoBehaviour
{
    [SerializeField] private BuilderSaveLoadManager builderSaveLoadManager;
    [Header("Grid Size")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TMP_InputField gridWidthInput;
    [SerializeField] private TMP_InputField gridHeightInput;
    
    [Header("Slider Limits")]
    [SerializeField] private int minBrushSize = 1;
    [SerializeField] private int maxBrushSize = 5;
    [SerializeField] private int minElevationValue = 0;
    [SerializeField] private int maxElevationValue = 5;
    
    [Header("Elevation")]
    [SerializeField] private Slider elevationSlider;
    [SerializeField] private TMP_Text elevationText;
    
    [Header("References")]
    [SerializeField] private BuilderStateController builderStateController;
    [SerializeField] private Toggle terrainToggle;
    [SerializeField] private Toggle obstacleToggle;
    [SerializeField] private Toggle unitToggle;
    [SerializeField] private Toggle elevationToggle;
    [SerializeField] private GameObject helpClosedPanel;
    [SerializeField] private GameObject helpOpenPanel;

    [Header("Selection Text")]
    [SerializeField] private TMP_Text terrainText;
    [SerializeField] private TMP_Text obstacleText;
    [SerializeField] private TMP_Text unitText;
    [SerializeField] private TMP_Text unitTeamText;
    [SerializeField] private TMP_Text brushSizeText;

    [Header("Brush Size")]
    [SerializeField] private Slider brushSizeSlider;
    

    private void Start()
    {
        if (brushSizeSlider != null)
        {
            brushSizeSlider.minValue = minBrushSize;
            brushSizeSlider.maxValue = maxBrushSize;
            brushSizeSlider.wholeNumbers = true;
            brushSizeSlider.value = builderStateController != null ? builderStateController.BrushSize : minBrushSize;
        }

        if (elevationSlider != null)
        {
            elevationSlider.minValue = minElevationValue;
            elevationSlider.maxValue = maxElevationValue;
            elevationSlider.wholeNumbers = true;
            elevationSlider.value = builderStateController != null ? builderStateController.SelectedElevationValue : minElevationValue;
        }
        
        if (gridManager != null)
        {
            if (gridWidthInput != null)
                gridWidthInput.text = gridManager.Width.ToString();

            if (gridHeightInput != null)
                gridHeightInput.text = gridManager.Height.ToString();
        }
        RefreshUI();
    }
    
    public void ApplyGridSizeFromUI()
    {
        if (gridManager == null)
            return;

        if (gridWidthInput == null || gridHeightInput == null)
            return;

        if (!int.TryParse(gridWidthInput.text, out int newWidth))
            return;

        if (!int.TryParse(gridHeightInput.text, out int newHeight))
            return;

        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        if (builderSaveLoadManager != null)
            builderSaveLoadManager.ClearBuilderBeforeGridResize();

        gridManager.RebuildGrid(newWidth, newHeight);

        gridWidthInput.text = gridManager.Width.ToString();
        gridHeightInput.text = gridManager.Height.ToString();

        Debug.Log($"UI Grid Size changed to: {gridManager.Width} x {gridManager.Height}");
    }

    public void RefreshUI()
    {
        if (elevationText != null)
            elevationText.text = $"Elevation: {builderStateController.SelectedElevationValue}";
        
        if (builderStateController == null)
            return;

        if (terrainText != null)
            terrainText.text = $"Terrain: {builderStateController.CurrentTerrainName}";

        if (obstacleText != null)
            obstacleText.text = $"Obstacle: {builderStateController.CurrentObstacleName}";

        if (unitText != null)
            unitText.text = $"Unit: {builderStateController.CurrentUnitName}";

        if (unitTeamText != null)
            unitTeamText.text = $"Team: {builderStateController.SelectedUnitPaintTeam}";

        if (brushSizeText != null)
            brushSizeText.text = $"Brush Size: {builderStateController.BrushSize}";
    }

    public void NextTerrain()
    {
        if (builderStateController == null)
            return;

        builderStateController.SelectNextTerrain();
        RefreshUI();
    }

    public void PreviousTerrain()
    {
        if (builderStateController == null)
            return;

        builderStateController.SelectPreviousTerrain();
        RefreshUI();
    }

    public void NextObstacle()
    {
        if (builderStateController == null)
            return;

        builderStateController.SelectNextObstacle();
        RefreshUI();
    }

    public void PreviousObstacle()
    {
        if (builderStateController == null)
            return;

        builderStateController.SelectPreviousObstacle();
        RefreshUI();
    }

    public void NextUnit()
    {
        if (builderStateController == null)
            return;

        builderStateController.SelectNextUnit();
        RefreshUI();
    }

    public void PreviousUnit()
    {
        if (builderStateController == null)
            return;

        builderStateController.SelectPreviousUnit();
        RefreshUI();
    }

    public void CycleUnitPaintTeam()
    {
        if (builderStateController == null)
            return;

        builderStateController.CycleUnitPaintTeam();
        RefreshUI();
    }

    public void OnBrushSizeChanged(float value)
    {
        if (builderStateController == null)
            return;

        int brushSize = Mathf.RoundToInt(value);
        brushSize = Mathf.Clamp(brushSize, minBrushSize, maxBrushSize);

        builderStateController.SetBrushSize(brushSize);

        if (brushSizeSlider != null)
            brushSizeSlider.SetValueWithoutNotify(brushSize);

        RefreshUI();

        Debug.Log($"UI Brush Size changed to: {brushSize}");
    }
    
    public void OnTerrainToggleChanged(bool isOn)
    {
        if (!isOn || builderStateController == null)
            return;

        builderStateController.SetToolMode(BuilderToolMode.TerrainPaint);
        RefreshUI();
    }

    public void OnObstacleToggleChanged(bool isOn)
    {
        if (!isOn || builderStateController == null)
            return;

        builderStateController.SetToolMode(BuilderToolMode.ObstaclePaint);
        RefreshUI();
    }

    public void OnUnitToggleChanged(bool isOn)
    {
        if (!isOn || builderStateController == null)
            return;

        builderStateController.SetToolMode(BuilderToolMode.UnitPaint);
        RefreshUI();
    }

    public void OnElevationToggleChanged(bool isOn)
    {
        if (!isOn || builderStateController == null)
            return;

        builderStateController.SetToolMode(BuilderToolMode.ElevationPaint);
        RefreshUI();
    }
    
    public void ShowInstructionsPanel()
    {
        if (helpClosedPanel != null)
            helpClosedPanel.SetActive(false);

        if (helpOpenPanel != null)
            helpOpenPanel.SetActive(true);
    }

    public void HideInstructionsPanel()
    {
        if (helpOpenPanel != null)
            helpOpenPanel.SetActive(false);

        if (helpClosedPanel != null)
            helpClosedPanel.SetActive(true);
    }
    
    public void OnElevationValueChanged(float value)
    {
        if (builderStateController == null)
            return;

        int elevationValue = Mathf.RoundToInt(value);
        elevationValue = Mathf.Clamp(elevationValue, minElevationValue, maxElevationValue);

        builderStateController.SetSelectedElevationValue(elevationValue);

        if (elevationSlider != null)
            elevationSlider.SetValueWithoutNotify(elevationValue);

        RefreshUI();

        Debug.Log($"UI Elevation Value changed to: {elevationValue}");
    }
}