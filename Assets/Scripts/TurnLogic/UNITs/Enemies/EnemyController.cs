using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyVisionDetector))]
public class EnemyController : MonoBehaviour
{
    private GridUnit pendingAttackTarget;
    private BarrelInteractable pendingBarrelTarget;
    private GridUnit pendingPushTarget;
    private bool pendingInvestigationScanAfterMove;

    [SerializeField] private GridManager gridManager;

    [Header("References")]
    [SerializeField] private GridUnit controlledUnit;
    [SerializeField] private AStarPathFinder pathFinder;
    [SerializeField] private InteractablePlacementService interactablePlacementService;
    [SerializeField] private EnemyStateFeedbackController stateFeedbackController;

    [Header("State Machine")]
    [SerializeField] private EnemyAIState defaultState = EnemyAIState.Idle;
    [SerializeField] private EnemyAIState currentState = EnemyAIState.Idle;
    [SerializeField] private EnemyAIBehavior configuredBehavior = EnemyAIBehavior.Static;
    [SerializeField] private bool hasPatrolRoute;
    [SerializeField] private Vector2Int patrolStartGridPosition;
    [SerializeField] private Vector2Int patrolEndGridPosition;
    [SerializeField] private bool logStateChanges = true;

    [Header("Home Post")]
    [SerializeField] private bool hasHomePost;
    [SerializeField] private Vector2Int homeGridPosition;
    [SerializeField] private Quaternion homeVisualRotation = Quaternion.identity;

    [Header("Investigation")]
    [SerializeField] private GridUnit rememberedTargetUnit;
    [SerializeField] private GridTile lastKnownTargetTile;
    [SerializeField] private List<Vector2Int> observedBarrelPositions = new List<Vector2Int>();
    [SerializeField] private List<Vector2Int> rememberedBarrelLayoutPositions = new List<Vector2Int>();
    [SerializeField] private List<Vector2Int> observedBarrelLayoutTilePositions = new List<Vector2Int>();
    [SerializeField] private List<Vector2Int> suspiciousBarrelPositions = new List<Vector2Int>();
    [SerializeField] private int lookDirectionIndex;
    [SerializeField] private float investigationLookPause = 0.45f;
    [SerializeField] private Vector3 lastSeenMovementDirection = Vector3.zero;
    [SerializeField] private bool prioritizeLastKnownBarrelMovement;
    [SerializeField, Min(1)] private int maxBarrelsToSearchInOneTurn = 8;
    [SerializeField, Min(1f)] private float barrelSearchMovementTimeout = 8f;
    [SerializeField] private bool hasBarrelLayoutMemory;
    [SerializeField] private bool hasSuspiciousInvestigationPosition;
    [SerializeField] private bool investigatingMissingBarrelLayoutChange;
    [SerializeField, Min(1)] private int maxFailedInvestigationAttempts = 2;
    private bool barrelSearchInterruptedByMovingTarget;
    private int failedInvestigationAttempts;
    private bool patrolHeadingToEnd = true;
    private bool pendingPatrolDestinationFlip;
    private bool shouldReturnToPost;

    public bool LastActionWasMovement { get; private set; }
    public bool IsInvestigationScanRunning { get; private set; }
    public bool IsActionAnimationRunning { get; private set; }
    public EnemyAIState CurrentState => currentState;

    public void ForceClearActionAnimationWait()
    {
        IsActionAnimationRunning = false;
    }

    private struct EnemyPushPlan
    {
        public GridTile StandTile;
        public GridTile DestinationTile;
        public int Score;
        public bool IsImmediate;
    }

    private void Awake()
    {
        if (controlledUnit == null)
            controlledUnit = GetComponent<GridUnit>();

        if (pathFinder == null)
            pathFinder = FindFirstObjectByType<AStarPathFinder>();
        
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (interactablePlacementService == null)
            interactablePlacementService = FindFirstObjectByType<InteractablePlacementService>();

        if (stateFeedbackController == null)
            stateFeedbackController = GetComponent<EnemyStateFeedbackController>();

        if (GetComponent<EnemyVisionDetector>() == null)
            Debug.LogError($"{name} is missing EnemyVisionDetector. Add it on the prefab before play.");

        currentState = GetCalmState();
    }

    public void ConfigureBehavior(EnemyAIBehavior behavior, bool patrolRouteEnabled, Vector2Int patrolStart, Vector2Int patrolEnd)
    {
        configuredBehavior = behavior;
        hasPatrolRoute = behavior == EnemyAIBehavior.Patrol && patrolRouteEnabled && patrolStart != patrolEnd;
        patrolStartGridPosition = patrolStart;
        patrolEndGridPosition = patrolEnd;
        patrolHeadingToEnd = true;

        defaultState = hasPatrolRoute ? EnemyAIState.Patrol : EnemyAIState.Idle;
        CaptureHomePost();
        SetState(GetCalmState());
    }

    public bool TryTakeTurn(GridUnit visibleTarget)
    {
        if (!hasHomePost)
            CaptureHomePost();

        RefreshBarrelLayoutMemory(true);

        EnemyAIState nextState = ResolveTurnState(visibleTarget);
        SetState(nextState);

        switch (currentState)
        {
            case EnemyAIState.Combat:
                if (visibleTarget == null)
                {
                    SetState(EnemyAIState.Investigate);
                    return TryInvestigate();
                }

                return TryAct(visibleTarget);

            case EnemyAIState.SearchBarrels:
                BarrelInteractable barrelTarget = GetPriorityVisibleBarrelTarget();
                if (barrelTarget == null)
                {
                    SetState(EnemyAIState.Investigate);
                    return TryInvestigate();
                }

                return TrySearchVisibleBarrels(barrelTarget);

            case EnemyAIState.Investigate:
                return TryInvestigate();

            case EnemyAIState.Alert:
                if (HasInvestigationPointTarget())
                {
                    SetState(EnemyAIState.Investigate);
                    return TryInvestigate();
                }

                SetState(GetCalmState());
                return false;

            case EnemyAIState.Patrol:
                return TryPatrol();

            case EnemyAIState.ReturnToPost:
                return TryReturnToPost();

            case EnemyAIState.Idle:
                if (configuredBehavior == EnemyAIBehavior.RandomLook)
                    return TryRandomLook();

                return false;

            default:
                return false;
        }
    }

    private EnemyAIState ResolveTurnState(GridUnit visibleTarget)
    {
        if (visibleTarget != null)
            return EnemyAIState.Combat;

        if (GetPriorityVisibleBarrelTarget() != null)
            return EnemyAIState.SearchBarrels;

        if (HasInvestigationPointTarget())
            return EnemyAIState.Investigate;

        if (shouldReturnToPost)
            return EnemyAIState.ReturnToPost;

        return GetCalmState();
    }

    private EnemyAIState GetCalmState()
    {
        if (configuredBehavior == EnemyAIBehavior.Patrol && hasPatrolRoute)
            return EnemyAIState.Patrol;

        if (defaultState == EnemyAIState.Patrol && hasPatrolRoute)
            return EnemyAIState.Patrol;

        return EnemyAIState.Idle;
    }

    private void CaptureHomePost()
    {
        if (controlledUnit == null || controlledUnit.CurrentTile == null)
            return;

        homeGridPosition = controlledUnit.CurrentTile.GridPosition;
        homeVisualRotation = controlledUnit.GetVisualRotation();
        hasHomePost = true;
    }

    private bool ShouldUseHomePostReturn()
    {
        if (!hasHomePost || controlledUnit == null || controlledUnit.CurrentTile == null)
            return false;

        return configuredBehavior != EnemyAIBehavior.Patrol && !hasPatrolRoute;
    }

    private GridTile GetHomeTile()
    {
        if (!hasHomePost || gridManager == null)
            return null;

        return gridManager.GetTileAt(homeGridPosition);
    }

    private bool IsAtHomePost()
    {
        return controlledUnit != null &&
               controlledUnit.CurrentTile != null &&
               controlledUnit.CurrentTile.GridPosition == homeGridPosition;
    }

    private bool RequestReturnToPostIfNeeded()
    {
        if (!ShouldUseHomePostReturn())
            return false;

        if (IsAtHomePost())
        {
            controlledUnit.RestoreVisualRotation(homeVisualRotation);
            shouldReturnToPost = false;
            return false;
        }

        shouldReturnToPost = true;
        return true;
    }

    private void FinishReturnToPost()
    {
        shouldReturnToPost = false;

        if (controlledUnit != null && hasHomePost)
            controlledUnit.RestoreVisualRotation(homeVisualRotation);

        SetState(GetCalmState());
    }

    private void SetState(EnemyAIState nextState)
    {
        if (currentState == nextState)
            return;

        EnemyAIState previousState = currentState;
        currentState = nextState;

        if (logStateChanges)
            Debug.Log($"{name} AI State: {previousState} -> {currentState}");

        if (Application.isPlaying && stateFeedbackController != null)
            stateFeedbackController.PlayStateEntered(currentState, controlledUnit != null ? controlledUnit.transform : transform);
    }

    public bool TryAct(GridUnit playerUnit)
    {
        SetState(EnemyAIState.Combat);
        LastActionWasMovement = false;
        pendingAttackTarget = null;
        pendingBarrelTarget = null;
        pendingPushTarget = null;
        pendingInvestigationScanAfterMove = false;

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

        RememberTarget(playerUnit);
        SetState(EnemyAIState.Combat);

        int attackScore = ScoreAttackOption(playerUnit);
        if (TryGetBestPushPlan(playerUnit, out EnemyPushPlan pushPlan) && pushPlan.Score > attackScore)
        {
            if (pushPlan.IsImmediate)
                return TryExecutePush(playerUnit);

            if (TryMoveForPush(playerUnit, pushPlan))
                return true;
        }

        if (controlledUnit.TryAttack(playerUnit))
        {
            Debug.Log("Enemy attacks player.");
            LastActionWasMovement = false;
            return true;
        }
        
        if (!controlledUnit.CanMoveThisTurn())
            return false;

        if (controlledUnit.RemainingMovementPoints <= 0)
        {
            Debug.LogWarning($"{controlledUnit.name} has no movement points.");
            return false;
        }

        if (!TryGetBestApproachPath(playerUnit.CurrentTile, controlledUnit.AttackRange, out List<GridTile> trimmedPath, out bool reachesActionRange))
            return false;

        if (trimmedPath == null || trimmedPath.Count <= 1)
            return false;
        
        if (reachesActionRange && controlledUnit.TurnRules != null && controlledUnit.TurnRules.CanAttackAfterMoving)
            pendingAttackTarget = playerUnit;

        controlledUnit.OnMovementFinished -= HandleMovementFinished;
        controlledUnit.OnMovementFinished += HandleMovementFinished;

        if (!controlledUnit.TryMove(trimmedPath))
        {
            controlledUnit.OnMovementFinished -= HandleMovementFinished;
            pendingAttackTarget = null;
            return false;
        }

        LastActionWasMovement = true;
        Debug.Log("Enemy movement started.");
        return true;
    }

    public bool HasInvestigationTarget()
    {
        return rememberedTargetUnit != null &&
               !rememberedTargetUnit.IsDead &&
               lastKnownTargetTile != null;
    }

    private bool HasInvestigationPointTarget()
    {
        return HasInvestigationTarget() ||
               (hasSuspiciousInvestigationPosition && lastKnownTargetTile != null);
    }

    public bool HasActiveTargetKnowledge()
    {
        return HasInvestigationPointTarget() ||
               CanSearchSuspiciousBarrels() ||
               shouldReturnToPost ||
               pendingAttackTarget != null ||
               pendingPushTarget != null ||
               pendingBarrelTarget != null;
    }

    public bool IsTrackingUnit(GridUnit unit)
    {
        if (unit == null || unit.IsDead)
            return false;

        return rememberedTargetUnit == unit ||
               pendingAttackTarget == unit ||
               pendingPushTarget == unit;
    }

    public GridUnit RefreshAwarenessFromCurrentFacing()
    {
        GridUnit visibleTarget = GetVisiblePlayerInCurrentFacing();
        if (visibleTarget == null)
        {
            RefreshBarrelLayoutMemory(true);
            return null;
        }

        RememberTarget(visibleTarget);
        Debug.Log($"{controlledUnit.name} spotted {visibleTarget.name} while looking.");
        return visibleTarget;
    }

    public void RememberTarget(GridUnit targetUnit)
    {
        if (targetUnit == null || targetUnit.CurrentTile == null)
            return;

        shouldReturnToPost = false;

        bool shouldShowAlertFeedback =
            rememberedTargetUnit != targetUnit ||
            lastKnownTargetTile == null ||
            currentState == EnemyAIState.Idle ||
            currentState == EnemyAIState.Patrol ||
            currentState == EnemyAIState.ReturnToPost;

        if (lastKnownTargetTile != null && targetUnit.CurrentTile != lastKnownTargetTile)
        {
            Vector3 movementDirection = GetDirectionBetweenTiles(lastKnownTargetTile, targetUnit.CurrentTile);
            if (movementDirection.sqrMagnitude > 0.0001f)
                lastSeenMovementDirection = movementDirection;
        }
        else if (targetUnit.CurrentTile != null && controlledUnit != null && controlledUnit.CurrentTile != null)
        {
            Vector3 fallbackDirection = GetDirectionBetweenTiles(controlledUnit.CurrentTile, targetUnit.CurrentTile);
            if (fallbackDirection.sqrMagnitude > 0.0001f)
                lastSeenMovementDirection = fallbackDirection;
        }

        rememberedTargetUnit = targetUnit;
        lastKnownTargetTile = targetUnit.CurrentTile;
        failedInvestigationAttempts = 0;
        HiddenStateComponent hiddenState = targetUnit.GetComponent<HiddenStateComponent>();
        RegisterObservedBarrelPosition(hiddenState != null ? hiddenState.CurrentBarrel : null);
        if (shouldShowAlertFeedback)
            SetState(EnemyAIState.Alert);
        lookDirectionIndex = 0;
    }

    public void RememberMovingBarrelTarget(GridUnit targetUnit)
    {
        if (targetUnit == null || targetUnit.CurrentTile == null)
            return;

        HiddenStateComponent hiddenState = targetUnit.GetComponent<HiddenStateComponent>();
        if (hiddenState == null || hiddenState.CurrentBarrel == null)
        {
            RememberTarget(targetUnit);
            return;
        }

        observedBarrelPositions.Clear();
        RememberTarget(targetUnit);
        RegisterObservedBarrelPosition(hiddenState.CurrentBarrel);
        prioritizeLastKnownBarrelMovement = true;
        barrelSearchInterruptedByMovingTarget = true;
        SetState(EnemyAIState.Alert);

        Debug.Log($"{controlledUnit.name} saw a barrel move and is prioritizing its last known path.");
    }

    public bool TryInvestigate()
    {
        SetState(EnemyAIState.Investigate);
        LastActionWasMovement = false;
        pendingAttackTarget = null;
        pendingBarrelTarget = null;
        pendingPushTarget = null;
        pendingInvestigationScanAfterMove = false;

        if (controlledUnit == null || controlledUnit.IsDead || lastKnownTargetTile == null)
        {
            FailSafeEndInvestigation("missing investigation data");
            return false;
        }

        if (controlledUnit.IsMoving)
            return false;

        GridTile destinationTile = GetBestInvestigationDestination(lastKnownTargetTile);
        if (destinationTile == null)
        {
            FailSafeEndInvestigation("no valid investigation destination");
            return false;
        }

        if (controlledUnit.CurrentTile != destinationTile)
        {
            if (!controlledUnit.CanMoveThisTurn())
            {
                FailSafeEndInvestigation("no movement available to continue investigation");
                return false;
            }

            List<GridTile> fullPath = pathFinder.FindPath(controlledUnit.CurrentTile, destinationTile, controlledUnit);
            if (fullPath == null || fullPath.Count <= 1)
            {
                FailSafeEndInvestigation("no path to investigation destination");
                return false;
            }

            List<GridTile> trimmedPath = TrimPathByMovementBudget(fullPath, controlledUnit);
            if (trimmedPath == null || trimmedPath.Count <= 1)
            {
                FailSafeEndInvestigation("movement budget cannot advance investigation");
                return false;
            }

            controlledUnit.OnMovementFinished -= HandleMovementFinished;
            controlledUnit.OnMovementFinished += HandleMovementFinished;

            pendingInvestigationScanAfterMove = true;
            if (!controlledUnit.TryMove(trimmedPath))
            {
                controlledUnit.OnMovementFinished -= HandleMovementFinished;
                pendingInvestigationScanAfterMove = false;
                FailSafeEndInvestigation("movement failed while investigating");
                return false;
            }

            LastActionWasMovement = true;
            Debug.Log($"{controlledUnit.name} is investigating last known position.");
            failedInvestigationAttempts = 0;
            return true;
        }

        if (IsInvestigationScanRunning)
            return false;

        failedInvestigationAttempts = 0;
        StartCoroutine(ScanAllDirectionsAndReactRoutine());
        return true;
    }

    public bool TryActAgainstBarrel(BarrelInteractable barrel)
    {
        SetState(EnemyAIState.SearchBarrels);
        LastActionWasMovement = false;
        pendingAttackTarget = null;
        pendingBarrelTarget = null;
        pendingPushTarget = null;
        pendingInvestigationScanAfterMove = false;

        if (barrel == null || controlledUnit == null || controlledUnit.IsDead || pathFinder == null)
            return false;

        GridTile barrelTile = barrel.GetBarrelTilePublic();
        if (barrelTile == null || controlledUnit.CurrentTile == null)
            return false;

        if (!controlledUnit.CanAttackThisTurn())
            return false;

        if (IsAdjacentToTile(barrelTile))
        {
            return TryBreakBarrelAsAttack(barrel, out _);
        }

        if (!controlledUnit.CanMoveThisTurn())
            return false;

        if (!TryGetBestApproachPath(barrelTile, 1, out List<GridTile> trimmedPath, out bool reachesBarrel))
            return false;

        if (trimmedPath == null || trimmedPath.Count <= 1)
            return false;

        if (reachesBarrel)
            pendingBarrelTarget = barrel;

        controlledUnit.OnMovementFinished -= HandleMovementFinished;
        controlledUnit.OnMovementFinished += HandleMovementFinished;

        if (!controlledUnit.TryMove(trimmedPath))
        {
            controlledUnit.OnMovementFinished -= HandleMovementFinished;
            pendingBarrelTarget = null;
            return false;
        }

        LastActionWasMovement = true;
        Debug.Log($"{controlledUnit.name} is moving to search a barrel.");
        return true;
    }

    public bool TrySearchVisibleBarrels(BarrelInteractable firstBarrel = null)
    {
        SetState(EnemyAIState.SearchBarrels);
        LastActionWasMovement = false;
        pendingAttackTarget = null;
        pendingBarrelTarget = null;
        pendingPushTarget = null;
        pendingInvestigationScanAfterMove = false;

        if (controlledUnit == null || controlledUnit.IsDead || controlledUnit.IsMoving || IsActionAnimationRunning)
            return false;

        if (!controlledUnit.CanAttackThisTurn())
            return false;

        if (!CanContinueBarrelSearch())
            return false;

        BarrelInteractable firstTarget = firstBarrel != null ? firstBarrel : GetPriorityVisibleBarrelTarget();
        if (firstTarget == null)
            return false;

        StartCoroutine(SearchVisibleBarrelsRoutine(firstTarget));
        return true;
    }

    private bool TryPatrol()
    {
        LastActionWasMovement = false;
        pendingAttackTarget = null;
        pendingBarrelTarget = null;
        pendingPushTarget = null;
        pendingInvestigationScanAfterMove = false;
        pendingPatrolDestinationFlip = false;

        if (!hasPatrolRoute || controlledUnit == null || controlledUnit.IsDead || controlledUnit.CurrentTile == null || pathFinder == null)
            return false;

        if (controlledUnit.IsMoving || !controlledUnit.CanMoveThisTurn())
            return false;

        GridTile destinationTile = GetCurrentPatrolDestinationTile();
        if (destinationTile == null)
            return false;

        if (controlledUnit.CurrentTile == destinationTile)
        {
            FlipPatrolDestination();
            destinationTile = GetCurrentPatrolDestinationTile();

            if (destinationTile == null || controlledUnit.CurrentTile == destinationTile)
                return false;
        }

        if (destinationTile.isOccupied && destinationTile != controlledUnit.CurrentTile)
            return false;

        List<GridTile> fullPath = pathFinder.FindPath(
            controlledUnit.CurrentTile,
            destinationTile,
            controlledUnit,
            true
        );
        if (fullPath == null || fullPath.Count <= 1)
            return false;

        List<GridTile> trimmedPath = TrimPathByMovementBudget(fullPath, controlledUnit);
        if (trimmedPath == null || trimmedPath.Count <= 1)
            return false;

        GridTile finalTile = trimmedPath[trimmedPath.Count - 1];
        pendingPatrolDestinationFlip = finalTile == destinationTile;

        controlledUnit.OnMovementFinished -= HandleMovementFinished;
        controlledUnit.OnMovementFinished += HandleMovementFinished;

        if (!controlledUnit.TryMove(trimmedPath))
        {
            controlledUnit.OnMovementFinished -= HandleMovementFinished;
            pendingPatrolDestinationFlip = false;
            return false;
        }

        LastActionWasMovement = true;
        Debug.Log($"{controlledUnit.name} is patrolling toward {destinationTile.GridPosition}.");
        return true;
    }

    private bool TryRandomLook()
    {
        if (controlledUnit == null || controlledUnit.IsDead || controlledUnit.IsMoving)
            return false;

        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.right,
            Vector3.back,
            Vector3.left
        };

        FaceWorldDirection(directions[Random.Range(0, directions.Length)]);
        RefreshAwarenessFromCurrentFacing();
        LastActionWasMovement = false;
        Debug.Log($"{controlledUnit.name} looks around randomly.");
        return true;
    }

    private bool TryReturnToPost()
    {
        LastActionWasMovement = false;
        pendingAttackTarget = null;
        pendingBarrelTarget = null;
        pendingPushTarget = null;
        pendingInvestigationScanAfterMove = false;
        pendingPatrolDestinationFlip = false;

        if (!shouldReturnToPost || !ShouldUseHomePostReturn())
        {
            FinishReturnToPost();
            return false;
        }

        if (pathFinder == null)
            return false;

        GridTile homeTile = GetHomeTile();
        if (homeTile == null)
        {
            shouldReturnToPost = false;
            SetState(GetCalmState());
            return false;
        }

        if (IsAtHomePost())
        {
            FinishReturnToPost();
            Debug.Log($"{controlledUnit.name} returned to its home post.");
            return true;
        }

        if (homeTile.isOccupied && homeTile.OccupyingUnit != controlledUnit.gameObject)
            return false;

        if (controlledUnit.IsMoving || !controlledUnit.CanMoveThisTurn())
            return false;

        List<GridTile> fullPath = pathFinder.FindPath(controlledUnit.CurrentTile, homeTile, controlledUnit);
        if (fullPath == null || fullPath.Count <= 1)
            return false;

        List<GridTile> trimmedPath = TrimPathByMovementBudget(fullPath, controlledUnit);
        if (trimmedPath == null || trimmedPath.Count <= 1)
            return false;

        controlledUnit.OnMovementFinished -= HandleMovementFinished;
        controlledUnit.OnMovementFinished += HandleMovementFinished;

        if (!controlledUnit.TryMove(trimmedPath))
        {
            controlledUnit.OnMovementFinished -= HandleMovementFinished;
            return false;
        }

        LastActionWasMovement = true;
        Debug.Log($"{controlledUnit.name} is returning to its home post.");
        return true;
    }

    private GridTile GetCurrentPatrolDestinationTile()
    {
        if (gridManager == null)
            return null;

        Vector2Int destination = patrolHeadingToEnd ? patrolEndGridPosition : patrolStartGridPosition;
        return gridManager.GetTileAt(destination);
    }

    private void FlipPatrolDestination()
    {
        patrolHeadingToEnd = !patrolHeadingToEnd;
    }

    private List<GridTile> TrimPathByMovementBudget(List<GridTile> fullPath, GridUnit unit)
    {
        List<GridTile> result = new List<GridTile>();

        if (fullPath == null || fullPath.Count == 0 || unit == null)
            return result;

        result.Add(fullPath[0]); // start tile

        int remainingMovement = unit.RemainingMovementPoints;

        for (int i = 1; i < fullPath.Count; i++)
        {
            GridTile tile = fullPath[i];
            bool isFinalDestination = (i == fullPath.Count - 1);

            int costToEnter = unit.GetMovementCostForTile(tile, isFinalDestination);

            if (costToEnter > remainingMovement)
                break;

            remainingMovement -= costToEnter;
            result.Add(tile);

            if (remainingMovement <= 0)
                break;
        }

        return result;
    }
    private bool TryGetBestApproachPath(GridTile targetTile, int actionRange, out List<GridTile> bestPath, out bool reachesActionRange)
    {
        bestPath = null;
        reachesActionRange = false;

        if (targetTile == null || controlledUnit == null || controlledUnit.CurrentTile == null || gridManager == null || pathFinder == null)
            return false;

        List<GridTile> candidateTiles = GetActionRangeStandTiles(targetTile, Mathf.Max(1, actionRange));
        int currentDistanceToTarget = GetDistanceBetweenTiles(controlledUnit.CurrentTile, targetTile);
        int bestScore = int.MinValue;

        foreach (GridTile candidateTile in candidateTiles)
        {
            if (!CanStandOnTile(candidateTile))
                continue;

            List<GridTile> fullPath = pathFinder.FindPath(controlledUnit.CurrentTile, candidateTile, controlledUnit);
            if (fullPath == null || fullPath.Count <= 1)
                continue;

            List<GridTile> trimmedPath = TrimPathByMovementBudget(fullPath, controlledUnit);
            if (trimmedPath == null || trimmedPath.Count <= 1)
                continue;

            GridTile finalTile = trimmedPath[trimmedPath.Count - 1];
            bool reachedCandidate = finalTile == candidateTile;
            int finalDistanceToTarget = GetDistanceBetweenTiles(finalTile, targetTile);

            if (!reachedCandidate && finalDistanceToTarget >= currentDistanceToTarget)
                continue;

            int score = ScoreApproachPath(trimmedPath, targetTile, reachedCandidate);

            if (score > bestScore)
            {
                bestScore = score;
                bestPath = trimmedPath;
                reachesActionRange = reachedCandidate && finalDistanceToTarget <= Mathf.Max(1, actionRange);
            }
        }

        return bestPath != null;
    }

    private List<GridTile> GetActionRangeStandTiles(GridTile targetTile, int actionRange)
    {
        List<GridTile> result = new List<GridTile>();

        if (targetTile == null || gridManager == null)
            return result;

        for (int x = -actionRange; x <= actionRange; x++)
        {
            for (int y = -actionRange; y <= actionRange; y++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(y);
                if (distance == 0 || distance > actionRange)
                    continue;

                GridTile tile = gridManager.GetTileAt(targetTile.GridPosition + new Vector2Int(x, y));
                if (tile != null && !result.Contains(tile))
                    result.Add(tile);
            }
        }

        return result;
    }

    private int ScoreApproachPath(List<GridTile> path, GridTile targetTile, bool reachesActionRange)
    {
        if (path == null || path.Count <= 1 || targetTile == null)
            return int.MinValue;

        GridTile finalTile = path[path.Count - 1];
        int distanceToTarget = GetDistanceBetweenTiles(finalTile, targetTile);
        int pathCost = CalculatePathCost(path);
        int riskPenalty = 0;

        for (int i = 1; i < path.Count; i++)
            riskPenalty += GetTerrainRiskScore(path[i]);

        int score = reachesActionRange ? 100000 : 0;
        score -= distanceToTarget * 1000;
        score -= pathCost * 20;
        score -= riskPenalty;
        return score;
    }

    private int GetTerrainRiskScore(GridTile tile)
    {
        TerrainTypeData terrainData = tile != null ? tile.CurrentTerrainData : null;
        if (terrainData == null)
            return 0;

        return (terrainData.DamageOnEnter * 200) +
               (terrainData.DamageOnStop * 150) +
               (terrainData.MovementPenaltyOnEntry * 20) +
               (terrainData.MovementPenaltyOnStop * 15);
    }

    private int GetDistanceBetweenTiles(GridTile a, GridTile b)
    {
        if (a == null || b == null)
            return int.MaxValue;

        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
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

    private GridTile GetClosestAdjacentTileToTile(GridTile targetTile)
    {
        if (targetTile == null || controlledUnit == null || controlledUnit.CurrentTile == null || gridManager == null)
            return null;

        List<GridTile> neighbors = gridManager.GetNeighbors(targetTile);
        GridTile bestTile = null;
        int bestDistance = int.MaxValue;

        foreach (GridTile tile in neighbors)
        {
            if (tile == null || !tile.isWalkable)
                continue;

            if (tile != controlledUnit.CurrentTile && tile.isOccupied)
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

    private int ScoreAttackOption(GridUnit target)
    {
        if (controlledUnit == null || target == null || target.IsDead)
            return 0;

        int expectedDamage = Mathf.Max(1, controlledUnit.AttackDamage - target.Defense);
        int score = expectedDamage * 100;

        if (expectedDamage >= target.CurrentHP)
            score += 120000;

        return score;
    }

    private bool TryGetBestPushPlan(GridUnit target, out EnemyPushPlan bestPlan)
    {
        bestPlan = new EnemyPushPlan();

        if (controlledUnit == null || target == null || target.CurrentTile == null || gridManager == null || pathFinder == null)
            return false;

        if (!controlledUnit.CanPushTarget(target))
            return false;

        List<GridTile> standTiles = gridManager.GetNeighbors(target.CurrentTile);
        bool foundPlan = false;
        int bestScore = int.MinValue;

        foreach (GridTile standTile in standTiles)
        {
            if (!CanStandOnTile(standTile))
                continue;

            GridTile pushDestination = controlledUnit.GetPushDestinationFromTile(target, gridManager, standTile);
            if (pushDestination == null)
                continue;

            int score = ScorePushDestination(target, pushDestination);
            if (score <= 0)
                continue;

            bool isImmediate = standTile == controlledUnit.CurrentTile;
            int pathCost = 0;

            if (!isImmediate)
            {
                if (!CanUseAttackActionAfterMoving())
                    continue;

                List<GridTile> pathToStandTile = pathFinder.FindPath(controlledUnit.CurrentTile, standTile, controlledUnit);
                if (pathToStandTile == null || pathToStandTile.Count <= 1)
                    continue;

                List<GridTile> trimmedPath = TrimPathByMovementBudget(pathToStandTile, controlledUnit);
                if (trimmedPath == null || trimmedPath.Count <= 1 || trimmedPath[trimmedPath.Count - 1] != standTile)
                    continue;

                pathCost = CalculatePathCost(trimmedPath);
            }

            score -= pathCost * 5;

            if (!foundPlan || score > bestScore)
            {
                foundPlan = true;
                bestScore = score;
                bestPlan = new EnemyPushPlan
                {
                    StandTile = standTile,
                    DestinationTile = pushDestination,
                    Score = score,
                    IsImmediate = isImmediate
                };
            }
        }

        return foundPlan;
    }

    private bool CanUseAttackActionAfterMoving()
    {
        return controlledUnit != null &&
               controlledUnit.TurnRules != null &&
               controlledUnit.TurnRules.CanAttackAfterMoving;
    }

    private int ScorePushDestination(GridUnit target, GridTile destinationTile)
    {
        if (target == null || destinationTile == null)
            return 0;

        TerrainTypeData terrainData = destinationTile.CurrentTerrainData;
        if (terrainData == null)
            return 0;

        int entryDamage = target.EstimateTerrainEntryDamage(destinationTile);
        int startTurnDamage = target.EstimateTerrainStartTurnDamage(destinationTile);
        int entryMovementPenalty = Mathf.Max(0, terrainData.MovementPenaltyOnEntry);
        int stopMovementPenalty = Mathf.Max(0, terrainData.MovementPenaltyOnStop);
        int totalMovementPenalty = entryMovementPenalty + stopMovementPenalty;

        int score = (entryDamage * 180) + (startTurnDamage * 120) + (totalMovementPenalty * 25);

        if (entryDamage >= target.CurrentHP)
            score += 110000;
        else if (entryDamage + startTurnDamage >= target.CurrentHP)
            score += 70000;

        return score;
    }

    private bool TryMoveForPush(GridUnit target, EnemyPushPlan pushPlan)
    {
        if (controlledUnit == null || target == null || pushPlan.StandTile == null)
            return false;

        if (!controlledUnit.CanMoveThisTurn())
            return false;

        List<GridTile> fullPath = pathFinder.FindPath(controlledUnit.CurrentTile, pushPlan.StandTile, controlledUnit);
        if (fullPath == null || fullPath.Count <= 1)
            return false;

        List<GridTile> trimmedPath = TrimPathByMovementBudget(fullPath, controlledUnit);
        if (trimmedPath == null || trimmedPath.Count <= 1 || trimmedPath[trimmedPath.Count - 1] != pushPlan.StandTile)
            return false;

        pendingPushTarget = target;
        controlledUnit.OnMovementFinished -= HandleMovementFinished;
        controlledUnit.OnMovementFinished += HandleMovementFinished;

        if (!controlledUnit.TryMove(trimmedPath))
        {
            controlledUnit.OnMovementFinished -= HandleMovementFinished;
            pendingPushTarget = null;
            return false;
        }

        LastActionWasMovement = true;
        Debug.Log($"{controlledUnit.name} moves to push {target.name} into {pushPlan.DestinationTile.TerrainType}.");
        return true;
    }

    private bool TryExecutePush(GridUnit target)
    {
        if (controlledUnit == null || target == null || target.IsDead)
            return false;

        if (!controlledUnit.TryPush(target, gridManager, false))
            return false;

        IsActionAnimationRunning = true;
        LastActionWasMovement = false;
        StartCoroutine(WaitForPushActionRoutine(target));
        Debug.Log($"{controlledUnit.name} pushes {target.name} because the destination is more dangerous than a direct attack.");
        return true;
    }

    private System.Collections.IEnumerator WaitForPushActionRoutine(GridUnit target)
    {
        yield return null;

        while (target != null && !target.IsDead && target.IsMoving)
            yield return null;

        if (target != null && !target.IsDead && target.CurrentTile != null)
            RememberTarget(target);

        IsActionAnimationRunning = false;
    }

    private GridTile GetBestInvestigationDestination(GridTile targetTile)
    {
        if (targetTile == null || controlledUnit == null || controlledUnit.CurrentTile == null || gridManager == null)
            return null;

        GridTile bestTile = null;
        int bestDistance = int.MaxValue;

        if (CanStandOnTile(targetTile))
        {
            bestTile = targetTile;
            bestDistance = GetDistanceToTile(targetTile);
        }

        List<GridTile> neighbors = gridManager.GetNeighbors(targetTile);
        foreach (GridTile tile in neighbors)
        {
            if (!CanStandOnTile(tile))
                continue;

            int distance = GetDistanceToTile(tile);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTile = tile;
            }
        }

        return bestTile;
    }

    private bool CanStandOnTile(GridTile tile)
    {
        if (tile == null)
            return false;

        if (!tile.isWalkable)
            return false;

        if (tile == controlledUnit.CurrentTile)
            return true;

        return !tile.isOccupied;
    }

    private int GetDistanceToTile(GridTile tile)
    {
        if (tile == null || controlledUnit == null || controlledUnit.CurrentTile == null)
            return int.MaxValue;

        return Mathf.Abs(tile.X - controlledUnit.CurrentTile.X) +
               Mathf.Abs(tile.Y - controlledUnit.CurrentTile.Y);
    }

    private System.Collections.IEnumerator ScanAllDirectionsAndReactRoutine()
    {
        IsInvestigationScanRunning = true;
        prioritizeLastKnownBarrelMovement = false;

        bool foundTarget = false;
        yield return ScanDirectionsOnce(
            () => foundTarget = true
        );

        if (foundTarget)
            yield break;

        if (TryGetDirectionalAdvancePath(out List<GridTile> advancePath))
        {
            bool finished = false;

            void OnAdvanceFinished(GridUnit _)
            {
                finished = true;
            }

            controlledUnit.OnMovementFinished -= OnAdvanceFinished;
            controlledUnit.OnMovementFinished += OnAdvanceFinished;

            if (controlledUnit.TryMove(advancePath))
            {
                LastActionWasMovement = true;

                while (!finished)
                    yield return null;
            }

            controlledUnit.OnMovementFinished -= OnAdvanceFinished;

            foundTarget = false;
            yield return ScanDirectionsOnce(
                () => foundTarget = true
            );

            if (foundTarget)
                yield break;
        }

        Debug.Log($"{controlledUnit.name} checked all directions at last known position and found nothing.");
        FaceLastSeenDirection();
        yield return new WaitForSeconds(investigationLookPause);
        ClearInvestigationState();
        LastActionWasMovement = false;
        IsInvestigationScanRunning = false;
    }

    private System.Collections.IEnumerator ScanDirectionsOnce(System.Action onTargetFound)
    {
        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.right,
            Vector3.back,
            Vector3.left
        };

        for (int i = 0; i < directions.Length; i++)
        {
            FaceWorldDirection(directions[i]);
            yield return new WaitForSeconds(investigationLookPause);

            GridUnit visibleTarget = GetVisiblePlayerInCurrentFacing();
            if (visibleTarget == null)
            {
                RefreshBarrelLayoutMemory(true);

                BarrelInteractable visibleBarrelTarget = GetPriorityVisibleBarrelTarget();
                if (visibleBarrelTarget != null && TrySearchVisibleBarrels(visibleBarrelTarget))
                {
                    onTargetFound?.Invoke();
                    IsInvestigationScanRunning = false;
                    yield break;
                }

                continue;
            }

            Debug.Log($"{controlledUnit.name} found {visibleTarget.name} while scanning.");
            RememberTarget(visibleTarget);
            onTargetFound?.Invoke();
            IsInvestigationScanRunning = false;
            TryAct(visibleTarget);
            yield break;
        }
    }

    private bool TryGetDirectionalAdvancePath(out List<GridTile> path)
    {
        path = null;

        if (controlledUnit == null || controlledUnit.CurrentTile == null || gridManager == null || pathFinder == null)
            return false;

        if (controlledUnit.RemainingMovementPoints <= 0)
            return false;

        Vector2Int direction = GetPrimaryGridDirection(lastSeenMovementDirection);
        if (direction == Vector2Int.zero)
            return false;

        GridTile bestCandidateTile = null;
        List<GridTile> bestCandidatePath = null;
        int bestScore = int.MinValue;

        for (int step = 1; step <= controlledUnit.RemainingMovementPoints; step++)
        {
            Vector2Int targetPos = controlledUnit.CurrentTile.GridPosition + (direction * step);
            GridTile candidateTile = gridManager.GetTileAt(targetPos);
            if (candidateTile == null)
                break;

            List<GridTile> candidatePath = pathFinder.FindPath(controlledUnit.CurrentTile, candidateTile, controlledUnit);
            if (candidatePath == null || candidatePath.Count <= 1)
                break;

            int pathCost = CalculatePathCost(candidatePath);
            if (pathCost > controlledUnit.RemainingMovementPoints)
                break;

            int score = ScoreDirectionalAdvanceTile(candidateTile, direction, pathCost);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidateTile = candidateTile;
                bestCandidatePath = candidatePath;
            }
        }

        if (bestCandidateTile == null || bestCandidatePath == null)
            return false;

        path = bestCandidatePath;
        return true;
    }

    private int ScoreDirectionalAdvanceTile(GridTile tile, Vector2Int preferredDirection, int pathCost)
    {
        if (tile == null || controlledUnit == null || controlledUnit.CurrentTile == null)
            return int.MinValue;

        Vector2Int deltaFromCurrent = tile.GridPosition - controlledUnit.CurrentTile.GridPosition;
        int directionalDistance = Mathf.Abs(deltaFromCurrent.x) + Mathf.Abs(deltaFromCurrent.y);

        Vector2Int deltaFromLastKnown = lastKnownTargetTile != null
            ? tile.GridPosition - lastKnownTargetTile.GridPosition
            : Vector2Int.zero;

        int forwardBias = (deltaFromLastKnown.x * preferredDirection.x) + (deltaFromLastKnown.y * preferredDirection.y);
        int lateralPenalty = Mathf.Abs(deltaFromLastKnown.x - preferredDirection.x * forwardBias) +
                             Mathf.Abs(deltaFromLastKnown.y - preferredDirection.y * forwardBias);

        return (directionalDistance * 100) + (forwardBias * 25) - (lateralPenalty * 10) - pathCost;
    }

    private Vector2Int GetPrimaryGridDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude <= 0.0001f)
            return Vector2Int.zero;

        if (Mathf.Abs(worldDirection.x) > Mathf.Abs(worldDirection.z))
            return worldDirection.x >= 0f ? Vector2Int.right : Vector2Int.left;

        return worldDirection.z >= 0f ? Vector2Int.up : Vector2Int.down;
    }

    private int CalculatePathCost(List<GridTile> path)
    {
        if (controlledUnit == null || path == null || path.Count <= 1)
            return 0;

        int totalCost = 0;

        for (int i = 1; i < path.Count; i++)
        {
            bool isFinalDestination = i == path.Count - 1;
            totalCost += controlledUnit.GetMovementCostForTile(path[i], isFinalDestination);
        }

        return totalCost;
    }

    private void FaceWorldDirection(Vector3 worldDirection)
    {
        if (controlledUnit == null)
            return;

        if (worldDirection.sqrMagnitude <= 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
        controlledUnit.RestoreVisualRotation(lookRotation);
    }

    private void FaceTile(GridTile tile)
    {
        if (controlledUnit == null || tile == null || controlledUnit.CurrentTile == null)
            return;

        Vector3 direction = tile.transform.position - controlledUnit.CurrentTile.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        FaceWorldDirection(direction.normalized);
    }

    private void ClearInvestigationState()
    {
        rememberedTargetUnit = null;
        lastKnownTargetTile = null;
        observedBarrelPositions.Clear();
        suspiciousBarrelPositions.Clear();
        hasSuspiciousInvestigationPosition = false;
        investigatingMissingBarrelLayoutChange = false;
        prioritizeLastKnownBarrelMovement = false;
        barrelSearchInterruptedByMovingTarget = false;
        lookDirectionIndex = 0;
        lastSeenMovementDirection = Vector3.zero;

        if (RequestReturnToPostIfNeeded())
            SetState(EnemyAIState.ReturnToPost);
        else
            SetState(GetCalmState());

        LevelObjectiveRuntimeManager.RefreshPlayerSeenState();
    }

    private void FailSafeEndInvestigation(string reason)
    {
        failedInvestigationAttempts++;
        string unitName = controlledUnit != null ? controlledUnit.name : name;
        Debug.Log($"{unitName} investigation fail-safe triggered ({failedInvestigationAttempts}/{maxFailedInvestigationAttempts}): {reason}.");

        if (failedInvestigationAttempts < maxFailedInvestigationAttempts)
            return;

        Debug.Log($"{unitName} has no useful investigation lead left. Returning to routine.");
        failedInvestigationAttempts = 0;
        ClearInvestigationState();
        LastActionWasMovement = false;
        IsInvestigationScanRunning = false;
        IsActionAnimationRunning = false;
    }

    private void FaceLastSeenDirection()
    {
        if (lastSeenMovementDirection.sqrMagnitude <= 0.0001f)
            return;

        FaceWorldDirection(lastSeenMovementDirection);
    }

    private Vector3 GetDirectionBetweenTiles(GridTile fromTile, GridTile toTile)
    {
        if (fromTile == null || toTile == null)
            return Vector3.zero;

        Vector3 fromPosition = fromTile.transform.position;
        Vector3 toPosition = toTile.transform.position;
        Vector3 direction = toPosition - fromPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        return direction.normalized;
    }

    private GridUnit GetVisiblePlayerInCurrentFacing()
    {
        EnemyVisionDetector detector = GetComponent<EnemyVisionDetector>();
        if (detector == null || controlledUnit == null || controlledUnit.CurrentTile == null)
            return null;

        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);
        GridUnit closestVisibleTarget = null;
        int closestDistance = int.MaxValue;

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null || unit.Team != UnitTeam.Player || unit.IsDead || unit.CurrentTile == null)
                continue;

            HiddenStateComponent hiddenState = unit.GetComponent<HiddenStateComponent>();
            bool isInsideBarrel = hiddenState != null && hiddenState.CurrentBarrel != null;

            bool isVisible = false;

            if (isInsideBarrel)
            {
                bool barrelVisible = detector.CanSeeBarrel(hiddenState.CurrentBarrel);
                bool barrelCarrierKnown =
                    hiddenState != null &&
                    (!hiddenState.IsHidden || hiddenState.BarrelKnownToEnemies);

                if (barrelVisible && barrelCarrierKnown && !hiddenState.IsHidden)
                {
                    RememberTarget(unit);
                    isVisible = true;
                }
                else if (barrelVisible && barrelCarrierKnown)
                {
                    RegisterObservedBarrelPosition(hiddenState.CurrentBarrel);
                }
            }
            else
            {
                isVisible = detector.CanSeeUnit(unit);
            }

            if (!isVisible)
                continue;

            int distance = Mathf.Abs(controlledUnit.CurrentTile.X - unit.CurrentTile.X) +
                           Mathf.Abs(controlledUnit.CurrentTile.Y - unit.CurrentTile.Y);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestVisibleTarget = unit;
            }
        }

        return closestVisibleTarget;
    }

    public BarrelInteractable GetPriorityVisibleBarrelTarget()
    {
        if (!CanSearchKnownBarrels() && !CanSearchSuspiciousBarrels())
            return null;

        EnemyVisionDetector detector = GetComponent<EnemyVisionDetector>();
        if (detector == null)
            return null;

        if (prioritizeLastKnownBarrelMovement)
        {
            BarrelInteractable trackedBarrel = GetTrackedVisibleTargetBarrel(detector);
            if (trackedBarrel != null)
                return trackedBarrel;

            return null;
        }

        BarrelInteractable suspiciousBarrel = GetPrioritySuspiciousVisibleBarrelTarget(detector);
        if (suspiciousBarrel != null)
            return suspiciousBarrel;

        if (!CanSearchKnownBarrels())
            return null;

        ObserveVisibleBarrels();
        PruneObservedBarrelPositions();

        BarrelInteractable closestBarrel = null;
        int closestDistance = int.MaxValue;

        BarrelInteractable[] visibleCandidates = FindObjectsByType<BarrelInteractable>(FindObjectsSortMode.None);

        foreach (BarrelInteractable barrel in visibleCandidates)
        {
            if (barrel == null)
                continue;

            GridTile barrelTile = barrel.GetBarrelTilePublic();
            if (barrelTile == null)
                continue;

            bool canSeeBarrel = detector.CanSeeBarrel(barrel);
            if (!canSeeBarrel)
                continue;

            RegisterObservedBarrelPosition(barrel);

            int distance = Mathf.Abs(controlledUnit.CurrentTile.X - barrelTile.X) +
                           Mathf.Abs(controlledUnit.CurrentTile.Y - barrelTile.Y);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBarrel = barrel;
            }
        }

        return closestBarrel;
    }

    private bool CanSearchKnownBarrels()
    {
        if (!HasInvestigationTarget() || rememberedTargetUnit == null)
            return false;

        HiddenStateComponent hiddenState = rememberedTargetUnit.GetComponent<HiddenStateComponent>();
        return hiddenState != null &&
               hiddenState.CurrentBarrel != null &&
               hiddenState.BarrelKnownToEnemies;
    }

    private bool CanSearchSuspiciousBarrels()
    {
        return suspiciousBarrelPositions != null && suspiciousBarrelPositions.Count > 0;
    }

    private BarrelInteractable GetPrioritySuspiciousVisibleBarrelTarget(EnemyVisionDetector detector)
    {
        if (detector == null || !CanSearchSuspiciousBarrels() || controlledUnit == null || controlledUnit.CurrentTile == null)
            return null;

        BarrelInteractable closestBarrel = null;
        int closestDistance = int.MaxValue;
        BarrelInteractable[] barrels = FindObjectsByType<BarrelInteractable>(FindObjectsSortMode.None);

        foreach (BarrelInteractable barrel in barrels)
        {
            if (barrel == null)
                continue;

            GridTile barrelTile = barrel.GetBarrelTilePublic();
            if (barrelTile == null)
                continue;

            if (!suspiciousBarrelPositions.Contains(barrelTile.GridPosition))
                continue;

            if (!detector.CanSeeBarrel(barrel))
                continue;

            int distance = Mathf.Abs(controlledUnit.CurrentTile.X - barrelTile.X) +
                           Mathf.Abs(controlledUnit.CurrentTile.Y - barrelTile.Y);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBarrel = barrel;
            }
        }

        return closestBarrel;
    }

    private BarrelInteractable GetTrackedVisibleTargetBarrel(EnemyVisionDetector detector)
    {
        if (detector == null || rememberedTargetUnit == null)
            return null;

        HiddenStateComponent hiddenState = rememberedTargetUnit.GetComponent<HiddenStateComponent>();
        BarrelInteractable barrel = hiddenState != null ? hiddenState.CurrentBarrel : null;
        if (barrel == null || !hiddenState.BarrelKnownToEnemies)
            return null;

        return detector.CanSeeBarrel(barrel) ? barrel : null;
    }

    public bool IsTrackingHiddenBarrelTarget()
    {
        if (!HasInvestigationTarget() || rememberedTargetUnit == null)
            return false;

        HiddenStateComponent hiddenState = rememberedTargetUnit.GetComponent<HiddenStateComponent>();
        return hiddenState != null &&
               hiddenState.CurrentBarrel != null &&
               hiddenState.BarrelKnownToEnemies;
    }

    private bool IsAdjacentToTile(GridTile tile)
    {
        if (tile == null || controlledUnit == null || controlledUnit.CurrentTile == null)
            return false;

        int dx = Mathf.Abs(controlledUnit.CurrentTile.X - tile.X);
        int dy = Mathf.Abs(controlledUnit.CurrentTile.Y - tile.Y);
        return (dx + dy) == 1;
    }

    private bool IsBarrelInLastSeenDirection(GridTile barrelTile)
    {
        if (barrelTile == null || lastKnownTargetTile == null)
            return false;

        if (lastSeenMovementDirection.sqrMagnitude <= 0.0001f)
            return false;

        Vector3 directionFromLastKnown = barrelTile.transform.position - lastKnownTargetTile.transform.position;
        directionFromLastKnown.y = 0f;

        if (directionFromLastKnown.sqrMagnitude <= 0.0001f)
            return true;

        directionFromLastKnown.Normalize();
        Vector3 expectedDirection = lastSeenMovementDirection.normalized;

        float alignment = Vector3.Dot(directionFromLastKnown, expectedDirection);
        return alignment >= 0.35f;
    }

    private System.Collections.IEnumerator SearchVisibleBarrelsRoutine(BarrelInteractable firstBarrel)
    {
        IsActionAnimationRunning = true;
        barrelSearchInterruptedByMovingTarget = false;

        BarrelInteractable nextBarrel = firstBarrel;
        int searchedCount = 0;

        while (searchedCount < maxBarrelsToSearchInOneTurn && CanContinueBarrelSearch() && controlledUnit.CanAttackThisTurn())
        {
            if (barrelSearchInterruptedByMovingTarget)
                break;

            ObserveVisibleBarrels();

            if (nextBarrel == null)
                nextBarrel = GetPriorityVisibleBarrelTarget();

            if (nextBarrel == null)
                break;

            EnemyVisionDetector detector = GetComponent<EnemyVisionDetector>();
            if (detector == null || !detector.CanSeeBarrel(nextBarrel))
            {
                nextBarrel = null;
                continue;
            }

            GridTile barrelTile = nextBarrel.GetBarrelTilePublic();
            if (barrelTile == null)
            {
                ForgetBarrelTarget(nextBarrel);
                nextBarrel = null;
                continue;
            }

            bool movedThisStep = false;

            if (!IsAdjacentToTile(barrelTile))
            {
                if (!controlledUnit.CanMoveThisTurn())
                    break;

                if (!TryGetBestApproachPath(barrelTile, 1, out List<GridTile> approachPath, out bool reachesBarrel))
                    break;

                if (approachPath == null || approachPath.Count <= 1 || !reachesBarrel)
                    break;

                if (!controlledUnit.TryMove(approachPath))
                    break;

                movedThisStep = true;

                float movementStartTime = Time.time;
                while (controlledUnit != null && controlledUnit.IsMoving)
                {
                    ObserveVisibleBarrels();

                    if (barrelSearchInterruptedByMovingTarget)
                        break;

                    if (Time.time - movementStartTime > barrelSearchMovementTimeout)
                    {
                        Debug.LogWarning($"{controlledUnit.name} barrel search movement timed out.");
                        break;
                    }

                    yield return null;
                }

                if (barrelSearchInterruptedByMovingTarget)
                    break;

                ObserveVisibleBarrels();
            }

            barrelTile = nextBarrel != null ? nextBarrel.GetBarrelTilePublic() : null;
            if (nextBarrel == null || barrelTile == null)
            {
                nextBarrel = null;
                continue;
            }

            if (!IsAdjacentToTile(barrelTile))
            {
                nextBarrel = GetPriorityVisibleBarrelTarget();
                if (!movedThisStep)
                    break;

                continue;
            }

            GridUnit releasedUnit = nextBarrel.HiddenUnit;

            if (!TryBreakBarrelAsAttack(nextBarrel, out bool foundUnit))
                break;

            ForgetBarrelTarget(nextBarrel);
            searchedCount++;

            yield return new WaitForSeconds(investigationLookPause);

            if (foundUnit && releasedUnit != null && !releasedUnit.IsDead)
            {
                RememberTarget(releasedUnit);
                break;
            }

            nextBarrel = GetPriorityVisibleBarrelTarget();
        }

        if (searchedCount >= maxBarrelsToSearchInOneTurn)
            Debug.LogWarning($"{controlledUnit.name} reached the barrel search safety limit.");

        barrelSearchInterruptedByMovingTarget = false;
        IsActionAnimationRunning = false;

        if (!CanContinueBarrelSearch() && !HasInvestigationPointTarget())
            FailSafeEndInvestigation("barrel search ended with no remaining leads");
    }

    private bool CanContinueBarrelSearch()
    {
        return CanSearchKnownBarrels() || CanSearchSuspiciousBarrels();
    }

    private void ForgetBarrelTarget(BarrelInteractable barrel)
    {
        if (barrel == null)
            return;

        GridTile barrelTile = barrel.GetBarrelTilePublic();
        if (barrelTile != null)
        {
            observedBarrelPositions.Remove(barrelTile.GridPosition);
            suspiciousBarrelPositions.Remove(barrelTile.GridPosition);
        }

        if (!CanSearchSuspiciousBarrels() && !CanSearchKnownBarrels())
            hasSuspiciousInvestigationPosition = false;
    }

    private bool TryBreakBarrelAsAttack(BarrelInteractable barrel, out bool foundUnit)
    {
        foundUnit = false;

        if (barrel == null || controlledUnit == null || !controlledUnit.CanAttackThisTurn())
            return false;

        GridTile barrelTile = barrel.GetBarrelTilePublic();
        if (barrelTile == null || !IsAdjacentToTile(barrelTile))
            return false;

        FaceTile(barrelTile);
        foundUnit = barrel.BreakOpenByEnemySearch();
        controlledUnit.MarkAttackedThisTurn();
        return true;
    }

    private void RegisterObservedBarrelPosition(BarrelInteractable barrel)
    {
        GridTile barrelTile = barrel != null ? barrel.GetBarrelTilePublic() : null;
        if (barrelTile == null)
            return;

        if (!observedBarrelPositions.Contains(barrelTile.GridPosition))
            observedBarrelPositions.Add(barrelTile.GridPosition);
    }
    
    private void HandleMovementFinished(GridUnit unit)
    {
        controlledUnit.OnMovementFinished -= HandleMovementFinished;

        if (pendingPushTarget != null)
        {
            GridUnit targetToPush = pendingPushTarget;
            pendingPushTarget = null;
            pendingInvestigationScanAfterMove = false;

            if (!TryExecutePush(targetToPush) && targetToPush != null && !targetToPush.IsDead)
            {
                if (controlledUnit.TryAttack(targetToPush))
                    Debug.Log("Enemy push failed after moving, so it attacks instead.");
            }

            return;
        }

        if (pendingAttackTarget != null)
        {
            if (controlledUnit.TryAttack(pendingAttackTarget))
            {
                Debug.Log("Enemy attacks after moving.");
            }

            pendingAttackTarget = null;
            pendingInvestigationScanAfterMove = false;
            return;
        }

        if (pendingBarrelTarget != null)
        {
            GridTile barrelTile = pendingBarrelTarget.GetBarrelTilePublic();
            if (barrelTile != null && IsAdjacentToTile(barrelTile))
            {
                TryBreakBarrelAsAttack(pendingBarrelTarget, out _);
            }

            ForgetBarrelTarget(pendingBarrelTarget);
            pendingBarrelTarget = null;
            pendingInvestigationScanAfterMove = false;
            return;
        }

        if (pendingInvestigationScanAfterMove)
        {
            pendingInvestigationScanAfterMove = false;
            StartCoroutine(ScanAllDirectionsAndReactRoutine());
            return;
        }

        if (pendingPatrolDestinationFlip)
        {
            pendingPatrolDestinationFlip = false;
            DestroyInteractableAtCurrentTile();
            FlipPatrolDestination();
            SetState(GetCalmState());
            return;
        }

        if (currentState == EnemyAIState.ReturnToPost && shouldReturnToPost)
        {
            if (IsAtHomePost())
            {
                FinishReturnToPost();
                Debug.Log($"{controlledUnit.name} returned to its home post.");
            }
        }
    }

    private void DestroyInteractableAtCurrentTile()
    {
        if (controlledUnit == null || controlledUnit.CurrentTile == null)
            return;

        if (interactablePlacementService == null)
            interactablePlacementService = FindFirstObjectByType<InteractablePlacementService>();

        if (interactablePlacementService == null)
            return;

        GridTile tile = controlledUnit.CurrentTile;
        PlacedInteractable placedInteractable = interactablePlacementService.GetPlacedInteractableAtTile(tile);
        if (placedInteractable == null)
            return;

        BarrelInteractable barrel = placedInteractable.GetComponent<BarrelInteractable>();
        if (barrel != null)
            barrel.BreakOpenByEnemySearch();
        else
            interactablePlacementService.RemoveInteractableAtTile(tile);

        tile.SetOccupant(controlledUnit.gameObject);
        Debug.Log($"{controlledUnit.name} destroyed an interactable on its patrol point.");
    }

    public static void NotifyEnemiesOfVisiblePlayer(GridUnit playerUnit)
    {
        if (playerUnit == null || playerUnit.Team != UnitTeam.Player || playerUnit.CurrentTile == null)
            return;

        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null || enemy.controlledUnit == null || enemy.controlledUnit.IsDead)
                continue;

            EnemyVisionDetector detector = enemy.GetComponent<EnemyVisionDetector>();
            if (detector == null)
                continue;

            if (detector.CanSeeUnit(playerUnit))
                enemy.RememberTarget(playerUnit);
        }
    }

    public static bool AreEnemiesAwareOfPlayer(GridUnit playerUnit)
    {
        if (playerUnit == null || playerUnit.Team != UnitTeam.Player || playerUnit.IsDead)
            return false;

        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null || enemy.controlledUnit == null || enemy.controlledUnit.IsDead)
                continue;

            if (enemy.IsTrackingUnit(playerUnit))
                return true;

            EnemyVisionDetector detector = enemy.GetComponent<EnemyVisionDetector>();
            if (detector != null && detector.CanSeeUnit(playerUnit))
                return true;
        }

        return false;
    }

    public static void NotifyEnemyHitByAttacker(GridUnit enemyUnit, GridUnit attacker)
    {
        if (enemyUnit == null || attacker == null)
            return;

        if (enemyUnit.Team != UnitTeam.Enemy || attacker.Team != UnitTeam.Player)
            return;

        EnemyController controller = enemyUnit.GetComponent<EnemyController>();
        if (controller == null)
            return;

        controller.RememberTarget(attacker);
    }

    public static void NotifyEnemiesOfVisibleBarrelCarrier(GridUnit playerUnit)
    {
        if (playerUnit == null || playerUnit.Team != UnitTeam.Player)
            return;

        HiddenStateComponent hiddenState = playerUnit.GetComponent<HiddenStateComponent>();
        if (hiddenState == null || hiddenState.CurrentBarrel == null)
            return;

        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null || enemy.controlledUnit == null || enemy.controlledUnit.IsDead)
                continue;

            EnemyVisionDetector detector = enemy.GetComponent<EnemyVisionDetector>();
            if (detector == null)
                continue;

            if (!detector.CanSeeBarrel(hiddenState.CurrentBarrel))
                continue;

            if (hiddenState.IsHidden && !hiddenState.BarrelKnownToEnemies)
                continue;

            enemy.RememberMovingBarrelTarget(playerUnit);
        }
    }

    private void ObserveVisibleBarrels()
    {
        EnemyVisionDetector detector = GetComponent<EnemyVisionDetector>();
        if (detector == null)
            return;

        BarrelInteractable[] barrels = FindObjectsByType<BarrelInteractable>(FindObjectsSortMode.None);
        foreach (BarrelInteractable barrel in barrels)
        {
            if (barrel == null)
                continue;

            if (detector.CanSeeBarrel(barrel))
                RegisterObservedBarrelPosition(barrel);
        }
    }

    private void RefreshBarrelLayoutMemory(bool allowSuspicion)
    {
        if (!CanUseBarrelLayoutMemory())
            return;

        EnemyVisionDetector detector = GetComponent<EnemyVisionDetector>();
        if (detector == null)
            return;

        List<Vector2Int> currentVisibleBarrelPositions = GetCurrentVisibleBarrelPositions(detector);
        List<Vector2Int> currentVisibleTilePositions = GetCurrentVisibleTilePositions(detector);

        if (!hasBarrelLayoutMemory)
        {
            rememberedBarrelLayoutPositions.Clear();
            rememberedBarrelLayoutPositions.AddRange(currentVisibleBarrelPositions);
            observedBarrelLayoutTilePositions.Clear();
            observedBarrelLayoutTilePositions.AddRange(currentVisibleTilePositions);
            hasBarrelLayoutMemory = true;
            return;
        }

        if (allowSuspicion)
        {
            foreach (Vector2Int visibleBarrelPosition in currentVisibleBarrelPositions)
            {
                if (rememberedBarrelLayoutPositions.Contains(visibleBarrelPosition))
                    continue;

                if (!investigatingMissingBarrelLayoutChange &&
                    !observedBarrelLayoutTilePositions.Contains(visibleBarrelPosition))
                {
                    continue;
                }

                RegisterSuspiciousBarrelChange(visibleBarrelPosition, true);
            }

            for (int i = 0; i < rememberedBarrelLayoutPositions.Count; i++)
            {
                Vector2Int rememberedPosition = rememberedBarrelLayoutPositions[i];
                if (currentVisibleBarrelPositions.Contains(rememberedPosition))
                    continue;

                GridTile rememberedTile = gridManager != null ? gridManager.GetTileAt(rememberedPosition) : null;
                if (rememberedTile == null || !detector.CanSeeTile(rememberedTile))
                    continue;

                RegisterMissingBarrelChange(rememberedPosition, currentVisibleBarrelPositions);
            }
        }

        UpdateBarrelLayoutMemoryForVisibleArea(currentVisibleTilePositions, currentVisibleBarrelPositions);
    }

    private bool CanUseBarrelLayoutMemory()
    {
        return controlledUnit != null &&
               controlledUnit.Team == UnitTeam.Enemy &&
               controlledUnit.CanNoticeBarrelLayoutChanges;
    }

    private List<Vector2Int> GetCurrentVisibleBarrelPositions(EnemyVisionDetector detector)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        if (detector == null)
            return result;

        BarrelInteractable[] barrels = FindObjectsByType<BarrelInteractable>(FindObjectsSortMode.None);
        foreach (BarrelInteractable barrel in barrels)
        {
            if (barrel == null || !detector.CanSeeBarrel(barrel))
                continue;

            GridTile barrelTile = barrel.GetBarrelTilePublic();
            if (barrelTile == null)
                continue;

            if (!result.Contains(barrelTile.GridPosition))
                result.Add(barrelTile.GridPosition);
        }

        return result;
    }

    private List<Vector2Int> GetCurrentVisibleTilePositions(EnemyVisionDetector detector)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        if (detector == null || gridManager == null || gridManager.Grid == null)
            return result;

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                GridTile tile = gridManager.Grid[x, y];
                if (tile == null || !detector.CanSeeTile(tile))
                    continue;

                if (!result.Contains(tile.GridPosition))
                    result.Add(tile.GridPosition);
            }
        }

        return result;
    }

    private void RegisterSuspiciousBarrelChange(Vector2Int gridPosition, bool hasVisibleBarrelAtPosition)
    {
        GridTile suspiciousTile = gridManager != null ? gridManager.GetTileAt(gridPosition) : null;
        if (suspiciousTile == null)
            return;

        if (hasVisibleBarrelAtPosition && !suspiciousBarrelPositions.Contains(gridPosition))
            suspiciousBarrelPositions.Add(gridPosition);

        lastKnownTargetTile = suspiciousTile;
        hasSuspiciousInvestigationPosition = true;
        investigatingMissingBarrelLayoutChange = false;
        failedInvestigationAttempts = 0;
        shouldReturnToPost = false;

        if (currentState == EnemyAIState.Idle || currentState == EnemyAIState.Patrol || currentState == EnemyAIState.ReturnToPost)
            SetState(EnemyAIState.Investigate);

        string reason = hasVisibleBarrelAtPosition ? "new barrel" : "missing barrel";
        Debug.Log($"{controlledUnit.name} noticed a {reason} at {gridPosition} and is investigating.");
    }

    private void RegisterMissingBarrelChange(Vector2Int missingBarrelPosition, List<Vector2Int> currentVisibleBarrelPositions)
    {
        GridTile missingBarrelTile = gridManager != null ? gridManager.GetTileAt(missingBarrelPosition) : null;
        if (missingBarrelTile == null)
            return;

        suspiciousBarrelPositions.Clear();
        lastKnownTargetTile = missingBarrelTile;
        hasSuspiciousInvestigationPosition = true;
        investigatingMissingBarrelLayoutChange = true;
        failedInvestigationAttempts = 0;
        shouldReturnToPost = false;

        if (currentState == EnemyAIState.Idle || currentState == EnemyAIState.Patrol || currentState == EnemyAIState.ReturnToPost)
            SetState(EnemyAIState.Investigate);

        Debug.Log($"{controlledUnit.name} noticed a missing barrel at {missingBarrelPosition} and will investigate where it was.");
    }

    private void ClearBarrelLayoutSuspicion()
    {
        suspiciousBarrelPositions.Clear();
        hasSuspiciousInvestigationPosition = false;
        investigatingMissingBarrelLayoutChange = false;

        if (!HasInvestigationTarget() && !shouldReturnToPost)
            SetState(GetCalmState());
    }

    private void UpdateBarrelLayoutMemoryForVisibleArea(List<Vector2Int> currentVisibleTilePositions, List<Vector2Int> currentVisibleBarrelPositions)
    {
        if (currentVisibleTilePositions == null || currentVisibleBarrelPositions == null)
            return;

        for (int i = rememberedBarrelLayoutPositions.Count - 1; i >= 0; i--)
        {
            if (currentVisibleTilePositions.Contains(rememberedBarrelLayoutPositions[i]))
                rememberedBarrelLayoutPositions.RemoveAt(i);
        }

        foreach (Vector2Int visibleBarrelPosition in currentVisibleBarrelPositions)
        {
            if (!rememberedBarrelLayoutPositions.Contains(visibleBarrelPosition))
                rememberedBarrelLayoutPositions.Add(visibleBarrelPosition);
        }

        foreach (Vector2Int visibleTilePosition in currentVisibleTilePositions)
        {
            if (!observedBarrelLayoutTilePositions.Contains(visibleTilePosition))
                observedBarrelLayoutTilePositions.Add(visibleTilePosition);
        }
    }

    private void PruneObservedBarrelPositions()
    {
        for (int i = observedBarrelPositions.Count - 1; i >= 0; i--)
        {
            if (HasBarrelAtObservedPosition(observedBarrelPositions[i]))
                continue;

            observedBarrelPositions.RemoveAt(i);
        }
    }

    private bool HasBarrelAtObservedPosition(Vector2Int gridPosition)
    {
        BarrelInteractable[] barrels = FindObjectsByType<BarrelInteractable>(FindObjectsSortMode.None);
        foreach (BarrelInteractable barrel in barrels)
        {
            GridTile tile = barrel != null ? barrel.GetBarrelTilePublic() : null;
            if (tile != null && tile.GridPosition == gridPosition)
                return true;
        }

        return false;
    }
}
