using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private ScrollRect levelScrollRect;
    [SerializeField] private bool selectConfirmAfterLevelSelection = true;

    [Header("Controller Navigation")]
    [SerializeField] private bool configureExplicitControllerNavigation = true;
    [SerializeField] private bool restrictNavigationToVisibleLevelButtons = true;
    [SerializeField] private float visibleButtonPadding = 8f;
    [SerializeField] private float rightStickScrollSpeed = 0.8f;
    [SerializeField] private float rightStickDeadzone = 0.2f;

    [Header("Button Colors")]
    [SerializeField] private Color selectedLevelButtonTextColor = Color.yellow;
    [SerializeField] private Color normalLevelButtonTextColor = Color.white;

    private Button currentSelectedLevelButton;
    private string selectedLevelFileName;
    private readonly List<Button> levelButtons = new List<Button>();

    private void Start()
    {
        RefreshLevelListUI();
        UpdateSelectedLevelText();
        RefreshConfirmButtonState();
        ConfigureControllerNavigation();
        DisableScrollbarNavigation();
    }

    private void Update()
    {
        HandleRightStickScroll();

        if (configureExplicitControllerNavigation)
            ConfigureControllerNavigation();
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
        levelButtons.Clear();

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
                levelButtons.Add(button);
            }
        }

        UpdateSelectedLevelText();
        RefreshConfirmButtonState();
        ConfigureControllerNavigation();
        DisableScrollbarNavigation();
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
        ConfigureControllerNavigation();
        SelectConfirmButtonIfReady();
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

    private void SelectConfirmButtonIfReady()
    {
        if (!ControllerInputModeTracker.IsControllerMode ||
            !selectConfirmAfterLevelSelection ||
            confirmButton == null ||
            !confirmButton.interactable ||
            EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
    }

    private void HandleRightStickScroll()
    {
        if (levelScrollRect == null || Gamepad.current == null)
            return;

        Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
        if (Mathf.Abs(rightStick.y) < rightStickDeadzone)
            return;

        ControllerInputModeTracker.NotifyControllerInput();

        levelScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            levelScrollRect.verticalNormalizedPosition + rightStick.y * rightStickScrollSpeed * Time.unscaledDeltaTime);
    }

    private void ConfigureControllerNavigation()
    {
        if (!configureExplicitControllerNavigation)
            return;

        List<Button> navigableLevelButtons = GetNavigableLevelButtons();

        for (int i = 0; i < navigableLevelButtons.Count; i++)
        {
            Button previousButton = i > 0 ? navigableLevelButtons[i - 1] : mainMenuButton;
            Button nextButton = i < navigableLevelButtons.Count - 1
                ? navigableLevelButtons[i + 1]
                : GetConfirmOrMainMenuButton();

            SetExplicitNavigation(
                navigableLevelButtons[i],
                previousButton,
                nextButton,
                mainMenuButton,
                GetConfirmOrMainMenuButton());
        }

        Button firstLevelButton = navigableLevelButtons.Count > 0 ? navigableLevelButtons[0] : null;
        Button lastLevelButton = navigableLevelButtons.Count > 0 ? navigableLevelButtons[navigableLevelButtons.Count - 1] : null;

        SetExplicitNavigation(
            mainMenuButton,
            GetConfirmOrFirstLevelButton(firstLevelButton),
            firstLevelButton,
            null,
            firstLevelButton);

        SetExplicitNavigation(
            confirmButton,
            lastLevelButton != null ? lastLevelButton : mainMenuButton,
            mainMenuButton,
            lastLevelButton,
            null);

        ReselectIfCurrentSelectionIsHidden(navigableLevelButtons);
    }

    private List<Button> GetNavigableLevelButtons()
    {
        List<Button> result = new List<Button>();

        for (int i = 0; i < levelButtons.Count; i++)
        {
            Button button = levelButtons[i];
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                continue;

            if (restrictNavigationToVisibleLevelButtons && !IsButtonVisibleInScrollView(button))
                continue;

            result.Add(button);
        }

        return result;
    }

    private bool IsButtonVisibleInScrollView(Button button)
    {
        if (levelScrollRect == null || levelScrollRect.viewport == null)
            return true;

        RectTransform buttonRect = button.transform as RectTransform;
        RectTransform viewportRect = levelScrollRect.viewport;
        if (buttonRect == null)
            return true;

        Vector3[] buttonCorners = new Vector3[4];
        Vector3[] viewportCorners = new Vector3[4];
        buttonRect.GetWorldCorners(buttonCorners);
        viewportRect.GetWorldCorners(viewportCorners);

        float buttonMinY = buttonCorners[0].y;
        float buttonMaxY = buttonCorners[1].y;
        float viewportMinY = viewportCorners[0].y - visibleButtonPadding;
        float viewportMaxY = viewportCorners[1].y + visibleButtonPadding;

        return buttonMaxY >= viewportMinY && buttonMinY <= viewportMaxY;
    }

    private void ReselectIfCurrentSelectionIsHidden(List<Button> navigableLevelButtons)
    {
        if (EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
            return;

        Button selectedButton = selectedObject.GetComponent<Button>();
        if (selectedButton == null || !levelButtons.Contains(selectedButton))
            return;

        if (navigableLevelButtons.Contains(selectedButton))
            return;

        Button fallback = GetConfirmOrMainMenuButton();
        if (fallback != null)
            EventSystem.current.SetSelectedGameObject(fallback.gameObject);
    }

    private Button GetConfirmOrMainMenuButton()
    {
        return confirmButton != null && confirmButton.interactable
            ? confirmButton
            : mainMenuButton;
    }

    private Button GetConfirmOrFirstLevelButton(Button firstLevelButton)
    {
        return confirmButton != null && confirmButton.interactable
            ? confirmButton
            : firstLevelButton;
    }

    private void SetExplicitNavigation(Button button, Selectable up, Selectable down, Selectable left, Selectable right)
    {
        if (button == null)
            return;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnUp = up;
        navigation.selectOnDown = down;
        navigation.selectOnLeft = left;
        navigation.selectOnRight = right;
        button.navigation = navigation;
    }

    private void DisableScrollbarNavigation()
    {
        if (levelScrollRect == null)
            return;

        DisableSelectableNavigation(levelScrollRect.verticalScrollbar);
        DisableSelectableNavigation(levelScrollRect.horizontalScrollbar);
    }

    private void DisableSelectableNavigation(Selectable selectable)
    {
        if (selectable == null)
            return;

        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.None;
        selectable.navigation = navigation;
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
