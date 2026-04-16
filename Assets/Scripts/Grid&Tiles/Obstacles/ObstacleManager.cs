using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform obstacleParent;

    private readonly List<PlacedObstacle> placedObstacles = new List<PlacedObstacle>();
    private readonly Dictionary<GridTile, PlacedObstacle> tileToObstacleMap = new Dictionary<GridTile, PlacedObstacle>();

    public bool TryPlaceObstacle(ObstacleData obstacleData, Vector2Int origin)
    {
        if (gridManager == null || obstacleData == null)
            return false;

        if (!CanPlaceObstacle(obstacleData, origin))
            return false;

        PlacedObstacle placedObstacle = new PlacedObstacle
        {
            ObstacleData = obstacleData,
            Origin = origin
        };
        
        for (int x = 0; x < obstacleData.FootprintSize.x; x++)
        {
            for (int y = 0; y < obstacleData.FootprintSize.y; y++)
            {
                Vector2Int tilePos = new Vector2Int(origin.x + x, origin.y + y);
                GridTile tile = gridManager.GetTileAt(tilePos);

                if (tile == null)
                    continue;

                placedObstacle.OccupiedTiles.Add(tile);
                tileToObstacleMap[tile] = placedObstacle;

                if (obstacleData.PaintTerrainUnderObstacle)
                {
                    tile.TerrainType = obstacleData.TerrainTypeUnderObstacle;
                    tile.ApplyTerrainSettings();
                }

                if (obstacleData.BlocksMovement)
                    tile.ForceSetWalkable(false);
            }
        }

        if (obstacleData.ObstaclePrefab != null)
        {
            Vector3 spawnPosition = GetObstacleCenterWorldPosition(obstacleData, origin) + obstacleData.VisualOffset;
            Quaternion spawnRotation = Quaternion.Euler(obstacleData.VisualRotationEuler);

            GameObject obstacleInstance = Instantiate(
                obstacleData.ObstaclePrefab,
                spawnPosition,
                spawnRotation,
                obstacleParent
            );

            obstacleInstance.transform.localScale = obstacleData.VisualScale;
            placedObstacle.Instance = obstacleInstance;
        }

        placedObstacles.Add(placedObstacle);
        return true;
    }

    public bool TryRemoveObstacleAtTile(Vector2Int tilePosition)
    {
        if (gridManager == null)
            return false;

        GridTile tile = gridManager.GetTileAt(tilePosition);

        if (tile == null)
            return false;

        if (!tileToObstacleMap.TryGetValue(tile, out PlacedObstacle placedObstacle))
            return false;

        RemoveObstacle(placedObstacle);
        return true;
    }

    private void RemoveObstacle(PlacedObstacle placedObstacle)
    {
        if (placedObstacle == null)
            return;

        foreach (GridTile tile in placedObstacle.OccupiedTiles)
        {
            if (tile == null)
                continue;

            tileToObstacleMap.Remove(tile);
            
            tile.ForceSetWalkable(true);
            
            tile.TerrainType = TerrainType.Ground;
            tile.ApplyTerrainSettings();
        }

        if (placedObstacle.Instance != null)
            Destroy(placedObstacle.Instance);

        placedObstacles.Remove(placedObstacle);
    }

    public bool CanPlaceObstacle(ObstacleData obstacleData, Vector2Int origin)
    {
        if (gridManager == null || obstacleData == null)
            return false;

        int? requiredElevation = null;

        for (int x = 0; x < obstacleData.FootprintSize.x; x++)
        {
            for (int y = 0; y < obstacleData.FootprintSize.y; y++)
            {
                Vector2Int tilePos = new Vector2Int(origin.x + x, origin.y + y);

                if (!gridManager.isInsideGrid(tilePos))
                    return false;

                GridTile tile = gridManager.GetTileAt(tilePos);

                if (tile == null)
                    return false;

                if (tileToObstacleMap.ContainsKey(tile))
                    return false;

                if (!tile.isWalkable || tile.isOccupied)
                    return false;

                int tileElevation = GetTileElevation(tile);

                if (requiredElevation == null)
                    requiredElevation = tileElevation;
                else if (tileElevation != requiredElevation.Value)
                    return false;
            }
        }

        return true;
    }

    private Vector3 GetObstacleCenterWorldPosition(ObstacleData obstacleData, Vector2Int origin)
    {
        GridTile originTile = gridManager.GetTileAt(origin);
        Vector3 originWorld = gridManager.GetWorldPosition(origin);

        float offsetX = (obstacleData.FootprintSize.x - 1) * 0.5f;
        float offsetZ = (obstacleData.FootprintSize.y - 1) * 0.5f;

        float y = originWorld.y;

        if (originTile != null)
        {
            Renderer topRenderer = originTile.GetComponent<GridTile>() != null
                ? originTile.GetComponent<GridTile>().GetTopRenderer()
                : null;

            if (topRenderer != null)
                y = topRenderer.bounds.max.y;
        }

        return new Vector3(
            originWorld.x + offsetX,
            y,
            originWorld.z + offsetZ
        );
    }
    
    private int GetTileElevation(GridTile tile)
    {
        if (tile == null)
            return 0;

        TileElevation tileElevation = tile.GetComponent<TileElevation>();
        if (tileElevation == null)
            return 0;

        return tileElevation.Elevation;
    }

    /*// ////Test//// ////
    [SerializeField] private ObstacleData testObstacle;
    [SerializeField] private Vector2Int testOrigin = new Vector2Int(3, 3);
    
    private void Start()
    {
        if (testObstacle != null)
            TryPlaceObstacle(testObstacle, testOrigin);
    }
    // // //////////////*/
}