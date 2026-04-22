using System.Collections.Generic;
using UnityEngine;

public class GridRangeFinder : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private InteractablePlacementService interactablePlacementService;

    private void Awake()
    {
        if (interactablePlacementService == null)
            interactablePlacementService = FindFirstObjectByType<InteractablePlacementService>();
    }

    public Dictionary<GridTile, int> GetReachableTiles(GridTile startTile, int movementBudget, GridUnit unit)
    {
        Dictionary<GridTile, int> reachableTiles = new Dictionary<GridTile, int>();

        if (gridManager == null)
        {
            Debug.LogError("GridRangeFinder: GridManager reference is missing.");
            return reachableTiles;
        }

        if (startTile == null || unit == null)
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

                if (neighbor.isOccupied && neighbor != startTile && neighbor.OccupyingUnit != null)
                    continue;

                if (!CanTraverseElevation(currentTile, neighbor, unit))
                    continue;

                BarrelInteractable barrel = GetBarrelOnTile(neighbor);
                bool blocksTraversal = barrel != null && neighbor != startTile;

                int destinationCost = currentCost + neighbor.GetTraversalCost(true);

                if (destinationCost <= movementBudget &&
                    (!blocksTraversal || CanUseBarrelAsDestination(unit, barrel)))
                {
                    if (!reachableTiles.ContainsKey(neighbor) || destinationCost < reachableTiles[neighbor])
                        reachableTiles[neighbor] = destinationCost;
                }

                if (blocksTraversal)
                    continue;
                
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

    private BarrelInteractable GetBarrelOnTile(GridTile tile)
    {
        if (tile == null)
            return null;

        if (interactablePlacementService == null)
            interactablePlacementService = FindFirstObjectByType<InteractablePlacementService>();

        if (interactablePlacementService == null)
            return null;

        PlacedInteractable placedInteractable = interactablePlacementService.GetPlacedInteractableAtTile(tile);
        return placedInteractable != null
            ? placedInteractable.GetComponent<BarrelInteractable>()
            : null;
    }

    private bool CanUseBarrelAsDestination(GridUnit unit, BarrelInteractable barrel)
    {
        return unit != null &&
               unit.Team == UnitTeam.Player &&
               barrel != null &&
               barrel.CanUnitHideHere(unit);
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
