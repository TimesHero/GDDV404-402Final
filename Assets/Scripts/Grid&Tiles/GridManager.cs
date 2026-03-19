using UnityEngine;

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
                Vector3 worldPosition = new Vector3(x * tileSpacing, 0, y * tileSpacing);
                GridTile tile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, tileParent);
                tile.Initialize(x, y);
                grid[x, y] = tile;
            }
        }
    }

    public bool isInsideGrid(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < widht && gridPos.y >= 0 && gridPos.y < height;
    }
    
    public GridTile GetTileAt(Vector2Int gridPos)
    {
        if (!isInsideGrid(gridPos)) return null;
        
        return grid[gridPos.x, gridPos.y];
    }
    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSpacing, 0f, gridPos.y * tileSpacing);
    }
}
