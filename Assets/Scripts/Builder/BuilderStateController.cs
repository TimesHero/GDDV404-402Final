using UnityEngine;

public class BuilderStateController : MonoBehaviour
{
    [Header("Current Tool")]
    [SerializeField] private BuilderToolMode currentToolMode = BuilderToolMode.TerrainPaint;

    [Header("Terrain Selection")]
    [SerializeField] private TerrainType selectedTerrainType = TerrainType.Ground;

    [Header("Obstacle Selection")]
    [SerializeField] private ObstacleData selectedObstacleData;

    [Header("Unit Selection")]
    [SerializeField] private UnitData selectedUnitData;

    [Header("Elevation Paint")]
    [SerializeField] private int selectedElevationValue = 0;

    public BuilderToolMode CurrentToolMode => currentToolMode;
    public TerrainType SelectedTerrainType => selectedTerrainType;
    public ObstacleData SelectedObstacleData => selectedObstacleData;
    public UnitData SelectedUnitData => selectedUnitData;
    public int SelectedElevationValue => selectedElevationValue;

    public void SetToolMode(BuilderToolMode newMode)
    {
        currentToolMode = newMode;
        Debug.Log($"Builder Tool Mode set to: {currentToolMode}");
    }

    public void SetSelectedTerrainType(TerrainType terrainType)
    {
        selectedTerrainType = terrainType;
        Debug.Log($"Selected Terrain Type set to: {selectedTerrainType}");
    }

    public void SetSelectedObstacleData(ObstacleData obstacleData)
    {
        selectedObstacleData = obstacleData;

        string obstacleName = obstacleData != null ? obstacleData.name : "None";
        Debug.Log($"Selected Obstacle set to: {obstacleName}");
    }

    public void SetSelectedUnitData(UnitData unitData)
    {
        selectedUnitData = unitData;

        string unitName = unitData != null ? unitData.unitName : "None";
        Debug.Log($"Selected UnitData set to: {unitName}");
    }

    public void SetSelectedElevationValue(int elevationValue)
    {
        selectedElevationValue = Mathf.Max(0, elevationValue);
        Debug.Log($"Selected Elevation Value set to: {selectedElevationValue}");
    }
}