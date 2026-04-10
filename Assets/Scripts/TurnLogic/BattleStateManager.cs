using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleStateManager : MonoBehaviour
{
    [SerializeField] private TileSelector tileSelector;
    public static BattleStateManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button restartButton;

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

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartScene);
        }
    }

    public void NotifyUnitDied(GridUnit deadUnit)
    {
        if (battleEnded)
            return;

        StartCoroutine(CheckBattleStateNextFrame());
    }

    private IEnumerator CheckBattleStateNextFrame()
    {
        yield return null;
        CheckBattleState();
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

            if (!unit.gameObject.activeInHierarchy)
                continue;

            if (unit.Team == UnitTeam.Player)
                hasPlayer = true;
            else if (unit.Team == UnitTeam.Enemy)
                hasEnemy = true;
        }

        if (!hasEnemy)
            EndBattle("You Win");
        else if (!hasPlayer)
            EndBattle("You Lose");
    }

    private void EndBattle(string result)
    {
        if (tileSelector != null)
            tileSelector.ForceClearSelectionAndHighlights();
        battleEnded = true;

        if (resultText != null)
            resultText.text = result;

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        Debug.Log(result);
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void ResetBattleState()
    {
        battleEnded = false;

        if (resultText != null)
            resultText.text = "";

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        Debug.Log("Battle state reset.");
    }
}