using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Toggle loseWhenSeenToggle;
    [SerializeField] private UnityEngine.UI.Image selectionModeButtonImage;
    [SerializeField] private Color selectionModeNormalColor = Color.white;
    [SerializeField] private Color selectionModeSelectedColor = Color.green;

    [Header("Objective List UI")]
    [SerializeField] private Transform objectiveListContainer;
    [SerializeField] private GameObject objectiveListButtonPrefab;
    [SerializeField] private Button removeSelectedObjectiveButton;
    [SerializeField] private Color selectedObjectiveButtonTextColor = Color.yellow;
    [SerializeField] private Color normalObjectiveButtonTextColor = Color.white;

    private readonly List<Vector2Int> stagedReachTiles = new List<Vector2Int>();
    private bool reachTileSelectionModeEnabled;
    private int selectedObjectiveIndex = -1;

    private void Start()
    {
        PopulateObjectiveDropdown();
        ClearRegistryForFreshBuilderSession();
        LoadFromRegistryIntoUI();
        RefreshAllUI();
    }
    private void Update()
    {
        if (IsReachObjectiveType(GetSelectedWinConditionType()))
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
            WinConditionType.ReachTile.ToString(),
            WinConditionType.ReachWithoutBeingSeen.ToString()
        };

        objectiveTypeDropdown.AddOptions(options);
    }

    private void ClearRegistryForFreshBuilderSession()
    {
        if (objectiveRegistry == null)
            return;

        objectiveRegistry.ClearObjectives();
        objectiveRegistry.SetLoseWhenSeen(false);
        selectedObjectiveIndex = -1;
        stagedReachTiles.Clear();

        if (loseWhenSeenToggle != null)
            loseWhenSeenToggle.SetIsOnWithoutNotify(false);
    }

    private int FindReplaceableObjectiveIndex(List<LevelObjectiveData> objectives, WinConditionType newType)
    {
        if (objectives == null)
            return -1;

        for (int i = 0; i < objectives.Count; i++)
        {
            LevelObjectiveData existingObjective = objectives[i];
            if (existingObjective == null)
                continue;

            if (AreEquivalentObjectiveSlots(existingObjective.winConditionType, newType))
                return i;
        }

        return -1;
    }

    private bool AreEquivalentObjectiveSlots(WinConditionType existingType, WinConditionType newType)
    {
        if (IsReachObjectiveType(existingType) && IsReachObjectiveType(newType))
            return true;

        return existingType == newType;
    }

    public void AddSelectedReachTile()
    {
        if (!IsReachObjectiveType(GetSelectedWinConditionType()))
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

        if (!TryBuildObjectiveFromCurrentUI(out LevelObjectiveData objective))
            return;

        List<LevelObjectiveData> objectives = objectiveRegistry.GetObjectives();
        objectives = objectives != null
            ? new List<LevelObjectiveData>(objectives)
            : new List<LevelObjectiveData>();

        int replacementIndex = FindReplaceableObjectiveIndex(objectives, objective.winConditionType);
        if (replacementIndex >= 0)
        {
            objectives[replacementIndex] = objective;
            selectedObjectiveIndex = replacementIndex;
        }
        else
        {
            objectives.Add(objective);
            selectedObjectiveIndex = objectives.Count - 1;
        }

        objectiveRegistry.SetObjectives(objectives);

        RefreshAllUI();
    }

    public void RemoveSelectedObjective()
    {
        if (objectiveRegistry == null)
            return;

        List<LevelObjectiveData> objectives = objectiveRegistry.GetObjectives();
        if (objectives == null || selectedObjectiveIndex < 0 || selectedObjectiveIndex >= objectives.Count)
        {
            Debug.LogWarning("BuilderObjectiveUIController: Select a win condition from the list before removing.");
            return;
        }

        objectives = new List<LevelObjectiveData>(objectives);
        objectives.RemoveAt(selectedObjectiveIndex);
        objectiveRegistry.SetObjectives(objectives);

        if (objectives.Count == 0)
        {
            selectedObjectiveIndex = -1;
        }
        else
        {
            selectedObjectiveIndex = Mathf.Clamp(selectedObjectiveIndex, 0, objectives.Count - 1);
            LoadObjectiveIntoEditorFields(objectives[selectedObjectiveIndex]);
        }

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

    public void SetLoseWhenSeen(bool isEnabled)
    {
        if (objectiveRegistry == null)
            return;

        objectiveRegistry.SetLoseWhenSeen(isEnabled);
        RefreshAllUI();
    }

    public void RefreshFromRegistry()
    {
        LoadFromRegistryIntoUI();
        RefreshAllUI();
    }

    public void OnObjectiveTypeChanged(int index)
    {
        if (!IsReachObjectiveType(GetSelectedWinConditionType()))
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
        if (!IsReachObjectiveType(GetSelectedWinConditionType()))
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

        if (loseWhenSeenToggle != null)
            loseWhenSeenToggle.SetIsOnWithoutNotify(objectiveRegistry.LoseWhenSeen);

        List<LevelObjectiveData> objectives = objectiveRegistry.GetObjectives();
        if (objectives == null || objectives.Count == 0 || objectives[0] == null)
        {
            selectedObjectiveIndex = -1;
            return;
        }

        selectedObjectiveIndex = Mathf.Clamp(selectedObjectiveIndex, 0, objectives.Count - 1);
        LoadObjectiveIntoEditorFields(objectives[selectedObjectiveIndex]);
    }

    private void LoadObjectiveIntoEditorFields(LevelObjectiveData objective)
    {
        stagedReachTiles.Clear();

        if (objective == null)
            return;

        if (objective.winConditionType == WinConditionType.SurviveTurns && surviveTurnsInputField != null)
            surviveTurnsInputField.text = objective.surviveTurnCount.ToString();

        if (IsReachObjectiveType(objective.winConditionType) && objective.targetGridPositions != null)
            stagedReachTiles.AddRange(objective.targetGridPositions);

        SetDropdownToType(objective.winConditionType);
    }

    private void RefreshAllUI()
    {
        RefreshInputVisibility();
        RefreshCurrentObjectiveText();
        RefreshObjectiveListUI();
        RefreshReachTilePreview();
        RefreshSelectionModeButtonVisual();
        RefreshRemoveButtonState();
    }

    private void RefreshCurrentObjectiveText()
    {
        if (currentObjectiveText == null)
            return;

        if (objectiveRegistry == null)
        {
            currentObjectiveText.text = "Objectives: None";
            return;
        }

        List<LevelObjectiveData> objectives = objectiveRegistry.GetObjectives();
        if (objectives == null || objectives.Count == 0)
        {
            currentObjectiveText.text = "Objectives: None";
            return;
        }

        string selectedText = selectedObjectiveIndex >= 0 && selectedObjectiveIndex < objectives.Count
            ? GetObjectiveSummary(objectives[selectedObjectiveIndex])
            : "None";

        currentObjectiveText.text = $"Objectives Assigned: {objectives.Count}\nSelected: {selectedText}";
    }

    private void RefreshObjectiveListUI()
    {
        if (objectiveListContainer == null || objectiveListButtonPrefab == null)
            return;

        for (int i = objectiveListContainer.childCount - 1; i >= 0; i--)
            Destroy(objectiveListContainer.GetChild(i).gameObject);

        if (objectiveRegistry == null)
            return;

        List<LevelObjectiveData> objectives = objectiveRegistry.GetObjectives();
        if (objectives == null)
            return;

        if (selectedObjectiveIndex >= objectives.Count)
            selectedObjectiveIndex = objectives.Count - 1;

        for (int i = 0; i < objectives.Count; i++)
        {
            LevelObjectiveData objective = objectives[i];
            GameObject buttonObject = Instantiate(objectiveListButtonPrefab, objectiveListContainer);

            TMP_Text buttonText = buttonObject.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = $"{i + 1}. {GetObjectiveSummary(objective)}";
                buttonText.color = i == selectedObjectiveIndex
                    ? selectedObjectiveButtonTextColor
                    : normalObjectiveButtonTextColor;
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                int capturedIndex = i;
                button.onClick.AddListener(() => SelectObjectiveFromList(capturedIndex));
            }
        }
    }

    private void SelectObjectiveFromList(int objectiveIndex)
    {
        if (objectiveRegistry == null)
            return;

        List<LevelObjectiveData> objectives = objectiveRegistry.GetObjectives();
        if (objectives == null || objectiveIndex < 0 || objectiveIndex >= objectives.Count)
            return;

        selectedObjectiveIndex = objectiveIndex;
        LoadObjectiveIntoEditorFields(objectives[selectedObjectiveIndex]);
        RefreshAllUI();
    }

    private void RefreshRemoveButtonState()
    {
        if (removeSelectedObjectiveButton == null)
            return;

        List<LevelObjectiveData> objectives = objectiveRegistry != null
            ? objectiveRegistry.GetObjectives()
            : null;

        removeSelectedObjectiveButton.interactable =
            objectives != null &&
            selectedObjectiveIndex >= 0 &&
            selectedObjectiveIndex < objectives.Count;
    }

    private void RefreshInputVisibility()
    {
        WinConditionType selectedType = GetSelectedWinConditionType();

        if (surviveTurnsInputField != null)
            surviveTurnsInputField.gameObject.SetActive(selectedType == WinConditionType.SurviveTurns);

        if (reachTilePreviewText != null)
            reachTilePreviewText.gameObject.SetActive(IsReachObjectiveType(selectedType));
    }

    private void RefreshReachTilePreview()
    {
        if (reachTilePreviewText == null)
            return;

        if (!IsReachObjectiveType(GetSelectedWinConditionType()))
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

        if (selectedText == WinConditionType.ReachWithoutBeingSeen.ToString())
            return WinConditionType.ReachWithoutBeingSeen;

        return WinConditionType.KillAllEnemies;
    }

    private bool TryBuildObjectiveFromCurrentUI(out LevelObjectiveData objective)
    {
        WinConditionType selectedType = GetSelectedWinConditionType();

        objective = new LevelObjectiveData
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
        else if (IsReachObjectiveType(selectedType))
        {
            if (stagedReachTiles.Count == 0)
            {
                Debug.LogWarning("BuilderObjectiveUIController: Add at least one reach tile before applying.");
                return false;
            }

            objective.targetGridPositions = new List<Vector2Int>(stagedReachTiles);
        }

        return true;
    }

    private string GetObjectiveSummary(LevelObjectiveData objective)
    {
        if (objective == null)
            return "None";

        switch (objective.winConditionType)
        {
            case WinConditionType.KillAllEnemies:
                return "Kill All Enemies";

            case WinConditionType.SurviveTurns:
                return $"Survive {Mathf.Max(1, objective.surviveTurnCount)} Turns";

            case WinConditionType.ReachTile:
                return $"Reach Tiles ({GetReachTileCount(objective)})";

            case WinConditionType.ReachWithoutBeingSeen:
                return $"Reach Without Being Seen ({GetReachTileCount(objective)})";

            default:
                return objective.winConditionType.ToString();
        }
    }

    private int GetReachTileCount(LevelObjectiveData objective)
    {
        return objective != null && objective.targetGridPositions != null
            ? objective.targetGridPositions.Count
            : 0;
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

        bool canUseSelectionMode = IsReachObjectiveType(GetSelectedWinConditionType());
        bool isSelected = canUseSelectionMode && reachTileSelectionModeEnabled;

        selectionModeButtonImage.color = isSelected
            ? selectionModeSelectedColor
            : selectionModeNormalColor;
    }

    private bool IsReachObjectiveType(WinConditionType type)
    {
        return type == WinConditionType.ReachTile ||
               type == WinConditionType.ReachWithoutBeingSeen;
    }
}
