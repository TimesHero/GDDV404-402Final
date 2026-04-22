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
    [SerializeField] private GameObject winLosePanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button restartButton;
    [SerializeField] private UICanvasSelectionFrame selectionFrame;
    [SerializeField] private ControllerUINavigationController navigationController;

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

        AutoAssignMissingReferences();
        SetWinLosePanelVisible(false);

        if (resultText != null)
            resultText.text = "";

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(winLosePanel != null);
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartScene);
        }
    }

    private void AutoAssignMissingReferences()
    {
        if (winLosePanel == null)
            winLosePanel = FindChildGameObjectByName(transform.root, "WINLose Panel");

        if (resultText == null && winLosePanel != null)
            resultText = winLosePanel.GetComponentInChildren<TextMeshProUGUI>(true);

        if (restartButton == null && winLosePanel != null)
            restartButton = winLosePanel.GetComponentInChildren<Button>(true);

        if (selectionFrame == null)
            selectionFrame = FindFirstObjectByType<UICanvasSelectionFrame>(FindObjectsInactive.Include);

        if (navigationController == null)
            navigationController = FindFirstObjectByType<ControllerUINavigationController>(FindObjectsInactive.Include);
    }

    private GameObject FindChildGameObjectByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == childName)
                return children[i].gameObject;
        }

        return null;
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

        SetWinLosePanelVisible(true);

        if (navigationController != null)
        {
            navigationController.enabled = true;
            navigationController.RefreshNavigation();
            navigationController.SelectFirstAvailable();
        }

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
            restartButton.gameObject.SetActive(winLosePanel != null);

        SetWinLosePanelVisible(false);

        Debug.Log("Battle state reset.");
    }

    private void SetWinLosePanelVisible(bool visible)
    {
        if (winLosePanel != null)
            winLosePanel.SetActive(visible);

        if (selectionFrame != null)
            selectionFrame.gameObject.SetActive(visible);
    }
}
