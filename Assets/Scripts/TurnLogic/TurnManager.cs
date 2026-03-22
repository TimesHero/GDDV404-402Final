using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI turnText;

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
        Debug.Log("Turn State: Player Turn");
    }

    public void StartEnemyTurn()
    {
        CurrentTurn = TurnState.EnemyTurn;
        RefreshTurnUI();
        Debug.Log("Turn State: Enemy Turn");
    }

    public void EndTurn()
    {
        if (CurrentTurn == TurnState.PlayerTurn)
            StartEnemyTurn();
        else if (CurrentTurn == TurnState.EnemyTurn)
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