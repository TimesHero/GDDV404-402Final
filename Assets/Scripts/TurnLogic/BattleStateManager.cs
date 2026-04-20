using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleStateManager : MonoBehaviour
{
    [SerializeField] private TileSelector tileSelector;
    [SerializeField] private LevelObjectiveRuntimeManager objectiveRuntimeManager;

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

        if (objectiveRuntimeManager != null)
            objectiveRuntimeManager.OnUnitDied(deadUnit);

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

        if (objectiveRuntimeManager != null)
        {
            objectiveRuntimeManager.EvaluateObjectives();

            if (battleEnded)
                return;
        }

        GridUnit[] allUnits = FindObjectsByType<GridUnit>(FindObjectsSortMode.None);

        bool hasPlayer = false;

        foreach (GridUnit unit in allUnits)
        {
            if (unit == null)
                continue;

            if (!unit.gameObject.activeInHierarchy)
                continue;

            if (unit.Team == UnitTeam.Player)
                hasPlayer = true;
        }

        if (!hasPlayer)
            EndBattle("You Lose");
    }

    public void EndBattleExternally(string result)
    {
        if (battleEnded)
            return;

        EndBattle(result);
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