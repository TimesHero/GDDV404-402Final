using System.Collections.Generic;
using UnityEngine;

public class LevelObjectiveRuntimeManager : MonoBehaviour
{
    [Header("Runtime State")]
    [SerializeField] private List<ObjectiveLayoutData> activeObjectives = new List<ObjectiveLayoutData>();
    [SerializeField] private int currentRoundCount = 0;
    [SerializeField] private bool objectivesInitialized = false;

    [Header("Reach Tile Visuals")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ReachTileMarkerData reachTileMarkerData;
    [SerializeField] private Transform reachTileMarkerParent;

    [Header("References")]
    [SerializeField] private BattleStateManager battleStateManager;

    private readonly List<GameObject> spawnedReachMarkers = new List<GameObject>();

    public void InitializeObjectives(List<ObjectiveLayoutData> objectives)
    {
        activeObjectives = objectives != null
            ? new List<ObjectiveLayoutData>(objectives)
            : new List<ObjectiveLayoutData>();

        currentRoundCount = 0;
        objectivesInitialized = true;

        RebuildReachTileMarkers();

        Debug.Log($"LevelObjectiveRuntimeManager: Initialized {activeObjectives.Count} objective(s).");
    }

    public void ResetObjectives()
    {
        activeObjectives.Clear();
        currentRoundCount = 0;
        objectivesInitialized = false;
        ClearReachTileMarkers();
    }

    public void OnPlayerTurnStarted()
    {
        if (!objectivesInitialized)
            return;

        currentRoundCount++;
        EvaluateNonReachObjectives();
    }

    public void OnPlayerTurnEnded()
    {
        if (!objectivesInitialized)
            return;

        EvaluateReachObjectives();
    }

    public void OnUnitDied(GridUnit deadUnit)
    {
        if (!objectivesInitialized)
            return;

        EvaluateNonReachObjectives();
    }

    public void EvaluateObjectives()
    {
        if (!objectivesInitialized || battleStateManager == null || battleStateManager.BattleEnded)
            return;

        EvaluateNonReachObjectives();

        if (battleStateManager.BattleEnded)
            return;

        if (!HasAnyPlayerUnitsAlive())
        {
            battleStateManager.EndBattleExternally("You Lose");
            return;
        }
    }

    private void EvaluateNonReachObjectives()
    {
        if (!objectivesInitialized || battleStateManager == null || battleStateManager.BattleEnded)
            return;

        bool hasAnyWinObjective = false;

        foreach (ObjectiveLayoutData objective in activeObjectives)
        {
            if (objective == null)
                continue;

            switch (objective.winConditionType)
            {
                case WinConditionType.KillAllEnemies:
                    hasAnyWinObjective = true;

                    if (AreAllEnemiesDefeated())
                    {
                        battleStateManager.EndBattleExternally("You Win");
                        return;
                    }
                    break;

                case WinConditionType.SurviveTurns:
                    hasAnyWinObjective = true;

                    if (currentRoundCount >= objective.surviveTurnCount)
                    {
                        battleStateManager.EndBattleExternally("You Win");
                        return;
                    }
                    break;
            }
        }

        if (!HasAnyPlayerUnitsAlive())
        {
            battleStateManager.EndBattleExternally("You Lose");
            return;
        }

        if (!hasAnyWinObjective)
        {
            return;
        }
    }

    private void EvaluateReachObjectives()
    {
        if (!objectivesInitialized || battleStateManager == null || battleStateManager.BattleEnded)
            return;

        foreach (ObjectiveLayoutData objective in activeObjectives)
        {
            if (objective == null)
                continue;

            if (objective.winConditionType != WinConditionType.ReachTile)
                continue;

            List<Vector2Int> targetZones = GetObjectiveTargetZones(objective);
            if (targetZones.Count == 0)
                continue;

            if (AreAllLivingPlayersOnDistinctTargetZones(targetZones))
            {
                battleStateManager.EndBattleExternally("You Win");
                return;
            }
        }
    }

    private bool AreAllEnemiesDefeated()
    {
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy)
                continue;

            if (unit.Team == UnitTeam.Enemy)
                return false;
        }

        return true;
    }

    private bool HasAnyPlayerUnitsAlive()
    {
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy)
                continue;

            if (unit.Team == UnitTeam.Player)
                return true;
        }

        return false;
    }

    private bool AreAllLivingPlayersOnDistinctTargetZones(List<Vector2Int> targetZones)
    {
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);
        List<GridUnit> livingPlayers = new List<GridUnit>();

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy)
                continue;

            if (unit.Team != UnitTeam.Player)
                continue;

            if (unit.CurrentTile == null)
                continue;

            livingPlayers.Add(unit);
        }

        if (livingPlayers.Count == 0)
            return false;

        if (targetZones.Count < livingPlayers.Count)
            return false;

        HashSet<Vector2Int> occupiedTargetZones = new HashSet<Vector2Int>();

        foreach (GridUnit unit in livingPlayers)
        {
            Vector2Int unitPos = new Vector2Int(unit.CurrentTile.X, unit.CurrentTile.Y);

            if (!targetZones.Contains(unitPos))
                return false;

            if (occupiedTargetZones.Contains(unitPos))
                return false;

            occupiedTargetZones.Add(unitPos);
        }

        return occupiedTargetZones.Count == livingPlayers.Count;
    }

    private List<Vector2Int> GetObjectiveTargetZones(ObjectiveLayoutData objective)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        if (objective == null)
            return result;

        if (objective.targetTiles != null && objective.targetTiles.Count > 0)
        {
            foreach (ObjectiveTargetTileData tileData in objective.targetTiles)
            {
                if (tileData == null)
                    continue;

                result.Add(new Vector2Int(tileData.x, tileData.y));
            }
        }
        else
        {
            result.Add(new Vector2Int(objective.targetX, objective.targetY));
        }

        return result;
    }

    private void RebuildReachTileMarkers()
    {
        ClearReachTileMarkers();

        if (reachTileMarkerData == null || reachTileMarkerData.markerPrefab == null || gridManager == null)
            return;

        foreach (ObjectiveLayoutData objective in activeObjectives)
        {
            if (objective == null || objective.winConditionType != WinConditionType.ReachTile)
                continue;

            List<Vector2Int> targetZones = GetObjectiveTargetZones(objective);

            foreach (Vector2Int zone in targetZones)
            {
                GridTile tile = gridManager.GetTileAt(zone);
                if (tile == null)
                    continue;

                Vector3 spawnPosition = GetTileTopCenter(tile) + reachTileMarkerData.localOffset;

                GameObject marker = Instantiate(
                    reachTileMarkerData.markerPrefab,
                    spawnPosition,
                    Quaternion.Euler(reachTileMarkerData.localRotationEuler),
                    reachTileMarkerParent
                );

                ApplyScaleRecursively(marker.transform, reachTileMarkerData.localScale);

                spawnedReachMarkers.Add(marker);
            }
        }
    }

    private void ClearReachTileMarkers()
    {
        for (int i = spawnedReachMarkers.Count - 1; i >= 0; i--)
        {
            if (spawnedReachMarkers[i] != null)
                Destroy(spawnedReachMarkers[i]);
        }

        spawnedReachMarkers.Clear();
    }

    private Vector3 GetTileTopCenter(GridTile tile)
    {
        if (tile == null)
            return Vector3.zero;

        Renderer topRenderer = tile.GetTopRenderer();
        if (topRenderer != null)
            return topRenderer.bounds.center + Vector3.up * topRenderer.bounds.extents.y;

        return tile.transform.position;
    }
    
    private void ApplyScaleRecursively(Transform root, Vector3 scaleMultiplier)
    {
        if (root == null)
            return;

        root.localScale = Vector3.Scale(root.localScale, scaleMultiplier);

        for (int i = 0; i < root.childCount; i++)
        {
            ApplyScaleRecursively(root.GetChild(i), scaleMultiplier);
        }
    }
}