using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private GridUnit pendingAttackTarget;
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
        pendingAttackTarget = null;

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
        
        if (controlledUnit.CanAttack(playerUnit))
        {
            Debug.Log("Enemy attacks player.");
            controlledUnit.Attack(playerUnit);
            controlledUnit.MarkAttackedThisTurn();
            LastActionWasMovement = false;
            return true;
        }
        
        if (!controlledUnit.CanMoveThisTurn())
            return false;

        GridTile startTile = controlledUnit.CurrentTile;
        GridTile targetTile = GetClosestAdjacentTile(playerUnit);

        if (startTile == null || targetTile == null)
            return false;

        List<GridTile> fullPath = pathFinder.FindPath(startTile, targetTile);

        if (fullPath == null || fullPath.Count <= 1)
            return false;

        List<GridTile> trimmedPath = TrimPath(fullPath, maxTilesToMovePerTurn);

        // Preparar ataque post-movimiento si las reglas lo permiten
        if (controlledUnit.TurnRules != null && controlledUnit.TurnRules.CanAttackAfterMoving)
            pendingAttackTarget = playerUnit;

        controlledUnit.OnMovementFinished -= HandleMovementFinished;
        controlledUnit.OnMovementFinished += HandleMovementFinished;

        controlledUnit.MarkMovedThisTurn();
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
    
    private void HandleMovementFinished(GridUnit unit)
    {
        controlledUnit.OnMovementFinished -= HandleMovementFinished;

        if (pendingAttackTarget == null)
            return;

        if (controlledUnit.CanAttack(pendingAttackTarget))
        {
            Debug.Log("Enemy attacks after moving.");
            controlledUnit.Attack(pendingAttackTarget);
            controlledUnit.MarkAttackedThisTurn();
        }

        pendingAttackTarget = null;
    }
}