using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedBuilderUnit
{
    public GridUnit Unit;
    public UnitData UnitData;
    public Vector2Int Origin;
    public Vector2Int FootprintSize = Vector2Int.one;
    public List<GridTile> OccupiedTiles = new List<GridTile>();
}