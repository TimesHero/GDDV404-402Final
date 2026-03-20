using UnityEngine;

[CreateAssetMenu(fileName = "TerrainType_", menuName = "Grid/Terrain Type Data")]
public class TerrainTypeData : ScriptableObject
{
    [Header("ID")]
    [SerializeField] private TerrainType terrainType;

    [Header("Gameplay")]
    [SerializeField] private int movementCost = 1;
    [SerializeField] private bool isWalkable = true;

    [Header("Visual")]
    [SerializeField] private Color tileColor = Color.white;

    public TerrainType TerrainType => terrainType;
    public int MovementCost => movementCost;
    public bool IsWalkable => isWalkable;
    public Color TileColor => tileColor;
}