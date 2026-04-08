using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    
    [SerializeField] private TileSelector tileSelector;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI playerHintText;
    
    [Header("Battle References")]
    [SerializeField] private UnitSpawner playerSpawner;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private float enemyTurnDelay = 0.75f;

    private InputSystem_Actions inputActions;

    public TurnState CurrentTurn { get; private set; } = TurnState.PlayerTurn;

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
                unit.ResetTurnState();
        }

        Debug.Log("Turn State: Player Turn");
    }
    public void ReturnToPlayerControl()
    {
        CurrentTurn = TurnState.PlayerTurn;
        RefreshTurnUI();
        Debug.Log("Turn State: Player Control Restored");
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
                unit.ResetTurnState();
        }

        Debug.Log("Turn State: Enemy Turn");

        StartCoroutine(RunEnemyTurnRoutine());
    }

    public void EndTurn()
    {
        if (CurrentTurn == TurnState.PlayerTurn)
            StartEnemyTurn();
        else if (CurrentTurn == TurnState.EnemyTurn)
            StartPlayerTurn();
    }
    
    private IEnumerator RunEnemyTurnRoutine()
    {
        Debug.Log("Enemy turn routine started.");

        CurrentTurn = TurnState.Busy;
        RefreshTurnUI();

        yield return new WaitForSeconds(enemyTurnDelay);

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

            GridUnit playerTarget = GetFirstLivingPlayer();

            if (playerTarget == null)
            {
                Debug.LogWarning("No living player targets found. Returning to player turn.");
                StartPlayerTurn();
                yield break;
            }

            EnemyController controller = enemy.GetComponent<EnemyController>();

            if (controller == null)
            {
                Debug.LogWarning($"{enemy.name} has no EnemyController. Skipping.");
                continue;
            }

            bool acted = controller.TryAct(playerTarget);
            Debug.Log($"{enemy.name} TryAct result: {acted}");

            if (!acted)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            if (!controller.LastActionWasMovement)
            {
                yield return new WaitForSeconds(0.4f);
                continue;
            }

            bool finished = false;

            void OnFinished(GridUnit u)
            {
                Debug.Log($"{enemy.name} movement finished event received.");
                finished = true;
            }

            enemy.OnMovementFinished += OnFinished;

            while (!finished)
                yield return null;

            enemy.OnMovementFinished -= OnFinished;

            yield return new WaitForSeconds(0.25f);
        }

        StartPlayerTurn();
    }
    
    private GridUnit GetFirstLivingPlayer()
    {
        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        foreach (GridUnit unit in allUnits)
        {
            if (unit != null && unit.Team == UnitTeam.Player && !unit.IsDead)
                return unit;
        }

        return null;
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