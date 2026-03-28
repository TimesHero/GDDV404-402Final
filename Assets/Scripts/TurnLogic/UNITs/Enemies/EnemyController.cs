using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    
    [SerializeField] private GridManager gridManager;
    [Header("References")]
    [SerializeField] private GridUnit controlledUnit;
    [SerializeField] private AStarPathFinder pathFinder;

    [Header("AI Settings")]
    [SerializeField] private int maxTilesToMovePerTurn = 3;
    
    public bool LastActionWasMovement { get; private set; }

    private void Awake()
    {
        if (controlledUnit == null)
            controlledUnit = GetComponent<GridUnit>();

        if (pathFinder == null)
            pathFinder = FindFirstObjectByType<AStarPathFinder>();
        
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    public bool TryAct(GridUnit playerUnit)
    {
        LastActionWasMovement = false;
        Debug.Log("Enemy TryAct started.");

        if (controlledUnit == null)
        {
            Debug.LogWarning("EnemyController: controlledUnit is null.");
            return false;
        }

        if (pathFinder == null)
        {
            Debug.LogWarning("EnemyController: pathFinder is null.");
            return false;
        }

        if (playerUnit == null)
        {
            Debug.LogWarning("EnemyController: playerUnit is null.");
            return false;
        }

        if (controlledUnit.IsMoving)
        {
            Debug.LogWarning("EnemyController: controlledUnit is already moving.");
            return false;
        }
        
        if (controlledUnit.IsTargetInRange(playerUnit))
        {
            Debug.Log("Enemy attacks player.");
            playerUnit.TakeDamage(controlledUnit.AttackDamage);
            LastActionWasMovement = false;
            return true;
        }

        GridTile startTile = controlledUnit.CurrentTile;
        GridTile targetTile = GetClosestAdjacentTile(playerUnit);

        Debug.Log($"Enemy start tile null: {startTile == null}");
        Debug.Log($"Player target tile null: {targetTile == null}");

        if (startTile == null || targetTile == null)
            return false;

        List<GridTile> fullPath = pathFinder.FindPath(startTile, targetTile);

        Debug.Log($"Full path found: {fullPath != null}");
        Debug.Log($"Full path count: {(fullPath != null ? fullPath.Count : 0)}");

        if (fullPath == null || fullPath.Count <= 1)
        {
            Debug.LogWarning("EnemyController: path is null or too short.");
            return false;
        }

        List<GridTile> trimmedPath = TrimPath(fullPath, maxTilesToMovePerTurn);
        Debug.Log($"Trimmed path count: {trimmedPath.Count}");

        controlledUnit.MoveAlongPath(trimmedPath);
        LastActionWasMovement = true;
        Debug.Log("Enemy movement started.");
        return true;
    }

    private List<GridTile> TrimPath(List<GridTile> fullPath, int maxSteps)
    {
        List<GridTile> result = new List<GridTile>();

        result.Add(fullPath[0]); // start

        int maxIndex = Mathf.Min(maxSteps, fullPath.Count - 1);

        for (int i = 1; i <= maxIndex; i++)
            result.Add(fullPath[i]);

        return result;
    }
    private GridTile GetClosestAdjacentTile(GridUnit playerUnit)
    {
        if (playerUnit == null || playerUnit.CurrentTile == null)
            return null;

        if (controlledUnit == null || controlledUnit.CurrentTile == null)
            return null;

        if (gridManager == null)
        {
            Debug.LogWarning("EnemyController: GridManager not found.");
            return null;
        }

        List<GridTile> neighbors = gridManager.GetNeighbors(playerUnit.CurrentTile);

        GridTile bestTile = null;
        int bestDistance = int.MaxValue;

        foreach (GridTile tile in neighbors)
        {
            if (tile == null)
                continue;

            if (!tile.isWalkable)
                continue;

            if (tile.isOccupied)
                continue;

            int distance = Mathf.Abs(tile.X - controlledUnit.CurrentTile.X) +
                           Mathf.Abs(tile.Y - controlledUnit.CurrentTile.Y);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTile = tile;
            }
        }

        return bestTile;
    }
}