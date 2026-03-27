using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridUnit controlledUnit;
    [SerializeField] private AStarPathFinder pathFinder;

    [Header("AI Settings")]
    [SerializeField] private int maxTilesToMovePerTurn = 3;

    private void Awake()
    {
        if (controlledUnit == null)
            controlledUnit = GetComponent<GridUnit>();

        if (pathFinder == null)
            pathFinder = FindFirstObjectByType<AStarPathFinder>();
    }

    public bool TryAct(GridUnit playerUnit)
    {
        if (controlledUnit == null || pathFinder == null || playerUnit == null)
            return false;

        if (controlledUnit.IsMoving)
            return false;

        GridTile startTile = controlledUnit.CurrentTile;
        GridTile targetTile = playerUnit.CurrentTile;

        if (startTile == null || targetTile == null)
            return false;

        List<GridTile> fullPath = pathFinder.FindPath(startTile, targetTile);

        if (fullPath == null || fullPath.Count <= 1)
            return false;

        List<GridTile> trimmedPath = TrimPath(fullPath, maxTilesToMovePerTurn);

        controlledUnit.MoveAlongPath(trimmedPath);

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
}