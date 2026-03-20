using UnityEngine;

public class PathNode 
{
    public GridTile Tile { get; private set;}
    
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    
    public PathNode Parent { get; set; }
    
    public PathNode(GridTile tile)
    {
        Tile = tile;
        GCost = int.MaxValue;
        HCost = 0;
        Parent = null;
    }
}
