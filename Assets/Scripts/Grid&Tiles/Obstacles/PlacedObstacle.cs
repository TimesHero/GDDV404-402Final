using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedObstacle
{
    public ObstacleData ObstacleData;
    public GameObject Instance;
    public Vector2Int Origin;
    public List<GridTile> OccupiedTiles = new List<GridTile>();
}