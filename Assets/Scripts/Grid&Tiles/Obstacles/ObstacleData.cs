using UnityEngine;

[CreateAssetMenu(fileName = "Obstacle_", menuName = "Grid/Obstacle Data")]
public class ObstacleData : ScriptableObject
{
    [Header("ID")]
    [SerializeField] private string obstacleId = "Obstacle";

    [Header("Visual")]
    [SerializeField] private GameObject obstaclePrefab;
    
    [Header("Transform")]
    [SerializeField] private Vector3 visualOffset = Vector3.zero;
    [SerializeField] private Vector3 visualRotationEuler = Vector3.zero;
    [SerializeField] private Vector3 visualScale = Vector3.one;

    [Header("Gameplay")]
    [SerializeField] private bool blocksMovement = true;

    [Header("Footprint")]
    [SerializeField] private Vector2Int footprintSize = Vector2Int.one;
    
    [Header("Surface Affect")]
    [SerializeField] private bool paintTerrainUnderObstacle = false;
    [SerializeField] private TerrainType terrainTypeUnderObstacle = TerrainType.Blocked;
    
 

    public string ObstacleId => obstacleId;
    public GameObject ObstaclePrefab => obstaclePrefab;
    public Vector3 VisualOffset => visualOffset;
    public bool BlocksMovement => blocksMovement;
    public Vector2Int FootprintSize => footprintSize;
    
    public Vector3 VisualRotationEuler => visualRotationEuler;
    public Vector3 VisualScale => visualScale;
    
    public bool PaintTerrainUnderObstacle => paintTerrainUnderObstacle;
    public TerrainType TerrainTypeUnderObstacle => terrainTypeUnderObstacle;
}