using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelPickerUIController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("UI References")]
    [SerializeField] private Transform levelListContainer;
    [SerializeField] private GameObject levelListButtonPrefab;
    [SerializeField] private TMP_Text selectedLevelText;
    [SerializeField] private Button confirmButton;

    [Header("Button Colors")]
    [SerializeField] private Color selectedLevelButtonTextColor = Color.yellow;
    [SerializeField] private Color normalLevelButtonTextColor = Color.white;

    private Button currentSelectedLevelButton;
    private string selectedLevelFileName;

    private void Start()
    {
        RefreshLevelListUI();
        UpdateSelectedLevelText();
        RefreshConfirmButtonState();
    }

    public void RefreshLevelListUI()
    {
        if (levelListContainer == null || levelListButtonPrefab == null)
            return;

        for (int i = levelListContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(levelListContainer.GetChild(i).gameObject);
        }

        currentSelectedLevelButton = null;
        selectedLevelFileName = null;

        List<string> fileNames = GetAvailableLevelFileNames();

        foreach (string fileName in fileNames)
        {
            GameObject buttonObject = Instantiate(levelListButtonPrefab, levelListContainer);

            TMP_Text buttonText = buttonObject.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = fileName;

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                string capturedFileName = fileName;
                TMP_Text capturedText = buttonText;

                button.onClick.AddListener(() => SelectLevelFile(capturedFileName, button, capturedText));
            }
        }

        UpdateSelectedLevelText();
        RefreshConfirmButtonState();
    }

    public void SelectLevelFile(string fileName, Button button, TMP_Text buttonText)
    {
        selectedLevelFileName = fileName;

        if (currentSelectedLevelButton != null)
        {
            TMP_Text previousText = currentSelectedLevelButton.GetComponentInChildren<TMP_Text>();
            if (previousText != null)
                previousText.color = normalLevelButtonTextColor;
        }

        currentSelectedLevelButton = button;

        if (buttonText != null)
            buttonText.color = selectedLevelButtonTextColor;

        UpdateSelectedLevelText();
        RefreshConfirmButtonState();
    }

    public void ConfirmSelectedLevel()
    {
        if (string.IsNullOrWhiteSpace(selectedLevelFileName))
        {
            Debug.LogWarning("LevelPickerUIController: No level selected.");
            return;
        }

        SelectedBattleLevel.SetLevel(selectedLevelFileName);
        SceneManager.LoadScene(battleSceneName);
    }

    public void GoToMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("LevelPickerUIController: Main menu scene name is empty.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void UpdateSelectedLevelText()
    {
        if (selectedLevelText == null)
            return;

        selectedLevelText.text = string.IsNullOrWhiteSpace(selectedLevelFileName)
            ? "Selected: None"
            : $"Selected: {selectedLevelFileName}";
    }

    private void RefreshConfirmButtonState()
    {
        if (confirmButton != null)
            confirmButton.interactable = !string.IsNullOrWhiteSpace(selectedLevelFileName);
    }

    private List<string> GetAvailableLevelFileNames()
    {
        List<string> result = new List<string>();

        TextAsset[] levelFiles = Resources.LoadAll<TextAsset>("LevelLayouts");
        foreach (TextAsset levelFile in levelFiles)
        {
            if (levelFile == null)
                continue;

            if (!result.Contains(levelFile.name))
                result.Add(levelFile.name);
        }

        result.Sort();
        return result;
    }
}