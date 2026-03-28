using TMPro;
using UnityEngine;

public class BattleStateManager : MonoBehaviour
{
    public static BattleStateManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI resultText;

    private bool battleEnded = false;

    public bool BattleEnded => battleEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (resultText != null)
            resultText.text = "";
    }

    public void CheckBattleState()
    {
        if (battleEnded)
            return;

        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        bool hasPlayer = false;
        bool hasEnemy = false;

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null)
                continue;

            if (unit.Team == UnitTeam.Player)
                hasPlayer = true;
            else if (unit.Team == UnitTeam.Enemy)
                hasEnemy = true;
        }

        if (!hasEnemy)
            EndBattle("Victory!");
        else if (!hasPlayer)
            EndBattle("Defeat!");
    }

    private void EndBattle(string result)
    {
        battleEnded = true;

        if (resultText != null)
            resultText.text = result;

        Debug.Log(result);
    }
}