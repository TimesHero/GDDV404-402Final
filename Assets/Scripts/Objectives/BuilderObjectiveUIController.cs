using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class BuilderObjectiveUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelObjectiveRegistry objectiveRegistry;
    [SerializeField] private BuilderInputController builderInputController;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown objectiveTypeDropdown;
    [SerializeField] private TMP_InputField surviveTurnsInputField;
    [SerializeField] private TMP_Text currentObjectiveText;
    [SerializeField] private TMP_Text reachTilePreviewText;
    [SerializeField] private UnityEngine.UI.Image selectionModeButtonImage;
    [SerializeField] private Color selectionModeNormalColor = Color.white;
    [SerializeField] private Color selectionModeSelectedColor = Color.green;

    private readonly List<Vector2Int> stagedReachTiles = new List<Vector2Int>();
    private bool reachTileSelectionModeEnabled;

    private void Start()
    {
        PopulateObjectiveDropdown();
        LoadFromRegistryIntoUI();
        RefreshAllUI();
    }
    private void Update()
    {
        if (GetSelectedWinConditionType() == WinConditionType.ReachTile)
            RefreshReachTilePreview();
    }

    private void PopulateObjectiveDropdown()
    {
        if (objectiveTypeDropdown == null)
            return;

        objectiveTypeDropdown.ClearOptions();

        List<string> options = new List<string>
        {
            WinConditionType.KillAllEnemies.ToString(),
            WinConditionType.SurviveTurns.ToString(),
            WinConditionType.ReachTile.ToString()
        };

        objectiveTypeDropdown.AddOptions(options);
    }

    public void AddSelectedReachTile()
    {
        if (GetSelectedWinConditionType() != WinConditionType.ReachTile)
            return;

        if (builderInputController == null || builderInputController.SelectedObjectiveTile == null)
        {
            Debug.LogWarning("BuilderObjectiveUIController: Select a target tile first.");
            return;
        }

        Vector2Int tilePos = builderInputController.SelectedObjectiveTile.GridPosition;

        if (!stagedReachTiles.Contains(tilePos))
            stagedReachTiles.Add(tilePos);

        RefreshAllUI();
    }

    public void RemoveLastReachTile()
    {
        if (stagedReachTiles.Count > 0)
            stagedReachTiles.RemoveAt(stagedReachTiles.Count - 1);

        RefreshAllUI();
    }

    public void ClearStagedReachTiles()
    {
        stagedReachTiles.Clear();

        if (builderInputController != null)
            builderInputController.ClearSelectedObjectiveTile();

        RefreshAllUI();
    }

    public void ApplyObjectiveFromUI()
    {
        if (objectiveRegistry == null)
        {
            Debug.LogWarning("BuilderObjectiveUIController: ObjectiveRegistry is missing.");
            return;
        }

        List<LevelObjectiveData> objectives = new List<LevelObjectiveData>();

        WinConditionType selectedType = GetSelectedWinConditionType();

        LevelObjectiveData objective = new LevelObjectiveData
        {
            winConditionType = selectedType
        };

        if (selectedType == WinConditionType.SurviveTurns)
        {
            int surviveTurns = 1;

            if (surviveTurnsInputField != null && !string.IsNullOrWhiteSpace(surviveTurnsInputField.text))
                int.TryParse(surviveTurnsInputField.text, out surviveTurns);

            objective.surviveTurnCount = Mathf.Max(1, surviveTurns);
        }
        else if (selectedType == WinConditionType.ReachTile)
        {
            if (stagedReachTiles.Count == 0)
            {
                Debug.LogWarning("BuilderObjectiveUIController: Add at least one reach tile before applying.");
                return;
            }

            objective.targetGridPositions = new List<Vector2Int>(stagedReachTiles);
        }

        objectives.Add(objective);
        objectiveRegistry.SetObjectives(objectives);

        RefreshAllUI();
    }

    public void ClearObjectives()
    {
        if (objectiveRegistry == null)
            return;

        objectiveRegistry.ClearObjectives();
        stagedReachTiles.Clear();

        if (builderInputController != null)
        {
            builderInputController.ClearSelectedObjectiveTile();
            builderInputController.SetObjectiveTilePickMode(false);
        }

        RefreshAllUI();
    }

    public void OnObjectiveTypeChanged(int index)
    {
        if (GetSelectedWinConditionType() != WinConditionType.ReachTile)
        {
            reachTileSelectionModeEnabled = false;

            if (builderInputController != null)
                builderInputController.SetObjectiveTilePickMode(false);
        }

        RefreshSelectionModeButtonVisual();
        RefreshAllUI();
    }
    
    public void ToggleReachTileSelectionMode()
    {
        if (GetSelectedWinConditionType() != WinConditionType.ReachTile)
        {
            Debug.LogWarning("BuilderObjectiveUIController: Selection Mode only works when ReachTile is selected.");
            return;
        }

        reachTileSelectionModeEnabled = !reachTileSelectionModeEnabled;

        if (builderInputController != null)
            builderInputController.SetObjectiveTilePickMode(reachTileSelectionModeEnabled);

        RefreshSelectionModeButtonVisual();
        RefreshAllUI();
    }

    private void LoadFromRegistryIntoUI()
    {
        stagedReachTiles.Clear();

        if (objectiveRegistry == null)
            return;

        List<LevelObjectiveData> objectives = objectiveRegistry.GetObjectives();
        if (objectives == null || objectives.Count == 0 || objectives[0] == null)
            return;

        LevelObjectiveData objective = objectives[0];

        if (objective.winConditionType == WinConditionType.SurviveTurns && surviveTurnsInputField != null)
            surviveTurnsInputField.text = objective.surviveTurnCount.ToString();

        if (objective.winConditionType == WinConditionType.ReachTile && objective.targetGridPositions != null)
            stagedReachTiles.AddRange(objective.targetGridPositions);

        SetDropdownToType(objective.winConditionType);
    }

    private void RefreshAllUI()
    {
        RefreshInputVisibility();
        RefreshCurrentObjectiveText();
        RefreshReachTilePreview();
        RefreshSelectionModeButtonVisual();
    }

    private void RefreshCurrentObjectiveText()
    {
        if (currentObjectiveText == null)
            return;

        if (objectiveRegistry == null)
        {
            currentObjectiveText.text = "Objective: None";
            return;
        }

        List<LevelObjectiveData> objectives = objectiveRegistry.GetObjectives();
        if (objectives == null || objectives.Count == 0 || objectives[0] == null)
        {
            currentObjectiveText.text = "Objective: None";
            return;
        }

        LevelObjectiveData objective = objectives[0];

        switch (objective.winConditionType)
        {
            case WinConditionType.KillAllEnemies:
                currentObjectiveText.text = "Objective: Kill All Enemies";
                break;

            case WinConditionType.SurviveTurns:
                currentObjectiveText.text = $"Objective: Survive {objective.surviveTurnCount} Turns";
                break;

            case WinConditionType.ReachTile:
                currentObjectiveText.text = $"Objective: Reach Tiles ({objective.targetGridPositions.Count})";
                break;

            default:
                currentObjectiveText.text = "Objective: None";
                break;
        }
    }

    private void RefreshInputVisibility()
    {
        WinConditionType selectedType = GetSelectedWinConditionType();

        if (surviveTurnsInputField != null)
            surviveTurnsInputField.gameObject.SetActive(selectedType == WinConditionType.SurviveTurns);

        if (reachTilePreviewText != null)
            reachTilePreviewText.gameObject.SetActive(selectedType == WinConditionType.ReachTile);
    }

    private void RefreshReachTilePreview()
    {
        if (reachTilePreviewText == null)
            return;

        if (GetSelectedWinConditionType() != WinConditionType.ReachTile)
        {
            reachTilePreviewText.text = "";
            return;
        }

        StringBuilder sb = new StringBuilder();

        string hoveredText = "Hover: None";
        string selectedText = "Selected: None";

        if (builderInputController != null)
        {
            if (builderInputController.CurrentHoveredTile != null)
            {
                Vector2Int hoveredPos = builderInputController.CurrentHoveredTile.GridPosition;
                hoveredText = $"Hover: ({hoveredPos.x}, {hoveredPos.y})";
            }

            if (builderInputController.SelectedObjectiveTile != null)
            {
                Vector2Int selectedPos = builderInputController.SelectedObjectiveTile.GridPosition;
                selectedText = $"Selected: ({selectedPos.x}, {selectedPos.y})";
            }
        }

        sb.AppendLine(hoveredText);
        sb.AppendLine(selectedText);
        sb.AppendLine($"Staged Zones: {stagedReachTiles.Count}");

        for (int i = 0; i < stagedReachTiles.Count; i++)
        {
            Vector2Int tilePos = stagedReachTiles[i];
            sb.AppendLine($"[{i + 1}] ({tilePos.x}, {tilePos.y})");
        }

        reachTilePreviewText.text = sb.ToString();
    }

    private WinConditionType GetSelectedWinConditionType()
    {
        if (objectiveTypeDropdown == null)
            return WinConditionType.KillAllEnemies;

        string selectedText = objectiveTypeDropdown.options[objectiveTypeDropdown.value].text;

        if (selectedText == WinConditionType.SurviveTurns.ToString())
            return WinConditionType.SurviveTurns;

        if (selectedText == WinConditionType.ReachTile.ToString())
            return WinConditionType.ReachTile;

        return WinConditionType.KillAllEnemies;
    }

    private void SetDropdownToType(WinConditionType type)
    {
        if (objectiveTypeDropdown == null)
            return;

        for (int i = 0; i < objectiveTypeDropdown.options.Count; i++)
        {
            if (objectiveTypeDropdown.options[i].text == type.ToString())
            {
                objectiveTypeDropdown.SetValueWithoutNotify(i);
                break;
            }
        }
    }
    private void RefreshSelectionModeButtonVisual()
    {
        if (selectionModeButtonImage == null)
            return;

        bool canUseSelectionMode = GetSelectedWinConditionType() == WinConditionType.ReachTile;
        bool isSelected = canUseSelectionMode && reachTileSelectionModeEnabled;

        selectionModeButtonImage.color = isSelected
            ? selectionModeSelectedColor
            : selectionModeNormalColor;
    }
}