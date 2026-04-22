using System.Collections.Generic;
using UnityEngine;

public class LevelObjectiveRuntimeManager : MonoBehaviour
{
    public static LevelObjectiveRuntimeManager Instance { get; private set; }

    [Header("Runtime State")]
    [SerializeField] private List<ObjectiveLayoutData> activeObjectives = new List<ObjectiveLayoutData>();
    [SerializeField] private int currentRoundCount = 0;
    [SerializeField] private bool objectivesInitialized = false;
    [SerializeField] private bool playerWasSeen = false;
    [SerializeField] private GridUnit firstSeenPlayerUnit;
    [SerializeField] private bool loseWhenSeen = false;

    [Header("Reach Tile Visuals")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ReachTileMarkerData reachTileMarkerData;
    [SerializeField] private Transform reachTileMarkerParent;

    [Header("References")]
    [SerializeField] private BattleStateManager battleStateManager;

    private readonly List<GameObject> spawnedReachMarkers = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void InitializeObjectives(List<ObjectiveLayoutData> objectives)
    {
        activeObjectives = objectives != null
            ? new List<ObjectiveLayoutData>(objectives)
            : new List<ObjectiveLayoutData>();

        currentRoundCount = 0;
        playerWasSeen = false;
        firstSeenPlayerUnit = null;
        objectivesInitialized = true;

        RebuildReachTileMarkers();

        Debug.Log($"LevelObjectiveRuntimeManager: Initialized {activeObjectives.Count} objective(s).");
    }

    public void ResetObjectives()
    {
        activeObjectives.Clear();
        currentRoundCount = 0;
        playerWasSeen = false;
        firstSeenPlayerUnit = null;
        objectivesInitialized = false;
        ClearReachTileMarkers();
    }

    public static void NotifyPlayerSeen(GridUnit playerUnit)
    {
        if (Instance != null)
            Instance.MarkPlayerSeen(playerUnit);
    }

    public static void NotifyPlayerSeenIfNotHidden(GridUnit playerUnit)
    {
        if (playerUnit == null || playerUnit.Team != UnitTeam.Player || playerUnit.IsDead)
            return;

        HiddenStateComponent hiddenState = playerUnit.GetComponent<HiddenStateComponent>();
        if (hiddenState != null && hiddenState.IsHidden)
            return;

        NotifyPlayerSeen(playerUnit);
    }

    public static void RefreshPlayerSeenState()
    {
        if (Instance != null)
            Instance.TryClearPlayerSeenState();
    }

    public void SetLoseWhenSeen(bool isEnabled)
    {
        loseWhenSeen = isEnabled;
        Debug.Log($"Lose When Seen set to: {loseWhenSeen}");

        if (loseWhenSeen && playerWasSeen && battleStateManager != null && !battleStateManager.BattleEnded)
            battleStateManager.EndBattleExternally("You Lose");
    }

    private void MarkPlayerSeen(GridUnit playerUnit)
    {
        if (!objectivesInitialized || playerWasSeen || playerUnit == null)
            return;

        playerWasSeen = true;
        firstSeenPlayerUnit = playerUnit;

        Debug.Log($"Reach Without Being Seen failed: {playerUnit.name} was seen.");

        if (loseWhenSeen && battleStateManager != null && !battleStateManager.BattleEnded)
            battleStateManager.EndBattleExternally("You Lose");
    }

    private void TryClearPlayerSeenState()
    {
        if (!objectivesInitialized || loseWhenSeen || !playerWasSeen)
            return;

        if (IsAnyEnemyStillTrackingOrSeeingPlayer())
            return;

        playerWasSeen = false;
        firstSeenPlayerUnit = null;
        Debug.Log("Reach Without Being Seen restored: enemies lost the player.");
    }

    private bool IsAnyEnemyStillTrackingOrSeeingPlayer()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null)
                continue;

            GridUnit enemyUnit = enemy.GetComponent<GridUnit>();
            if (enemyUnit == null || enemyUnit.IsDead)
                continue;

            if (enemy.HasActiveTargetKnowledge())
                return true;
        }

        EnemyVisionDetector[] detectors = FindObjectsByType<EnemyVisionDetector>(FindObjectsSortMode.None);
        GridUnit[] units = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (EnemyVisionDetector detector in detectors)
        {
            if (detector == null)
                continue;

            GridUnit enemyUnit = detector.OwnerUnit;
            if (enemyUnit == null || enemyUnit.IsDead)
                continue;

            foreach (GridUnit unit in units)
            {
                if (unit == null || unit.Team != UnitTeam.Player || unit.IsDead)
                    continue;

                HiddenStateComponent hiddenState = unit.GetComponent<HiddenStateComponent>();
                if (hiddenState != null && hiddenState.IsHidden)
                    continue;

                if (detector.CanSeeUnit(unit))
                    return true;
            }
        }

        return false;
    }

    public void OnPlayerTurnStarted()
    {
        if (!objectivesInitialized)
            return;

        currentRoundCount++;
        EvaluateBattleObjectives();
    }

    public void OnPlayerTurnEnded()
    {
        if (!objectivesInitialized)
            return;

        EvaluateBattleObjectives();
    }

    public void OnUnitDied(GridUnit deadUnit)
    {
        if (!objectivesInitialized)
            return;

        EvaluateBattleObjectives();
    }

    public void EvaluateObjectives()
    {
        if (!objectivesInitialized || battleStateManager == null || battleStateManager.BattleEnded)
            return;

        EvaluateBattleObjectives();
    }

    private void EvaluateBattleObjectives()
    {
        if (!objectivesInitialized || battleStateManager == null || battleStateManager.BattleEnded)
            return;

        if (AreAllEnemiesDefeated())
            ClearPlayerSeenState();

        if (!HasAnyPlayerUnitsAlive())
        {
            battleStateManager.EndBattleExternally("You Lose");
            return;
        }

        if (IsAnyReachObjectiveImpossibleBecausePlayersDied())
        {
            battleStateManager.EndBattleExternally("You Lose");
            return;
        }

        if (!AreAllRequiredWinObjectivesComplete())
            return;

        battleStateManager.EndBattleExternally("You Win");
    }

    private bool AreAllRequiredWinObjectivesComplete()
    {
        bool hasAnyWinObjective = false;

        foreach (ObjectiveLayoutData objective in activeObjectives)
        {
            if (objective == null)
                continue;

            if (!IsWinObjective(objective.winConditionType))
                continue;

            hasAnyWinObjective = true;

            if (!IsObjectiveComplete(objective))
                return false;
        }

        return hasAnyWinObjective;
    }

    private bool IsWinObjective(WinConditionType type)
    {
        return type == WinConditionType.KillAllEnemies ||
               type == WinConditionType.SurviveTurns ||
               type == WinConditionType.ReachTile ||
               type == WinConditionType.ReachWithoutBeingSeen ||
               type == WinConditionType.InteractWithObject;
    }

    private bool IsObjectiveComplete(ObjectiveLayoutData objective)
    {
        if (objective == null)
            return false;

        switch (objective.winConditionType)
        {
            case WinConditionType.KillAllEnemies:
                return AreAllEnemiesDefeated();

            case WinConditionType.SurviveTurns:
                return currentRoundCount >= Mathf.Max(1, objective.surviveTurnCount);

            case WinConditionType.ReachTile:
                return IsReachObjectiveComplete(objective);

            case WinConditionType.ReachWithoutBeingSeen:
                if (playerWasSeen)
                    return false;

                return IsReachObjectiveComplete(objective);

            case WinConditionType.InteractWithObject:
                return false;

            default:
                return false;
        }
    }

    private bool IsReachObjectiveComplete(ObjectiveLayoutData objective)
    {
        List<Vector2Int> targetZones = GetObjectiveTargetZones(objective);
        return targetZones.Count > 0 && AreAllRequiredReachZonesOccupied(targetZones);
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

    private bool IsAnyReachObjectiveImpossibleBecausePlayersDied()
    {
        foreach (ObjectiveLayoutData objective in activeObjectives)
        {
            if (objective == null)
                continue;

            bool isReachObjective =
                objective.winConditionType == WinConditionType.ReachTile ||
                objective.winConditionType == WinConditionType.ReachWithoutBeingSeen;

            if (!isReachObjective)
                continue;

            List<Vector2Int> targetZones = GetObjectiveTargetZones(objective);
            if (targetZones.Count == 0)
                continue;

            if (GetLivingPlayerCount() < GetUniqueTargetZoneCount(targetZones))
                return true;
        }

        return false;
    }

    private int GetLivingPlayerCount()
    {
        int count = 0;
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy)
                continue;

            if (unit.Team == UnitTeam.Player && !unit.IsDead && unit.CurrentTile != null)
                count++;
        }

        return count;
    }

    private int GetUniqueTargetZoneCount(List<Vector2Int> targetZones)
    {
        if (targetZones == null)
            return 0;

        HashSet<Vector2Int> uniqueZones = new HashSet<Vector2Int>(targetZones);
        return uniqueZones.Count;
    }

    private bool AreAllRequiredReachZonesOccupied(List<Vector2Int> targetZones)
    {
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);
        List<GridUnit> livingPlayers = new List<GridUnit>();
        HashSet<Vector2Int> requiredZones = new HashSet<Vector2Int>(targetZones);

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

        if (requiredZones.Count == 0 || livingPlayers.Count != requiredZones.Count)
            return false;

        HashSet<Vector2Int> occupiedTargetZones = new HashSet<Vector2Int>();

        foreach (GridUnit unit in livingPlayers)
        {
            Vector2Int unitPos = new Vector2Int(unit.CurrentTile.X, unit.CurrentTile.Y);

            if (!requiredZones.Contains(unitPos))
                return false;

            if (occupiedTargetZones.Contains(unitPos))
                return false;

            occupiedTargetZones.Add(unitPos);
        }

        return occupiedTargetZones.Count == requiredZones.Count;
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
            if (objective == null ||
                (objective.winConditionType != WinConditionType.ReachTile &&
                 objective.winConditionType != WinConditionType.ReachWithoutBeingSeen))
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

    private void ClearPlayerSeenState()
    {
        if (!playerWasSeen)
            return;

        playerWasSeen = false;
        firstSeenPlayerUnit = null;
        Debug.Log("Reach Without Being Seen restored: all enemies are defeated.");
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
