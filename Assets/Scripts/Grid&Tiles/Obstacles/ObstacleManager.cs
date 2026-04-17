using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform obstacleParent;

    private readonly List<PlacedObstacle> placedObstacles = new List<PlacedObstacle>();
    private readonly Dictionary<GridTile, PlacedObstacle> tileToObstacleMap = new Dictionary<GridTile, PlacedObstacle>();

    public Vector3 GetPreviewWorldPosition(ObstacleData obstacleData, Vector2Int origin, int rotationY)
    {
        return GetObstacleCenterWorldPosition(obstacleData, origin, rotationY);
    }
    public bool TryPlaceObstacle(ObstacleData obstacleData, Vector2Int origin, int rotationY)
    {
        if (gridManager == null || obstacleData == null)
            return false;

        if (!CanPlaceObstacle(obstacleData, origin, rotationY))
            return false;

        PlacedObstacle placedObstacle = new PlacedObstacle
        {
            ObstacleData = obstacleData,
            Origin = origin,
            RotationY = rotationY
        };

        List<Vector2Int> rotatedOffsets = GetRotatedFootprintOffsets(obstacleData.FootprintSize, rotationY);

        foreach (Vector2Int offset in rotatedOffsets)
        {
            Vector2Int tilePos = origin + offset;
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

        if (obstacleData.ObstaclePrefab != null)
        {
            Vector3 spawnPosition = GetObstacleCenterWorldPosition(obstacleData, origin, rotationY) 
                                    + obstacleData.GetVisualOffsetForRotation(rotationY);

            Quaternion spawnRotation = Quaternion.Euler(
                obstacleData.GetVisualRotationEulerForRotation(rotationY)
            );

            GameObject obstacleInstance = Instantiate(
                obstacleData.ObstaclePrefab,
                spawnPosition,
                spawnRotation,
                obstacleParent
            );

            obstacleInstance.transform.localScale = obstacleData.GetVisualScaleForRotation(rotationY);
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

    public bool CanPlaceObstacle(ObstacleData obstacleData, Vector2Int origin, int rotationY)
    {
        if (gridManager == null || obstacleData == null)
            return false;

        int? requiredElevation = null;
        List<Vector2Int> rotatedOffsets = GetRotatedFootprintOffsets(obstacleData.FootprintSize, rotationY);

        foreach (Vector2Int offset in rotatedOffsets)
        {
            Vector2Int tilePos = origin + offset;

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

        return true;
    }

    private Vector3 GetObstacleCenterWorldPosition(ObstacleData obstacleData, Vector2Int origin, int rotationY)
    {
        GridTile originTile = gridManager.GetTileAt(origin);
        if (originTile == null)
            return gridManager.GetWorldPosition(origin);

        Vector3 pivot = GetTileTopCenter(originTile);

        Vector3 rotatedAnchorOffset = RotateOffsetY(
            obstacleData.VisualAnchorOffsetFromOrigin,
            rotationY
        );

        return pivot + rotatedAnchorOffset;
    }
    
    private Vector3 GetTileTopCenter(GridTile tile)
    {
        if (tile == null)
            return Vector3.zero;

        Renderer topRenderer = tile.GetTopRenderer();
        if (topRenderer != null)
        {
            return new Vector3(
                topRenderer.bounds.center.x,
                topRenderer.bounds.max.y,
                topRenderer.bounds.center.z
            );
        }

        return tile.transform.position;
    }

    private Vector3 RotateOffsetY(Vector3 offset, int rotationY)
    {
        switch (NormalizeRotationY(rotationY))
        {
            case 90:
                return new Vector3(offset.z, offset.y, -offset.x);
            case 180:
                return new Vector3(-offset.x, offset.y, -offset.z);
            case 270:
                return new Vector3(-offset.z, offset.y, offset.x);
            default:
                return offset;
        }
    }

    private int NormalizeRotationY(int rotationY)
    {
        rotationY %= 360;
        if (rotationY < 0)
            rotationY += 360;

        if (rotationY >= 315 || rotationY < 45) return 0;
        if (rotationY >= 45 && rotationY < 135) return 90;
        if (rotationY >= 135 && rotationY < 225) return 180;
        return 270;
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
    public IReadOnlyList<PlacedObstacle> GetPlacedObstacles()
    {
        return placedObstacles;
    }

    public void ClearAllObstacles()
    {
        List<PlacedObstacle> obstaclesToRemove = new List<PlacedObstacle>(placedObstacles);

        foreach (PlacedObstacle obstacle in obstaclesToRemove)
        {
            RemoveObstacle(obstacle);
        }
    }
    
    public PlacedObstacle GetPlacedObstacleAtTile(Vector2Int tilePosition)
    {
        if (gridManager == null)
            return null;

        GridTile tile = gridManager.GetTileAt(tilePosition);
        if (tile == null)
            return null;

        if (tileToObstacleMap.TryGetValue(tile, out PlacedObstacle placedObstacle))
            return placedObstacle;

        return null;
    }
    
    public bool TrySetObstacleElevationAtTile(Vector2Int tilePosition, int newElevation)
    {
        PlacedObstacle placedObstacle = GetPlacedObstacleAtTile(tilePosition);
        if (placedObstacle == null)
            return false;

        SetObstacleElevation(placedObstacle, newElevation);
        return true;
    }
    
    public void SetObstacleElevation(PlacedObstacle placedObstacle, int newElevation)
    {
        if (placedObstacle == null)
            return;

        foreach (GridTile tile in placedObstacle.OccupiedTiles)
        {
            if (tile == null)
                continue;

            TileElevation tileElevation = tile.GetComponent<TileElevation>();
            if (tileElevation != null)
                tileElevation.SetElevation(newElevation);
        }

        UpdateObstacleInstanceHeight(placedObstacle);
    }
    
    private void UpdateObstacleInstanceHeight(PlacedObstacle placedObstacle)
    {
        if (placedObstacle == null || placedObstacle.Instance == null || placedObstacle.ObstacleData == null)
            return;

        Vector3 worldPos = GetObstacleCenterWorldPosition(
            placedObstacle.ObstacleData,
            placedObstacle.Origin,
            placedObstacle.RotationY
        ) + placedObstacle.ObstacleData.GetVisualOffsetForRotation(placedObstacle.RotationY);

        placedObstacle.Instance.transform.position = worldPos;
        placedObstacle.Instance.transform.rotation = Quaternion.Euler(
            placedObstacle.ObstacleData.GetVisualRotationEulerForRotation(placedObstacle.RotationY)
        );
        placedObstacle.Instance.transform.localScale = placedObstacle.ObstacleData.GetVisualScaleForRotation(placedObstacle.RotationY);
    }
    
    private Vector2Int GetRotatedFootprintSize(Vector2Int originalSize, int rotationY)
    {
        if (rotationY == 90 || rotationY == 270)
            return new Vector2Int(originalSize.y, originalSize.x);

        return originalSize;
    }
    
    private List<Vector2Int> GetRotatedFootprintOffsets(Vector2Int footprintSize, int rotationY)
    {
        List<Vector2Int> offsets = new List<Vector2Int>();

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int offset = new Vector2Int(x, y);

                switch (NormalizeRotationY(rotationY))
                {
                    case 90:
                        offset = new Vector2Int(y, -x);
                        break;
                    case 180:
                        offset = new Vector2Int(-x, -y);
                        break;
                    case 270:
                        offset = new Vector2Int(-y, x);
                        break;
                }

                offsets.Add(offset);
            }
        }

        return offsets;
    }
}