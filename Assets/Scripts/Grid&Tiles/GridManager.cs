using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private float tileSpacing = 1f;

    [Header("References")]
    [SerializeField] private GridTile tilePrefab;
    [SerializeField] private Transform tileParent;
    [SerializeField] private TileManager tileManager;

    private GridTile[,] grid;

    public int Width => width;
    public int Height => height;
    public GridTile[,] Grid => grid;
    public float TileSpacing => tileSpacing;

    private void Awake()
    {
        GenerateGrid();
    }

    public void RebuildGrid(int newWidth, int newHeight)
    {
        width = Mathf.Max(1, newWidth);
        height = Mathf.Max(1, newHeight);

        ClearExistingGrid();
        GenerateGrid();

        Debug.Log($"Grid rebuilt to {width} x {height}");
    }

    private void GenerateGrid()
    {
        grid = new GridTile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPosition = new Vector3(x * tileSpacing, 0f, y * tileSpacing);
                GridTile tile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, tileParent);
                tile.Initialize(x, y, tileManager);
                grid[x, y] = tile;
            }
        }
    }

    private void ClearExistingGrid()
    {
        if (tileParent != null)
        {
            for (int i = tileParent.childCount - 1; i >= 0; i--)
            {
                Transform child = tileParent.GetChild(i);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
#endif
                    Destroy(child.gameObject);
            }
        }

        grid = null;
    }

    public bool IsInsideGrid(Vector2Int gridPos)
    {
        return gridPos.x >= 0 &&
               gridPos.x < width &&
               gridPos.y >= 0 &&
               gridPos.y < height;
    }

    public bool isInsideGrid(Vector2Int gridPos)
    {
        return IsInsideGrid(gridPos);
    }

    public GridTile GetTileAt(Vector2Int gridPos)
    {
        if (!IsInsideGrid(gridPos))
            return null;

        return grid[gridPos.x, gridPos.y];
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * tileSpacing,
            0f,
            gridPos.y * tileSpacing
        );
    }

    public List<GridTile> GetNeighbors(GridTile tile)
    {
        List<GridTile> neighbors = new List<GridTile>();

        if (tile == null)
            return neighbors;

        Vector2Int[] directions =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighborPos = tile.GridPosition + direction;

            if (!IsInsideGrid(neighborPos))
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