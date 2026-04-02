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

    public void SetBusy()
    {
        CurrentTurn = TurnState.Busy;
        RefreshTurnUI();
        Debug.Log("Turn State: Busy");
    }

    public void StartPlayerTurn()
    {
        CurrentTurn = TurnState.PlayerTurn;
        RefreshTurnUI();

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

        GridUnit player = playerSpawner != null ? playerSpawner.SpawnedUnit : null;
        GridUnit enemy = enemySpawner != null ? enemySpawner.SpawnedEnemy : null;

        Debug.Log($"Player found: {player != null}");
        Debug.Log($"Enemy found: {enemy != null}");

        if (player == null || enemy == null)
        {
            Debug.LogWarning("Missing player or enemy. Returning to player turn.");
            StartPlayerTurn();
            yield break;
        }

        EnemyController controller = enemy.GetComponent<EnemyController>();
        Debug.Log($"EnemyController found: {controller != null}");

        if (controller == null)
        {
            Debug.LogWarning("Enemy has no EnemyController. Returning to player turn.");
            StartPlayerTurn();
            yield break;
        }

        bool acted = controller.TryAct(player);
        Debug.Log($"Enemy TryAct result: {acted}");

        if (!acted)
        {
            yield return new WaitForSeconds(0.25f);
            StartPlayerTurn();
            yield break;
        }
        
        if (!controller.LastActionWasMovement)
        {
            yield return new WaitForSeconds(0.4f);
            StartPlayerTurn();
            yield break;
        }

        bool finished = false;

        void OnFinished(GridUnit u)
        {
            Debug.Log("Enemy movement finished event received.");
            finished = true;
        }

        enemy.OnMovementFinished += OnFinished;

        while (!finished)
            yield return null;

        enemy.OnMovementFinished -= OnFinished;

        yield return new WaitForSeconds(0.25f);
        StartPlayerTurn();
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
}