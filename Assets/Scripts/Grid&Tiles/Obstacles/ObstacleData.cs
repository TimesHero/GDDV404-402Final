using System;
using UnityEngine;

[Serializable]
public class ObstacleRotationTransformPreset
{
    public int RotationY;
    public Vector3 VisualOffset;
    public Vector3 VisualRotationEuler;
    public Vector3 VisualScale = Vector3.one;
}

[CreateAssetMenu(fileName = "Obstacle_", menuName = "Grid/Obstacle Data")]
public class ObstacleData : ScriptableObject
{
    [Header("Rotation Transform Mode")]
    [SerializeField] private bool useRotationPresets = false;
    
    [Header("Optional Rotation Presets")]
    [SerializeField] private ObstacleRotationTransformPreset rotation0Preset;
    [SerializeField] private ObstacleRotationTransformPreset rotation90Preset;
    [SerializeField] private ObstacleRotationTransformPreset rotation180Preset;
    [SerializeField] private ObstacleRotationTransformPreset rotation270Preset;
    
    [Header("Transform")]
        [SerializeField] private Vector3 visualOffset = Vector3.zero;
        [SerializeField] private Vector3 visualRotationEuler = Vector3.zero;
        [SerializeField] private Vector3 visualScale = Vector3.one;
        
    [Header("Builder Placement")]
    public Vector3 VisualAnchorOffsetFromOrigin = Vector3.zero;
    
    [Header("ID")]
    [SerializeField] private string obstacleId = "Obstacle";

    [Header("Visual")]
    [SerializeField] private GameObject obstaclePrefab;

    [Header("Gameplay")]
    [SerializeField] private bool blocksMovement = true;

    [Header("Footprint")]
    [SerializeField] private Vector2Int footprintSize = Vector2Int.one;
    
    [Header("Surface Affect")]
    [SerializeField] private bool paintTerrainUnderObstacle = false;
    [SerializeField] private TerrainType terrainTypeUnderObstacle = TerrainType.Blocked;
    
 
    public bool UseRotationPresets => useRotationPresets;
    public string ObstacleId => obstacleId;
    public GameObject ObstaclePrefab => obstaclePrefab;
    public Vector3 VisualOffset => visualOffset;
    public bool BlocksMovement => blocksMovement;
    public Vector2Int FootprintSize => footprintSize;
    
    public Vector3 VisualRotationEuler => visualRotationEuler;
    public Vector3 VisualScale => visualScale;
    
    public bool PaintTerrainUnderObstacle => paintTerrainUnderObstacle;
    public TerrainType TerrainTypeUnderObstacle => terrainTypeUnderObstacle;
    
    public ObstacleRotationTransformPreset GetTransformPresetForRotation(int rotationY)
    {
        rotationY = NormalizeRotation(rotationY);

        switch (rotationY)
        {
            case 90:
                return rotation90Preset;
            case 180:
                return rotation180Preset;
            case 270:
                return rotation270Preset;
            default:
                return rotation0Preset;
        }
    }

    public Vector3 GetVisualOffsetForRotation(int rotationY)
    {
        if (!useRotationPresets)
            return VisualOffset;

        ObstacleRotationTransformPreset preset = GetTransformPresetForRotation(rotationY);

        if (preset != null)
            return preset.VisualOffset;

        return VisualOffset;
    }

    public Vector3 GetVisualRotationEulerForRotation(int rotationY)
    {
        if (!useRotationPresets)
        {
            Vector3 euler = VisualRotationEuler;
            euler.y += NormalizeRotation(rotationY);
            return euler;
        }

        ObstacleRotationTransformPreset preset = GetTransformPresetForRotation(rotationY);

        if (preset != null)
            return preset.VisualRotationEuler;

        Vector3 fallbackEuler = VisualRotationEuler;
        fallbackEuler.y += NormalizeRotation(rotationY);
        return fallbackEuler;
    }

    public Vector3 GetVisualScaleForRotation(int rotationY)
    {
        if (!useRotationPresets)
            return VisualScale;

        ObstacleRotationTransformPreset preset = GetTransformPresetForRotation(rotationY);

        if (preset != null && preset.VisualScale != Vector3.zero)
            return preset.VisualScale;

        return VisualScale;
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