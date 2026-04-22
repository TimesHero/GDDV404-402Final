using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private LevelObjectiveRuntimeManager objectiveRuntimeManager;
    
    [Header("Restart Turn")]
    [SerializeField] private int maxRestartTurnUses = 1;

    private int remainingRestartTurnUses;
    private System.Collections.Generic.List<UnitTurnSnapshot> playerTurnSnapshots = new System.Collections.Generic.List<UnitTurnSnapshot>();
    
    [Header("Enemy Turn Speed")]
    [SerializeField] private EnemyTurnSpeedMode enemyTurnSpeedMode = EnemyTurnSpeedMode.Normal;

    [SerializeField, Range(0.05f, 1f)] private float fastDelayMultiplier = 0.5f;
    [SerializeField, Range(0.05f, 1f)] private float superFastDelayMultiplier = 0.2f;
    
    [Header("Enemy Turn Speed UI")]
    [SerializeField] private Image enemySpeedButtonImage;
    [SerializeField] private TMP_Text enemySpeedButtonText;

    [SerializeField] private Sprite normalSpeedSprite;
    [SerializeField] private Sprite fastSpeedSprite;
    [SerializeField] private Sprite superFastSpeedSprite;
    
    public static TurnManager Instance { get; private set; }
    
    [SerializeField] private TileSelector tileSelector;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI playerHintText;
    
    [Header("Turn Options")]
    [SerializeField] private bool autoEndTurnEnabled = false;

    public bool AutoEndTurnEnabled => autoEndTurnEnabled;
    
    [Header("Battle References")]
    [SerializeField] private UnitSpawner playerSpawner;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private float enemyTurnDelay = 0.75f;
    [SerializeField, Min(1f)] private float enemyActionSafetyTimeout = 8f;

    private InputSystem_Actions inputActions;

    public TurnState CurrentTurn { get; private set; } = TurnState.PlayerTurn;
    
    public int RemainingRestartTurnUses => remainingRestartTurnUses;
    public int MaxRestartTurnUses => maxRestartTurnUses;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        inputActions = new InputSystem_Actions();
        CurrentTurn = TurnState.PlayerTurn;
        RefreshTurnUI();
        RefreshEnemyTurnSpeedUI();
        remainingRestartTurnUses = maxRestartTurnUses;
    }
    
    private void Start()
    {
        CapturePlayerTurnSnapshot();
    }
    
    public void CycleEnemyTurnSpeedMode()
    {
        switch (enemyTurnSpeedMode)
        {
            case EnemyTurnSpeedMode.Normal:
                enemyTurnSpeedMode = EnemyTurnSpeedMode.Fast;
                break;

            case EnemyTurnSpeedMode.Fast:
                enemyTurnSpeedMode = EnemyTurnSpeedMode.SuperFast;
                break;

            case EnemyTurnSpeedMode.SuperFast:
                enemyTurnSpeedMode = EnemyTurnSpeedMode.Normal;
                break;
        }

        Debug.Log($"Enemy Turn Speed Mode changed to: {enemyTurnSpeedMode}");
        RefreshEnemyTurnSpeedUI();
    }
    
    private float GetEnemyDelayMultiplier()
    {
        switch (enemyTurnSpeedMode)
        {
            case EnemyTurnSpeedMode.Fast:
                return fastDelayMultiplier;

            case EnemyTurnSpeedMode.SuperFast:
                return superFastDelayMultiplier;

            case EnemyTurnSpeedMode.Normal:
            default:
                return 1f;
        }
    }
    
    private float GetEnemyDelay(float normalDelay)
    {
        return normalDelay * GetEnemyDelayMultiplier();
    }
    
    private void RefreshEnemyTurnSpeedUI()
    {
        bool hasAllSprites =
            normalSpeedSprite != null &&
            fastSpeedSprite != null &&
            superFastSpeedSprite != null;

        if (enemySpeedButtonImage != null)
        {
            if (hasAllSprites)
            {
                switch (enemyTurnSpeedMode)
                {
                    case EnemyTurnSpeedMode.Normal:
                        enemySpeedButtonImage.sprite = normalSpeedSprite;
                        break;

                    case EnemyTurnSpeedMode.Fast:
                        enemySpeedButtonImage.sprite = fastSpeedSprite;
                        break;

                    case EnemyTurnSpeedMode.SuperFast:
                        enemySpeedButtonImage.sprite = superFastSpeedSprite;
                        break;
                }

                enemySpeedButtonImage.enabled = true;
            }
            else
            {
                enemySpeedButtonImage.enabled = false;
            }
        }

        if (enemySpeedButtonText != null)
        {
            if (hasAllSprites)
            {
                enemySpeedButtonText.text = string.Empty;
            }
            else
            {
                switch (enemyTurnSpeedMode)
                {
                    case EnemyTurnSpeedMode.Normal:
                        enemySpeedButtonText.text = "Normal";
                        break;

                    case EnemyTurnSpeedMode.Fast:
                        enemySpeedButtonText.text = "Fast";
                        break;

                    case EnemyTurnSpeedMode.SuperFast:
                        enemySpeedButtonText.text = "Fast as Fuck";
                        break;
                }
            }
        }
    }
    
    public void SetAutoEndTurn(bool isEnabled)
    {
        autoEndTurnEnabled = isEnabled;
        Debug.Log($"Auto End Turn set to: {autoEndTurnEnabled}");
    }
    
    public void HandlePlayerUnitsDoneState()
    {
        if (!AreAllPlayerUnitsDone())
        {
            ClearPlayerHint();
            return;
        }

        ShowPlayerHint("All player units are done. You can end the turn.");

        if (!autoEndTurnEnabled)
            return;

        if (!IsPlayerTurn() || IsBusy())
            return;

        Debug.Log("All player units are done. Auto ending turn.");
        EndTurn();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Gameplay.EndTurn.performed += OnEndTurnPressed;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.EndTurn.performed -= OnEndTurnPressed;
        inputActions.Disable();
    }

    private void OnEndTurnPressed(InputAction.CallbackContext context)
    {
        if (BattleStateManager.Instance != null && BattleStateManager.Instance.BattleEnded)
            return;
        if (CurrentTurn == TurnState.Busy)
            return;

        EndTurn();
    }

    public bool IsPlayerTurn()
    {
        return CurrentTurn == TurnState.PlayerTurn;
    }

    public bool IsEnemyTurn()
    {
        return CurrentTurn == TurnState.EnemyTurn;
    }

    public bool IsBusy()
    {
        return CurrentTurn == TurnState.Busy;
    }
    
    public void ShowPlayerHint(string message)
    {
        if (playerHintText == null)
            return;

        playerHintText.text = message;
    }

    public void ClearPlayerHint()
    {
        if (playerHintText == null)
            return;

        playerHintText.text = string.Empty;
    }

    public void SetBusy()
    {
        CurrentTurn = TurnState.Busy;
        RefreshTurnUI();
        ClearPlayerHint();
        Debug.Log("Turn State: Busy");
    }

    public void StartPlayerTurn()
    {
        CurrentTurn = TurnState.PlayerTurn;
        RefreshTurnUI();
        ClearPlayerHint();

        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (GridUnit unit in allUnits)
        {
            if (unit != null && unit.Team == UnitTeam.Player)
            {
                unit.ResetTurnState();
                unit.ApplyTerrainStartTurnEffects();
            }
        }

        EnemyVisionDetector.RefreshAllHiddenStates();
        CapturePlayerTurnSnapshot();
        
        if (objectiveRuntimeManager != null)
            objectiveRuntimeManager.OnPlayerTurnStarted();
        Debug.Log("Turn State: Player Turn");
        
    }
    
    private void CapturePlayerTurnSnapshot()
    {
        playerTurnSnapshots.Clear();

        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null)
                continue;

            GridTile tile = unit.CurrentTile;
            Vector2Int gridPos = tile != null ? new Vector2Int(tile.X, tile.Y) : Vector2Int.zero;

            UnitTurnSnapshot snapshot = new UnitTurnSnapshot
            {
                unit = unit,
                gridPosition = gridPos,
                currentHP = unit.CurrentHP,
                wasDead = unit.IsDead || !unit.gameObject.activeSelf,
                hasMovedThisTurn = unit.HasMovedThisTurn,
                hasAttackedThisTurn = unit.HasAttackedThisTurn,
                attacksUsedThisTurn = unit.AttacksUsedThisTurn,
                remainingMovementPoints = unit.RemainingMovementPoints,
                visualRotation = unit.GetVisualRotation()
            };

            playerTurnSnapshots.Add(snapshot);
        }
    }
    
    public void RestartPlayerTurn()
    {
        if (!IsPlayerTurn() || IsBusy())
        {
            Debug.Log("Cannot restart turn right now.");
            return;
        }

        if (remainingRestartTurnUses <= 0)
        {
            Debug.Log("No restart turn uses remaining.");
            return;
        }

        if (playerTurnSnapshots == null || playerTurnSnapshots.Count == 0)
        {
            Debug.LogWarning("No player turn snapshot available.");
            return;
        }

        RestorePlayerTurnSnapshot();
        remainingRestartTurnUses--;
        
        if (BattleStateManager.Instance != null)
        {
            BattleStateManager.Instance.ResetBattleState();
            BattleStateManager.Instance.CheckBattleState();
        }

        if (tileSelector != null)
            tileSelector.ForceClearSelectionAndHighlights();

        RefreshTurnUI();
        ClearPlayerHint();

        Debug.Log($"Player turn restarted. Remaining restart uses: {remainingRestartTurnUses}");
    }
    
    private void RestorePlayerTurnSnapshot()
    {
        foreach (UnitTurnSnapshot snapshot in playerTurnSnapshots)
        {
            if (snapshot == null || snapshot.unit == null)
                continue;

            GridUnit unit = snapshot.unit;

            unit.RestoreAliveState(snapshot.wasDead);
            unit.RestoreHealth(snapshot.currentHP);
            unit.RestoreTurnState(
                snapshot.hasMovedThisTurn,
                snapshot.attacksUsedThisTurn,
                snapshot.remainingMovementPoints
            );

            if (!snapshot.wasDead)
            {
                GridTile tile = FindTileAt(snapshot.gridPosition);
                if (tile != null)
                    unit.PlaceOnTile(tile);
            }
            else
            {
                if (unit.CurrentTile != null)
                    unit.CurrentTile.SetOccupant(null);
            }

            unit.RestoreVisualRotation(snapshot.visualRotation);
        }

        CurrentTurn = TurnState.PlayerTurn;
    }
    
    private GridTile FindTileAt(Vector2Int gridPosition)
    {
        GridTile[] allTiles = FindObjectsByType<GridTile>(FindObjectsSortMode.None);

        foreach (GridTile tile in allTiles)
        {
            if (tile != null && tile.X == gridPosition.x && tile.Y == gridPosition.y)
                return tile;
        }

        return null;
    }
    
    public void ReturnToPlayerControl()
    {
        CurrentTurn = TurnState.PlayerTurn;
        RefreshTurnUI();
        Debug.Log("Turn State: Player Control Restored");
        
        HandlePlayerUnitsDoneState();
    }

    public void StartEnemyTurn()
    {
        if (tileSelector != null)
            tileSelector.ForceClearSelectionAndHighlights();
        
        CurrentTurn = TurnState.EnemyTurn;
        RefreshTurnUI();
        ClearPlayerHint();

        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (GridUnit unit in allUnits)
        {
            if (unit != null && unit.Team == UnitTeam.Enemy)
            {
                unit.ResetTurnState();
                unit.ApplyTerrainStartTurnEffects();
            }
        }

        Debug.Log("Turn State: Enemy Turn");

        StartCoroutine(RunEnemyTurnRoutine());
    }

    public void EndTurn()
    {
        if (CurrentTurn == TurnState.PlayerTurn)
        {
            if (objectiveRuntimeManager != null)
                objectiveRuntimeManager.OnPlayerTurnEnded();

            if (BattleStateManager.Instance != null && BattleStateManager.Instance.BattleEnded)
                return;

            StartEnemyTurn();
        }
        else if (CurrentTurn == TurnState.EnemyTurn)
        {
            StartPlayerTurn();
        }
    }
    private void OnValidate()
    {
        fastDelayMultiplier = Mathf.Clamp(fastDelayMultiplier, 0.05f, 1f);
        superFastDelayMultiplier = Mathf.Clamp(superFastDelayMultiplier, 0.05f, 1f);
    }
    
    private IEnumerator RunEnemyTurnRoutine()
    {
        Debug.Log("Enemy turn routine started.");

        CurrentTurn = TurnState.Busy;
        RefreshTurnUI();

        yield return new WaitForSeconds(GetEnemyDelay(enemyTurnDelay));

        GridUnit[] enemies = GetLivingEnemies();

        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogWarning("No living enemies found. Returning to player turn.");
            StartPlayerTurn();
            yield break;
        }

        foreach (GridUnit enemy in enemies)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            EnemyController controller = enemy.GetComponent<EnemyController>();
            if (controller == null)
            {
                Debug.LogWarning($"{enemy.name} has no EnemyController. Skipping.");
                continue;
            }

            GridUnit playerTarget = GetBestTargetForEnemy(enemy);
            bool acted = controller.TryTakeTurn(playerTarget);
            Debug.Log($"{enemy.name} state {controller.CurrentState} TryTakeTurn result: {acted}");

            if (!acted && !controller.HasActiveTargetKnowledge())
            {
                Debug.Log($"{enemy.name} has no visible or remembered target. Skipping.");
                yield return new WaitForSeconds(GetEnemyDelay(0.2f));
                continue;
            }

            if (!acted)
            {
                controller.RefreshAwarenessFromCurrentFacing();
                EnemyVisionDetector.RefreshAllHiddenStates();
                yield return new WaitForSeconds(GetEnemyDelay(0.2f));
                continue;
            }

            if (controller.IsInvestigationScanRunning)
            {
                while (controller.IsInvestigationScanRunning)
                    yield return null;
            }

            if (controller.LastActionWasMovement)
            {
                bool finished = false;

                void OnFinished(GridUnit u)
                {
                    Debug.Log($"{enemy.name} movement finished event received.");
                    finished = true;
                }

                enemy.OnMovementFinished += OnFinished;

                float movementWaitStartTime = Time.time;
                if (!enemy.IsMoving)
                    finished = true;

                while (!finished && enemy != null && enemy.IsMoving)
                {
                    if (Time.time - movementWaitStartTime > enemyActionSafetyTimeout)
                    {
                        Debug.LogWarning($"{enemy.name} movement wait timed out. Continuing enemy turn to avoid a soft lock.");
                        break;
                    }

                    yield return null;
                }

                enemy.OnMovementFinished -= OnFinished;

                if (controller.IsInvestigationScanRunning)
                {
                    while (controller.IsInvestigationScanRunning)
                        yield return null;
                }

                if (controller.IsActionAnimationRunning)
                {
                    float actionWaitStartTime = Time.time;
                    while (controller.IsActionAnimationRunning)
                    {
                        if (Time.time - actionWaitStartTime > enemyActionSafetyTimeout)
                        {
                            Debug.LogWarning($"{enemy.name} action wait timed out. Clearing wait to avoid a soft lock.");
                            controller.ForceClearActionAnimationWait();
                            break;
                        }

                        yield return null;
                    }
                }

                controller.RefreshAwarenessFromCurrentFacing();
                EnemyVisionDetector.RefreshAllHiddenStates();
                yield return new WaitForSeconds(GetEnemyDelay(0.25f));
                continue;
            }

            if (!controller.LastActionWasMovement)
            {
                if (controller.IsActionAnimationRunning)
                {
                    float actionWaitStartTime = Time.time;
                    while (controller.IsActionAnimationRunning)
                    {
                        if (Time.time - actionWaitStartTime > enemyActionSafetyTimeout)
                        {
                            Debug.LogWarning($"{enemy.name} action wait timed out. Clearing wait to avoid a soft lock.");
                            controller.ForceClearActionAnimationWait();
                            break;
                        }

                        yield return null;
                    }
                }

                controller.RefreshAwarenessFromCurrentFacing();
                EnemyVisionDetector.RefreshAllHiddenStates();
                yield return new WaitForSeconds(GetEnemyDelay(0.4f));
                continue;
            }
        }

        EnemyVisionDetector.RefreshAllHiddenStates();
        StartPlayerTurn();
    }
    
    private GridUnit GetBestTargetForEnemy(GridUnit enemyUnit)
    {
        if (enemyUnit == null)
            return null;

        EnemyVisionDetector detector = enemyUnit.GetComponent<EnemyVisionDetector>();
        EnemyController controller = enemyUnit.GetComponent<EnemyController>();
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);
        GridUnit closestVisibleTarget = null;
        int closestVisibleDistance = int.MaxValue;

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null || unit.Team != UnitTeam.Player || unit.IsDead || unit.CurrentTile == null)
                continue;

            HiddenStateComponent hiddenState = unit.GetComponent<HiddenStateComponent>();
            bool isInsideBarrel = hiddenState != null && hiddenState.CurrentBarrel != null;

            bool isVisible = false;
            if (detector != null)
            {
                if (isInsideBarrel)
                {
                    bool barrelVisible = detector.CanSeeBarrel(hiddenState.CurrentBarrel);
                    bool barrelCarrierKnown =
                        hiddenState != null &&
                        (!hiddenState.IsHidden || hiddenState.BarrelKnownToEnemies);

                    if (barrelVisible && barrelCarrierKnown && !hiddenState.IsHidden && controller != null)
                        controller.RememberTarget(unit);

                    isVisible = false;
                }
                else
                {
                    isVisible = detector.CanSeeUnit(unit);
                }
            }

            int distance =
                Mathf.Abs(enemyUnit.CurrentTile.X - unit.CurrentTile.X) +
                Mathf.Abs(enemyUnit.CurrentTile.Y - unit.CurrentTile.Y);

            if (isVisible && distance < closestVisibleDistance)
            {
                closestVisibleDistance = distance;
                closestVisibleTarget = unit;
                if (controller != null)
                    controller.RememberTarget(unit);
                continue;
            }
        }

        return closestVisibleTarget;
    }

    private GridUnit[] GetLivingEnemies()
    {
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);
        System.Collections.Generic.List<GridUnit> livingEnemies = new System.Collections.Generic.List<GridUnit>();

        foreach (GridUnit unit in allUnits)
        {
            if (unit != null && unit.Team == UnitTeam.Enemy && !unit.IsDead)
                livingEnemies.Add(unit);
        }

        return livingEnemies.ToArray();
    }

    private void RefreshTurnUI()
    {
        if (turnText == null)
            return;

        switch (CurrentTurn)
        {
            case TurnState.PlayerTurn:
                turnText.text = "Player Turn";
                break;

            case TurnState.EnemyTurn:
                turnText.text = "Enemy Turn";
                break;

            case TurnState.Busy:
                turnText.text = "Busy...";
                break;
        }
    }
    public bool AreAllPlayerUnitsDone()
    {
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null || unit.IsDead)
                continue;

            if (unit.Team != UnitTeam.Player)
                continue;

            if (unit.CanMoveThisTurn() || unit.CanAttackThisTurn())
                return false;
        }

        return true;
    }
    
    public bool AreAllEnemyUnitsDone()
    {
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null || unit.IsDead)
                continue;

            if (unit.Team != UnitTeam.Enemy)
                continue;

            if (unit.CanMoveThisTurn() || unit.CanAttackThisTurn())
                return false;
        }

        return true;
    }
}
