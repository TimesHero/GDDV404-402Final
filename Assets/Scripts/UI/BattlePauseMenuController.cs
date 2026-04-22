using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BattlePauseMenuController : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private GameObject pauseUiRoot;
    [SerializeField] private Transform navigationRoot;
    [SerializeField] private Selectable initialSelection;
    [SerializeField] private UICanvasSelectionFrame selectionFrame;
    [SerializeField] private ControllerUINavigationController navigationController;
    [SerializeField] private bool hidePauseUiRootWhenClosed = true;

    [Header("Input")]
    [SerializeField] private bool startButtonTogglesPause = true;
    [SerializeField] private bool eastButtonClosesPause = true;

    public static bool IsPauseMenuOpen { get; private set; }

    private InputSystem_Actions inputActions;
    private InputAction pauseAction;
    private bool previousEastPressed;

    private void Awake()
    {
        AutoAssignMissingReferences();
        inputActions = new InputSystem_Actions();
        pauseAction = inputActions.FindAction("Gameplay/Pause", false);
        SetPauseOpen(false, true);
    }

    private void AutoAssignMissingReferences()
    {
        if (pauseUiRoot == null)
            pauseUiRoot = FindChildGameObjectByName(transform.root, "Pause Panel");

        if (navigationRoot == null && pauseUiRoot != null)
            navigationRoot = pauseUiRoot.transform;

        if (initialSelection == null && navigationRoot != null)
            initialSelection = GetFirstUsableSelectable();

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

    private void OnEnable()
    {
        if (pauseAction == null && inputActions != null)
            pauseAction = inputActions.FindAction("Gameplay/Pause", false);

        if (pauseAction != null)
        {
            pauseAction.performed += OnPausePressed;
            pauseAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePressed;
            pauseAction.Disable();
        }
    }

    private void Update()
    {
        if (BattleStateManager.Instance != null && BattleStateManager.Instance.BattleEnded)
        {
            if (IsPauseMenuOpen)
                SetPauseOpen(false);

            return;
        }

        HandleGamepadInput();
    }

    public void OpenPauseMenu()
    {
        SetPauseOpen(true);
    }

    public void ClosePauseMenu()
    {
        SetPauseOpen(false);
    }

    public void TogglePauseMenu()
    {
        SetPauseOpen(!IsPauseMenuOpen);
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (!startButtonTogglesPause)
            return;

        if (context.control != null && context.control.device is Gamepad)
            ControllerInputModeTracker.NotifyControllerInput();
        else
            ControllerInputModeTracker.NotifyMouseKeyboardInput();

        TogglePauseMenu();
    }

    private void HandleGamepadInput()
    {
        if (Gamepad.current == null)
        {
            previousEastPressed = false;
            return;
        }

        bool eastPressed = Gamepad.current.buttonEast.isPressed;
        if (eastButtonClosesPause && IsPauseMenuOpen && eastPressed && !previousEastPressed)
        {
            ControllerInputModeTracker.NotifyControllerInput();
            SetPauseOpen(false);
        }

        previousEastPressed = eastPressed;
    }

    private void SetPauseOpen(bool open, bool instant = false)
    {
        IsPauseMenuOpen = open;

        if (pauseUiRoot != null && hidePauseUiRootWhenClosed)
            pauseUiRoot.SetActive(open);

        bool resultPanelOpen = IsBattleResultPanelOpen();

        if (selectionFrame != null)
            selectionFrame.gameObject.SetActive(open || resultPanelOpen);

        if (navigationController != null)
            navigationController.enabled = open || resultPanelOpen;

        if (open)
        {
            if (navigationController != null)
                navigationController.RefreshNavigation();

            if (ControllerInputModeTracker.IsControllerMode)
                SelectInitialButton();

            return;
        }

        if (!instant)
            ClearPauseSelection();
    }

    private void SelectInitialButton()
    {
        if (EventSystem.current == null)
            return;

        Selectable selection = IsSelectableUsable(initialSelection)
            ? initialSelection
            : GetFirstUsableSelectable();

        if (selection != null)
            EventSystem.current.SetSelectedGameObject(selection.gameObject);
    }

    private Selectable GetFirstUsableSelectable()
    {
        if (navigationRoot == null)
            return null;

        Selectable[] selectables = navigationRoot.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            if (IsSelectableUsable(selectables[i]))
                return selectables[i];
        }

        return null;
    }

    private bool IsSelectableUsable(Selectable selectable)
    {
        return selectable != null &&
               selectable.gameObject.activeInHierarchy &&
               selectable.IsInteractable();
    }

    private void ClearPauseSelection()
    {
        if (EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
            return;

        Transform root = navigationRoot != null
            ? navigationRoot
            : pauseUiRoot != null
                ? pauseUiRoot.transform
                : null;

        if (root != null && selectedObject.transform.IsChildOf(root))
            EventSystem.current.SetSelectedGameObject(null);
    }

    private bool IsBattleResultPanelOpen()
    {
        return BattleStateManager.Instance != null && BattleStateManager.Instance.BattleEnded;
    }
}
