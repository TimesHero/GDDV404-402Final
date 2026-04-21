using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedBuilderUnit
{
    public GridUnit Unit;
    public UnitData UnitData;
    public BuilderUnitPaintTeam PaintTeam;
    public Vector2Int Origin;
    public int RotationY;
    public bool UseCardinalFacing;
    public EnemyAIBehavior EnemyBehavior = EnemyAIBehavior.Static;
    public bool HasPatrolRoute;
    public Vector2Int PatrolStart;
    public Vector2Int PatrolEnd;
    public GameObject PatrolEndMarker;
    public Vector2Int FootprintSize = Vector2Int.one;
    public List<GridTile> OccupiedTiles = new List<GridTile>();
}
