using System.Collections.Generic;
using UnityEngine;

public class GridRangeFinder : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    public Dictionary<GridTile, int> GetReachableTiles(GridTile startTile, int movementBudget)
    {
        Dictionary<GridTile, int> reachableTiles = new Dictionary<GridTile, int>();

        if (gridManager == null)
        {
            Debug.LogError("GridRangeFinder: GridManager reference is missing.");
            return reachableTiles;
        }

        if (startTile == null)
            return reachableTiles;

        List<GridTile> openList = new List<GridTile>();
        Dictionary<GridTile, int> traversalCosts = new Dictionary<GridTile, int>();

        traversalCosts[startTile] = 0;
        reachableTiles[startTile] = 0;
        openList.Add(startTile);

        while (openList.Count > 0)
        {
            GridTile currentTile = GetLowestCostTile(openList, traversalCosts);
            openList.Remove(currentTile);

            int currentCost = traversalCosts[currentTile];

            foreach (GridTile neighbor in gridManager.GetNeighbors(currentTile))
            {
                if (neighbor == null)
                    continue;

                if (!neighbor.isWalkable)
                    continue;

                if (neighbor.isOccupied && neighbor != startTile)
                    continue;
                
                int destinationCost = currentCost + neighbor.GetTraversalCost(true);

                if (destinationCost <= movementBudget)
                {
                    if (!reachableTiles.ContainsKey(neighbor) || destinationCost < reachableTiles[neighbor])
                        reachableTiles[neighbor] = destinationCost;
                }
                
                int traversalCost = currentCost + neighbor.GetTraversalCost(false);

                if (traversalCost > movementBudget)
                    continue;

                if (!traversalCosts.ContainsKey(neighbor))
                {
                    traversalCosts[neighbor] = traversalCost;
                    openList.Add(neighbor);
                }
                else if (traversalCost < traversalCosts[neighbor])
                {
                    traversalCosts[neighbor] = traversalCost;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        return reachableTiles;
    }

    private GridTile GetLowestCostTile(List<GridTile> openList, Dictionary<GridTile, int> costs)
    {
        GridTile lowest = openList[0];

        for (int i = 1; i < openList.Count; i++)
        {
            if (costs[openList[i]] < costs[lowest])
                lowest = openList[i];
        }

        return lowest;
    }
}