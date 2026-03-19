using UnityEngine;

public enum TerrainType
{
    Ground,
    Forest,
    Water,
    Hazard,
    Blocked
}

public class GridTile : MonoBehaviour
{
    [Header("Grid Coordinates")] 
    public int X;
    public int Y;
    
    [Header("Tile Data")]
    public bool isWalkable = true;
    public int movementCost = 1;
    public TerrainType terrainType = TerrainType.Ground;

    [Header("Occupancy")] 
    public bool isOccupied;
    public GameObject occupyingUnit;
    public Vector2Int GridPosition => new Vector2Int(X, Y);

    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;
        gameObject.name = $"Tile_{x}_{y}";
    }
    
    public void SetOccupant(GameObject unit)
    {
        occupyingUnit = unit;
        isOccupied = unit != null;
    }
}
