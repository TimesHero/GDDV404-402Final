using UnityEngine;
using System.Collections.Generic;

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

        Queue<GridTile> frontier = new Queue<GridTile>();
        reachableTiles[startTile] = 0;
        frontier.Enqueue(startTile);

        while (frontier.Count > 0)
        {
            GridTile currentTile = frontier.Dequeue();
            int currentCost = reachableTiles[currentTile];

            foreach (GridTile neighbor in gridManager.GetNeighbors(currentTile))
            {
                if(neighbor == null)
                    continue;
                if(!neighbor.isWalkable)
                    continue;
                if(neighbor.isOccupied && neighbor != startTile)
                    continue;
                
                int newCost = currentCost + neighbor.movementCost;
                
                if (newCost > movementBudget)
                    continue;

                if (!reachableTiles.ContainsKey(neighbor) || newCost < reachableTiles[neighbor])
                {
                    reachableTiles[neighbor] = newCost;
                    frontier.Enqueue(neighbor);
                }
            }
        }
        return reachableTiles;
    }
}
