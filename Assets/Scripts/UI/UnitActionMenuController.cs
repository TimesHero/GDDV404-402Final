using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UnitActionMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button pushButton;
    [SerializeField] private Button removeBarrelButton;
    [FormerlySerializedAs("exitBarrelButton")]
    [SerializeField] private Button exitButton;

    [Header("World Space")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector3 worldOffset = new Vector3(-1.25f, 1.05f, 0f);

    [Header("Sorting")]
    [SerializeField] private int sortingOrder = 30000;

    [Header("Controller Selection Frame")]
    [SerializeField] private bool autoCreateControllerSelectionFrame = true;

    [Header("Auto Layout")]
    [SerializeField] private bool configureCompactLayout = true;
    [SerializeField] private Vector2 buttonSize = new Vector2(120f, 26f);
    [SerializeField] private int panelPadding = 6;
    [SerializeField] private float buttonSpacing = 4f;

    [Header("Debug")]
    [SerializeField] private bool logButtonClicks = true;

    private Action moveAction;
    private Action attackAction;
    private Action pushAction;
    private Action removeBarrelAction;
    private Action exitAction;
    private int lastHandledClickFrame = -1;
    private int menuShownFrame = -1;
    private bool isShowing;
    private bool wasControllerMode;
    private static UnitActionMenuController activeMenu;

    public static bool IsAnyActionMenuOpen => activeMenu != null && activeMenu.IsVisible;
    public static UnitActionMenuController ActiveMenu => activeMenu;

    private void Awake()
    {
        if (menuRoot == null)
            menuRoot = gameObject;

        ConfigureCanvasSorting();
        ConfigureLayout();
        ConfigureRaycastTargets();
        EnsureControllerSelectionFrame();

        if (!isShowing)
            Hide();
    }

    public bool IsVisible => menuRoot != null && menuRoot.activeInHierarchy;

    public bool Contains(GameObject target)
    {
        if (target == null)
            return false;

        if (target.transform == transform || target.transform.IsChildOf(transform))
            return true;

        return menuRoot != null &&
               (target.transform == menuRoot.transform || target.transform.IsChildOf(menuRoot.transform));
    }

    private void LateUpdate()
    {
        if (menuRoot == null || !menuRoot.activeSelf)
            return;

        UpdateControllerSelectionMode();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
            transform.forward = targetCamera.transform.forward;
    }

    public void ShowForUnit(
        Vector3 anchorWorldPosition,
        bool canMove,
        bool canAttack,
        bool canPush,
        bool hasBarrel,
        Action onMove,
        Action onAttack,
        Action onPush,
        Action onRemoveBarrel,
        Action onExitMenu)
    {
        ConfigureCanvasSorting();
        transform.position = GetMenuWorldPosition(anchorWorldPosition);

        moveAction = onMove;
        attackAction = onAttack;
        pushAction = onPush;
        removeBarrelAction = onRemoveBarrel;
        exitAction = onExitMenu;

        SetupButton(moveButton, canMove, onMove);
        SetupButton(attackButton, canAttack, onAttack);
        SetupButton(pushButton, canPush, onPush);
        SetupButton(removeBarrelButton, hasBarrel, onRemoveBarrel);
        SetupButton(exitButton, true, onExitMenu);

        if (menuRoot != null)
        {
            isShowing = true;
            menuRoot.SetActive(true);
            isShowing = false;
        }

        activeMenu = this;
        menuShownFrame = Time.frameCount;
        wasControllerMode = ControllerInputModeTracker.IsControllerMode;

        if (wasControllerMode)
            StartCoroutine(SelectFirstAvailableButtonNextFrame());
        else
            ClearSelectionIfMenuOwnsIt();
    }

    public void Hide()
    {
        ClearSelectionIfMenuOwnsIt();

        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (activeMenu == this)
            activeMenu = null;
    }

    private void OnDisable()
    {
        if (activeMenu == this)
            activeMenu = null;
    }

    private void ClearSelectionIfMenuOwnsIt()
    {
        if (EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null && Contains(selectedObject))
            EventSystem.current.SetSelectedGameObject(null);
    }

    private IEnumerator SelectFirstAvailableButtonNextFrame()
    {
        yield return null;
        SelectFirstAvailableButton();
    }

    public void SelectFirstAvailableButton()
    {
        if (!IsVisible || EventSystem.current == null)
            return;

        Button button = GetFirstAvailableButton();
        if (button != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private Button GetFirstAvailableButton()
    {
        if (CanClickButton(moveButton))
            return moveButton;

        if (CanClickButton(attackButton))
            return attackButton;

        if (CanClickButton(pushButton))
            return pushButton;

        if (CanClickButton(removeBarrelButton))
            return removeBarrelButton;

        if (CanClickButton(exitButton))
            return exitButton;

        return null;
    }

    private void UpdateControllerSelectionMode()
    {
        bool isControllerMode = ControllerInputModeTracker.IsControllerMode;
        if (isControllerMode == wasControllerMode)
            return;

        wasControllerMode = isControllerMode;

        if (!isControllerMode)
        {
            ClearSelectionIfMenuOwnsIt();
            return;
        }

        SelectFirstAvailableButton();
    }

    public bool TryHandlePointerClick(Vector2 screenPosition, IReadOnlyList<RaycastResult> raycastResults)
    {
        if (!IsVisible)
            return false;

        if (TryClickButtonFromRaycastResults(raycastResults, moveButton, moveAction))
            return true;

        if (TryClickButtonFromRaycastResults(raycastResults, attackButton, attackAction))
            return true;

        if (TryClickButtonFromRaycastResults(raycastResults, pushButton, pushAction))
            return true;

        if (TryClickButtonFromRaycastResults(raycastResults, removeBarrelButton, removeBarrelAction))
            return true;

        if (TryClickButtonFromRaycastResults(raycastResults, exitButton, exitAction))
            return true;

        if (TryClickButtonByRect(screenPosition, moveButton, moveAction))
            return true;

        if (TryClickButtonByRect(screenPosition, attackButton, attackAction))
            return true;

        if (TryClickButtonByRect(screenPosition, pushButton, pushAction))
            return true;

        if (TryClickButtonByRect(screenPosition, removeBarrelButton, removeBarrelAction))
            return true;

        if (TryClickButtonByRect(screenPosition, exitButton, exitAction))
            return true;

        return false;
    }

    private void SetupButton(Button button, bool visible, Action callback)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
        button.onClick.RemoveAllListeners();

        if (!visible)
            return;

        button.onClick.AddListener(() =>
        {
            ExecuteButtonAction(button, callback, "EventSystem");
        });
    }

    private bool TryClickButtonFromRaycastResults(IReadOnlyList<RaycastResult> raycastResults, Button button, Action callback)
    {
        if (!CanClickButton(button))
            return false;

        if (raycastResults == null)
            return false;

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject hitObject = raycastResults[i].gameObject;
            if (hitObject == null)
                continue;

            if (hitObject.transform == button.transform || hitObject.transform.IsChildOf(button.transform))
            {
                ExecuteButtonAction(button, callback, "TileSelector raycast");
                return true;
            }
        }

        return false;
    }

    private bool TryClickButtonByRect(Vector2 screenPosition, Button button, Action callback)
    {
        if (!CanClickButton(button))
            return false;

        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform == null)
            return false;

        Camera eventCamera = targetCamera != null ? targetCamera : Camera.main;
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera))
            return false;

        ExecuteButtonAction(button, callback, "TileSelector rect");
        return true;
    }

    private bool CanClickButton(Button button)
    {
        return button != null &&
               button.gameObject.activeInHierarchy &&
               button.interactable;
    }

    private void ExecuteButtonAction(Button button, Action callback, string source)
    {
        if (lastHandledClickFrame == Time.frameCount)
            return;

        if (Time.frameCount <= menuShownFrame)
            return;

        lastHandledClickFrame = Time.frameCount;

        if (logButtonClicks)
            Debug.Log($"Action menu button clicked: {button.name} via {source}");

        Hide();
        callback?.Invoke();
    }

    private void ConfigureCanvasSorting()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].overrideSorting = true;
            canvases[i].sortingOrder = sortingOrder;

            if (targetCamera == null)
                targetCamera = Camera.main;

            canvases[i].worldCamera = targetCamera;
        }

        GraphicRaycaster[] raycasters = GetComponentsInChildren<GraphicRaycaster>(true);
        for (int i = 0; i < raycasters.Length; i++)
            raycasters[i].blockingObjects = GraphicRaycaster.BlockingObjects.None;
    }

    private Vector3 GetMenuWorldPosition(Vector3 anchorWorldPosition)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return anchorWorldPosition + worldOffset;

        return anchorWorldPosition +
               targetCamera.transform.right * worldOffset.x +
               Vector3.up * worldOffset.y +
               targetCamera.transform.forward * worldOffset.z;
    }

    private void ConfigureLayout()
    {
        if (!configureCompactLayout)
            return;

        if (panelRoot == null)
            panelRoot = GetPanelRootFromButtons();

        if (panelRoot == null)
            return;

        VerticalLayoutGroup layout = panelRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = panelRoot.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(panelPadding, panelPadding, panelPadding, panelPadding);
        layout.spacing = buttonSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = panelRoot.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ConfigureButtonLayout(moveButton);
        ConfigureButtonLayout(attackButton);
        ConfigureButtonLayout(pushButton);
        ConfigureButtonLayout(removeBarrelButton);
        ConfigureButtonLayout(exitButton);
    }

    private RectTransform GetPanelRootFromButtons()
    {
        if (moveButton != null && moveButton.transform.parent != null)
            return moveButton.transform.parent as RectTransform;

        if (attackButton != null && attackButton.transform.parent != null)
            return attackButton.transform.parent as RectTransform;

        if (pushButton != null && pushButton.transform.parent != null)
            return pushButton.transform.parent as RectTransform;

        if (removeBarrelButton != null && removeBarrelButton.transform.parent != null)
            return removeBarrelButton.transform.parent as RectTransform;

        if (exitButton != null && exitButton.transform.parent != null)
            return exitButton.transform.parent as RectTransform;

        return null;
    }

    private void ConfigureButtonLayout(Button button)
    {
        if (button == null)
            return;

        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform != null)
            rectTransform.sizeDelta = buttonSize;

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = button.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = buttonSize.x;
        layoutElement.preferredHeight = buttonSize.y;
    }

    private void ConfigureRaycastTargets()
    {
        ConfigureButtonRaycasts(moveButton);
        ConfigureButtonRaycasts(attackButton);
        ConfigureButtonRaycasts(pushButton);
        ConfigureButtonRaycasts(removeBarrelButton);
        ConfigureButtonRaycasts(exitButton);
    }

    private void ConfigureButtonRaycasts(Button button)
    {
        if (button == null)
            return;

        Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = graphics[i] == button.targetGraphic;

        if (button.targetGraphic != null)
            button.targetGraphic.raycastTarget = true;
    }

    private void EnsureControllerSelectionFrame()
    {
        if (!autoCreateControllerSelectionFrame)
            return;

        if (GetComponentInChildren<UIWorldSpaceSelectionFrame>(true) != null)
            return;

        Canvas targetCanvas = GetComponentInChildren<Canvas>(true);
        if (targetCanvas == null)
            return;

        GameObject frameObject = new GameObject("ControllerSelectionFrame", typeof(RectTransform), typeof(UIWorldSpaceSelectionFrame));
        frameObject.transform.SetParent(targetCanvas.transform, false);

        RectTransform frameTransform = frameObject.transform as RectTransform;
        frameTransform.anchorMin = new Vector2(0.5f, 0.5f);
        frameTransform.anchorMax = new Vector2(0.5f, 0.5f);
        frameTransform.pivot = new Vector2(0.5f, 0.5f);
        frameTransform.sizeDelta = Vector2.zero;
        frameObject.transform.SetAsLastSibling();
    }
}
