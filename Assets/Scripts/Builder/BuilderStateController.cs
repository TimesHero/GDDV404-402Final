using UnityEngine;

public class BuilderStateController : MonoBehaviour
{
    [Header("Current Tool")]
    [SerializeField] private BuilderToolMode currentToolMode = BuilderToolMode.TerrainPaint;

    [Header("Brush Settings")]
    [SerializeField] private int brushSize = 1;

    [Header("Elevation Paint")]
    [SerializeField] private int selectedElevationValue = 0;

    [Header("Unit Paint Team")]
    [SerializeField] private BuilderUnitPaintTeam selectedUnitPaintTeam = BuilderUnitPaintTeam.Player;

    [Header("Loaded Assets Debug")]
    [SerializeField] private TerrainTypeData[] loadedTerrainTypes;
    [SerializeField] private ObstacleData[] loadedObstacleTypes;
    [SerializeField] private InteractableData[] loadedInteractableTypes;

    [Header("Selection Indices")]
    [SerializeField] private int terrainIndex = 0;
    [SerializeField] private int obstacleIndex = 0;
    [SerializeField] private int interactableIndex = 0;
    [SerializeField] private int unitIndex = 0;
    
    [SerializeField] private int selectedObstacleRotationY = 0;
    [SerializeField] private int selectedInteractableRotationY = 0;
    [SerializeField] private int selectedUnitRotationY = 0;
    [SerializeField] private bool selectedUnitUsesCardinalFacing = false;

    public int SelectedObstacleRotationY => selectedObstacleRotationY;
    public int SelectedInteractableRotationY => selectedInteractableRotationY;
    public int SelectedUnitRotationY => selectedUnitRotationY;
    public bool SelectedUnitUsesCardinalFacing => selectedUnitUsesCardinalFacing;

    public BuilderToolMode CurrentToolMode => currentToolMode;
    public int BrushSize => brushSize;
    public int SelectedElevationValue => selectedElevationValue;
    public BuilderUnitPaintTeam SelectedUnitPaintTeam => selectedUnitPaintTeam;

    public TerrainType SelectedTerrainType
    {
        get
        {
            TerrainTypeData terrainData = GetCurrentTerrainData();
            return terrainData != null ? terrainData.TerrainType : TerrainType.Ground;
        }
    }

    public ObstacleData SelectedObstacleData => GetCurrentObstacleData();
    public InteractableData SelectedInteractableData => GetCurrentInteractableData();

    public UnitData SelectedUnitData
    {
        get
        {
            UnitData[] teamUnits = GetUnitsForSelectedTeam();

            if (teamUnits == null || teamUnits.Length == 0)
                return null;

            unitIndex = Mathf.Clamp(unitIndex, 0, teamUnits.Length - 1);
            return teamUnits[unitIndex];
        }
    }

    public string CurrentTerrainName
    {
        get
        {
            TerrainTypeData terrainData = GetCurrentTerrainData();
            return terrainData != null ? terrainData.name : "None";
        }
    }

    public string CurrentObstacleName
    {
        get
        {
            ObstacleData obstacleData = GetCurrentObstacleData();
            return obstacleData != null ? obstacleData.name : "None";
        }
    }
    public string CurrentInteractableName
    {
        get
        {
            InteractableData interactableData = GetCurrentInteractableData();
            return interactableData != null ? interactableData.displayName : "None";
        }
    }

    public string CurrentUnitName
    {
        get
        {
            UnitData unitData = SelectedUnitData;
            return unitData != null ? unitData.unitName : "None";
        }
    }

    private void Awake()
    {
        LoadAssetsFromResources();
        ClampSelectionIndices();
    }

    public void LoadAssetsFromResources()
    {
        loadedTerrainTypes = Resources.LoadAll<TerrainTypeData>("TerrainTypes");
        loadedObstacleTypes = Resources.LoadAll<ObstacleData>("ObstacleTypes");
        loadedInteractableTypes = Resources.LoadAll<InteractableData>("InteractableData");

        Debug.Log($"BuilderStateController loaded {loadedTerrainTypes.Length} terrain types.");
        Debug.Log($"BuilderStateController loaded {loadedObstacleTypes.Length} obstacle types.");
        Debug.Log($"BuilderStateController loaded {loadedInteractableTypes.Length} interactable types.");
        Debug.Log($"BuilderStateController loaded {GetUnitsForSelectedTeam().Length} unit types for team {selectedUnitPaintTeam}.");
    }

    public UnitData[] GetUnitsForSelectedTeam()
    {
        string path = selectedUnitPaintTeam == BuilderUnitPaintTeam.Player
            ? "UnitData/Player"
            : "UnitData/Enemy";

        return Resources.LoadAll<UnitData>(path);
    }

    public void SetToolMode(BuilderToolMode newMode)
    {
        currentToolMode = newMode;
        Debug.Log($"Builder Tool Mode set to: {currentToolMode}");
    }

    public void SetBrushSize(int newBrushSize)
    {
        brushSize = Mathf.Max(1, newBrushSize);
        Debug.Log($"Builder Brush Size set to: {brushSize}");
    }

    public void SetSelectedElevationValue(int elevationValue)
    {
        selectedElevationValue = Mathf.Max(0, elevationValue);
        Debug.Log($"Selected Elevation Value set to: {selectedElevationValue}");
    }

    public void SetSelectedUnitPaintTeam(BuilderUnitPaintTeam team)
    {
        selectedUnitPaintTeam = team;
        unitIndex = 0;
        selectedUnitUsesCardinalFacing = false;

        Debug.Log($"Selected Unit Paint Team set to: {selectedUnitPaintTeam}");
        Debug.Log($"Loaded {GetUnitsForSelectedTeam().Length} unit types for team {selectedUnitPaintTeam}.");
    }

    public void CycleUnitPaintTeam()
    {
        selectedUnitPaintTeam = selectedUnitPaintTeam == BuilderUnitPaintTeam.Player
            ? BuilderUnitPaintTeam.Enemy
            : BuilderUnitPaintTeam.Player;

        unitIndex = 0;
        selectedUnitUsesCardinalFacing = false;

        Debug.Log($"Selected Unit Paint Team set to: {selectedUnitPaintTeam}");
        Debug.Log($"Loaded {GetUnitsForSelectedTeam().Length} unit types for team {selectedUnitPaintTeam}.");
    }

    public void SelectNextTerrain()
    {
        if (loadedTerrainTypes == null || loadedTerrainTypes.Length == 0)
            return;

        terrainIndex = (terrainIndex + 1) % loadedTerrainTypes.Length;
        Debug.Log($"Selected Terrain set to: {CurrentTerrainName}");
    }

    public void SelectPreviousTerrain()
    {
        if (loadedTerrainTypes == null || loadedTerrainTypes.Length == 0)
            return;

        terrainIndex--;
        if (terrainIndex < 0)
            terrainIndex = loadedTerrainTypes.Length - 1;

        Debug.Log($"Selected Terrain set to: {CurrentTerrainName}");
    }

    public void SelectNextObstacle()
    {
        if (loadedObstacleTypes == null || loadedObstacleTypes.Length == 0)
            return;

        obstacleIndex = (obstacleIndex + 1) % loadedObstacleTypes.Length;
        Debug.Log($"Selected Obstacle set to: {CurrentObstacleName}");
    }

    public void SelectPreviousObstacle()
    {
        if (loadedObstacleTypes == null || loadedObstacleTypes.Length == 0)
            return;

        obstacleIndex--;
        if (obstacleIndex < 0)
            obstacleIndex = loadedObstacleTypes.Length - 1;

        Debug.Log($"Selected Obstacle set to: {CurrentObstacleName}");
    }

    public void SelectNextUnit()
    {
        UnitData[] teamUnits = GetUnitsForSelectedTeam();

        if (teamUnits == null || teamUnits.Length == 0)
            return;

        unitIndex = (unitIndex + 1) % teamUnits.Length;
        selectedUnitUsesCardinalFacing = false;
        Debug.Log($"Selected Unit set to: {CurrentUnitName}");
    }

    public void SelectPreviousUnit()
    {
        UnitData[] teamUnits = GetUnitsForSelectedTeam();

        if (teamUnits == null || teamUnits.Length == 0)
            return;

        unitIndex--;
        if (unitIndex < 0)
            unitIndex = teamUnits.Length - 1;

        selectedUnitUsesCardinalFacing = false;
        Debug.Log($"Selected Unit set to: {CurrentUnitName}");
    }

    public TerrainTypeData GetCurrentTerrainData()
    {
        if (loadedTerrainTypes == null || loadedTerrainTypes.Length == 0)
            return null;

        terrainIndex = Mathf.Clamp(terrainIndex, 0, loadedTerrainTypes.Length - 1);
        return loadedTerrainTypes[terrainIndex];
    }

    public ObstacleData GetCurrentObstacleData()
    {
        if (loadedObstacleTypes == null || loadedObstacleTypes.Length == 0)
            return null;

        obstacleIndex = Mathf.Clamp(obstacleIndex, 0, loadedObstacleTypes.Length - 1);
        return loadedObstacleTypes[obstacleIndex];
    }
    
    private InteractableData GetCurrentInteractableData()
    {
        if (loadedInteractableTypes == null || loadedInteractableTypes.Length == 0)
            return null;

        interactableIndex = Mathf.Clamp(interactableIndex, 0, loadedInteractableTypes.Length - 1);
        return loadedInteractableTypes[interactableIndex];
    }

    public void SelectNextInteractable()
    {
        if (loadedInteractableTypes == null || loadedInteractableTypes.Length == 0)
            return;

        interactableIndex = (interactableIndex + 1) % loadedInteractableTypes.Length;
        Debug.Log($"Selected Interactable set to: {CurrentInteractableName}");
    }

    public void SelectPreviousInteractable()
    {
        if (loadedInteractableTypes == null || loadedInteractableTypes.Length == 0)
            return;

        interactableIndex--;
        if (interactableIndex < 0)
            interactableIndex = loadedInteractableTypes.Length - 1;

        Debug.Log($"Selected Interactable set to: {CurrentInteractableName}");
    }

    public void RotateSelectedInteractableClockwise()
    {
        selectedInteractableRotationY += 90;
        if (selectedInteractableRotationY >= 360)
            selectedInteractableRotationY = 0;

        Debug.Log($"Selected Interactable Rotation set to: {selectedInteractableRotationY}");
    }

    public void RotateSelectedInteractableCounterClockwise()
    {
        selectedInteractableRotationY -= 90;
        if (selectedInteractableRotationY < 0)
            selectedInteractableRotationY = 270;

        Debug.Log($"Selected Interactable Rotation set to: {selectedInteractableRotationY}");
    }

    private void ClampSelectionIndices()
    {
        if (loadedInteractableTypes != null && loadedInteractableTypes.Length > 0)
            interactableIndex = Mathf.Clamp(interactableIndex, 0, loadedInteractableTypes.Length - 1);
        else
            interactableIndex = 0;
        if (loadedTerrainTypes != null && loadedTerrainTypes.Length > 0)
            terrainIndex = Mathf.Clamp(terrainIndex, 0, loadedTerrainTypes.Length - 1);
        else
            terrainIndex = 0;

        if (loadedObstacleTypes != null && loadedObstacleTypes.Length > 0)
            obstacleIndex = Mathf.Clamp(obstacleIndex, 0, loadedObstacleTypes.Length - 1);
        else
            obstacleIndex = 0;

        UnitData[] teamUnits = GetUnitsForSelectedTeam();
        if (teamUnits != null && teamUnits.Length > 0)
            unitIndex = Mathf.Clamp(unitIndex, 0, teamUnits.Length - 1);
        else
            unitIndex = 0;
    }
    
    public void SetSelectedObstacleRotationY(int rotationY)
    {
        selectedObstacleRotationY = NormalizeRotation(rotationY);
        Debug.Log($"Selected Obstacle Rotation set to: {selectedObstacleRotationY}");
    }
    
    public void SetSelectedInteractableRotationY(int rotationY)
    {
        selectedInteractableRotationY = NormalizeRotation(rotationY);
        Debug.Log($"Selected Interactable Rotation set to: {selectedInteractableRotationY}");
    }

    public void SetSelectedUnitRotationY(int rotationY)
    {
        selectedUnitRotationY = NormalizeRotation(rotationY);
        selectedUnitUsesCardinalFacing = true;
        Debug.Log($"Selected Unit Rotation set to: {selectedUnitRotationY}");
    }

    private int NormalizeRotation(int rotationY)
    {
        rotationY %= 360;
        if (rotationY < 0)
            rotationY += 360;

        if (rotationY >= 315 || rotationY < 45) return 0;
        if (rotationY >= 45 && rotationY < 135) return 90;
        if (rotationY >= 135 && rotationY < 225) return 180;
        return 270;
    }
}
