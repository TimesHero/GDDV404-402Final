using UnityEngine;
using System.Collections.Generic;

public class AStarPathFinder : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private InteractablePlacementService interactablePlacementService;

    private void Awake()
    {
        if (interactablePlacementService == null)
            interactablePlacementService = FindFirstObjectByType<InteractablePlacementService>();
    }

    public List<GridTile> FindPath(GridTile startTile, GridTile targetTile, GridUnit unit, bool allowInteractableTarget = false)
    {
        if (startTile == null || targetTile == null || unit == null)
            return null;
        
        if (!targetTile.isWalkable)
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

                if (IsTileOccupiedByOtherUnit(neighborTile, unit))
                    continue;

                if (ShouldTreatTileAsBlockedByInteractable(neighborTile, targetTile, unit, allowInteractableTarget))
                    continue;

                if (!CanTraverseElevation(currentNode.Tile, neighborTile, unit))
                    continue;
                
                PathNode neighborNode = allNodes[neighborTile];
                
                if (closedSet.Contains(neighborNode))
                    continue;
                
                bool isFinalDestination = neighborTile == targetTile;
                int traversalCost = neighborTile.GetTraversalCost(isFinalDestination);

                int tentativeGCost = currentNode.GCost + traversalCost;
                
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

    private bool IsTileOccupiedByOtherUnit(GridTile tile, GridUnit unit)
    {
        if (tile == null || unit == null || !tile.isOccupied)
            return false;

        if (tile.OccupyingUnit == null)
            return true;

        return tile.OccupyingUnit != unit.gameObject;
    }

    private bool ShouldTreatTileAsBlockedByInteractable(GridTile tile, GridTile targetTile, GridUnit unit, bool allowInteractableTarget)
    {
        if (tile == null || unit == null || interactablePlacementService == null)
            return false;

        PlacedInteractable placedInteractable = interactablePlacementService.GetPlacedInteractableAtTile(tile);
        if (placedInteractable == null)
            return false;

        if (allowInteractableTarget && tile == targetTile)
            return false;

        BarrelInteractable barrel = placedInteractable.GetComponent<BarrelInteractable>();
        if (barrel == null)
            return false;

        bool playerCanEnterThisBarrel =
            unit.Team == UnitTeam.Player &&
            tile == targetTile &&
            barrel.CanUnitHideHere(unit);

        return !playerCanEnterThisBarrel;
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

    private bool CanTraverseElevation(GridTile fromTile, GridTile toTile, GridUnit unit)
    {
        if (fromTile == null || toTile == null || unit == null)
            return false;

        int fromElevation = GetTileElevation(fromTile);
        int toElevation = GetTileElevation(toTile);

        int climbDelta = toElevation - fromElevation;

        return climbDelta <= unit.MaxClimbHeight;
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
                    continue;
                
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
