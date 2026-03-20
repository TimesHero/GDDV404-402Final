using UnityEngine;
using System.Collections.Generic;

public class AStarPathFinder : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    public List<GridTile> FindPath(GridTile startTile, GridTile targetTile)
    {
        if (startTile == null || targetTile == null)
        return null;
        
        if(!targetTile.isWalkable)
            return null;

        Dictionary<GridTile, PathNode> allNodes = CreateNodeMap();
        
        PathNode startNode = allNodes[startTile];
        PathNode targetNode = allNodes[targetTile];

        List<PathNode> openList = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();
        
        startNode.GCost = 0;
        startNode.HCost = CalculateHeuristic(startTile, targetTile);
        
        openList.Add(startNode);
        
        while (openList.Count > 0)
        {
            PathNode currentNode = GetLowestFCostNode(openList);
            
            if (currentNode == targetNode)
                return RetracePath(startNode, targetNode);
            
            openList.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (GridTile neighborTile in gridManager.GetNeighbors(currentNode.Tile))
            {
                if (neighborTile == null)
                    continue;
                if (!neighborTile.isWalkable)
                    continue;
                if (neighborTile.isOccupied && neighborTile != targetTile)
                    continue;
                
                PathNode neighborNode = allNodes[neighborTile];
                
                if (closedSet.Contains(neighborNode))
                    continue;
                
                int tentativeGCost = currentNode.GCost + neighborTile.movementCost;
                
                if (tentativeGCost < neighborNode.GCost)
                {
                    neighborNode.Parent = currentNode;
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.HCost = CalculateHeuristic(neighborTile, targetTile);
                    
                    
                    if (!openList.Contains(neighborNode))
                        openList.Add(neighborNode);
                }
            }
        }
        return null;
    }
    
    private Dictionary<GridTile, PathNode> CreateNodeMap()
    {
        Dictionary<GridTile, PathNode> nodeMap = new Dictionary<GridTile, PathNode>();
        
        GridTile[,] grid = gridManager.Grid;

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                GridTile tile = grid[x, y];

                if (tile == null)
                {
                    continue;
                }
                
                nodeMap[tile] = new PathNode(tile);
            }
        }
        return nodeMap;
    }

    private int CalculateHeuristic(GridTile fromTile, GridTile toTile)
    {
        Vector2Int from = fromTile.GridPosition;
        Vector2Int to = toTile.GridPosition;
        
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }

    private PathNode GetLowestFCostNode(List<PathNode> nodeList)
    {
        PathNode lowestNode = nodeList[0];

        for (int i = 1; i < nodeList.Count; i++)
        {
            PathNode currentNode = nodeList[i];
            
            if (currentNode.FCost < lowestNode.FCost)
                lowestNode = currentNode;
            else if (currentNode.FCost == lowestNode.FCost)
            {
                if (currentNode.HCost < lowestNode.HCost)
                    lowestNode = currentNode;
            }
        }
        return lowestNode;
    }
    
    private List<GridTile> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<GridTile> path = new List<GridTile>();
        PathNode currentNode = endNode;
        
        while (currentNode != null && currentNode != startNode)
        {
            path.Add(currentNode.Tile);
            currentNode = currentNode.Parent;
        }
        path.Add(startNode.Tile);
        path.Reverse();
        return path;
    }

}
