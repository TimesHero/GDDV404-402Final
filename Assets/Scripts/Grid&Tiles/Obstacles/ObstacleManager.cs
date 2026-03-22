using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform obstacleParent;

    private readonly List<GameObject> spawnedObstacles = new List<GameObject>();

    public bool TryPlaceObstacle(ObstacleData obstacleData, Vector2Int origin)
    {
        if (gridManager == null || obstacleData == null)
            return false;

        if (!CanPlaceObstacle(obstacleData, origin))
            return false;

        // Marca tiles bloqueados
        for (int x = 0; x < obstacleData.FootprintSize.x; x++)
        {
            for (int y = 0; y < obstacleData.FootprintSize.y; y++)
            {
                Vector2Int tilePos = new Vector2Int(origin.x + x, origin.y + y);
                GridTile tile = gridManager.GetTileAt(tilePos);

                if (tile == null)
                    continue;

                if (obstacleData.BlocksMovement)
                    tile.ForceSetWalkable(false);
            }
        }

        // Instancia un solo prefab visual
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

            spawnedObstacles.Add(obstacleInstance);
        }

        return true;
    }

    public bool CanPlaceObstacle(ObstacleData obstacleData, Vector2Int origin)
    {
        if (gridManager == null || obstacleData == null)
            return false;

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

                if (!tile.isWalkable || tile.isOccupied)
                    return false;
            }
        }

        return true;
    }

    private Vector3 GetObstacleCenterWorldPosition(ObstacleData obstacleData, Vector2Int origin)
    {
        Vector3 originWorld = gridManager.GetWorldPosition(origin);

        float offsetX = (obstacleData.FootprintSize.x - 1) * 0.5f;
        float offsetZ = (obstacleData.FootprintSize.y - 1) * 0.5f;

        return originWorld + new Vector3(offsetX, 0f, offsetZ);
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