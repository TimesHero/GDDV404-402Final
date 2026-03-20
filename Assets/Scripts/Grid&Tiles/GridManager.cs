using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Size")] 
    [SerializeField] private int widht = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private float tileSpacing = 1f;
    
    [Header("References")]
    [SerializeField] private GridTile tilePrefab;
    [SerializeField] private Transform tileParent;
    
    private GridTile[,] grid;
    
    public int Width => widht;
    public int Height => height;
    public GridTile[,] Grid => grid;
    
    private void Awake()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        grid = new GridTile[widht, height];

        for (int x = 0; x < widht; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPosition = new Vector3(x * tileSpacing, 0f, y * tileSpacing);
                GridTile tile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, tileParent);
                tile.Initialize(x, y);
                grid[x, y] = tile;
            }
        }
    }

    public bool isInsideGrid(Vector2Int gridPos)
    {
        return gridPos.x >= 0 &&
               gridPos.x < widht &&
               gridPos.y >= 0 &&
               gridPos.y < height;
    }
    
    public GridTile GetTileAt(Vector2Int gridPos)
    {
        if (!isInsideGrid(gridPos)) return null;
        
        return grid[gridPos.x, gridPos.y];
    }
    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSpacing,
            0f, gridPos.y * tileSpacing);
    }

    public List<GridTile> GetNeighbors(GridTile tile)
    {
        List<GridTile> neighbors = new List<GridTile>();

        Vector2Int[] directions =
        {
            new Vector2Int(0,1), //Up
            new Vector2Int(1,0), //Right
            new Vector2Int(0,-1), //Down
            new Vector2Int(-1,0) //Left
            
        };
        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighborPos = tile.GridPosition + direction;
            
            if (!isInsideGrid(neighborPos))
                continue;
            
            GridTile neighborTile = GetTileAt(neighborPos);
            
            if (neighborTile == null)
                continue;
            
            neighbors.Add(neighborTile);
        }
        return neighbors;
    }

    public List<GridTile> GetWalkableNeighbors(GridTile tile)
    {
        List<GridTile> neighbors = new List<GridTile>();
        
        foreach (GridTile neighbor in GetNeighbors(tile))
        {
            if (!neighbor.isWalkable)
                continue;
            if (neighbor.isOccupied)
                continue;
            neighbors.Add(neighbor);
        }
        return neighbors;
    }
    
    public bool CanUnitEnterTile(GridTile tile)
    {
        if (tile == null)
            return false;
        if (!tile.isWalkable)
            return false;
        if (tile.isOccupied)
            return false;
        
        return true;
    }
}
