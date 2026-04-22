using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICanvasSelectionFrame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform frameRoot;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Transform navigationRoot;
    [SerializeField] private RectTransform sizeClampRoot;

    [Header("Look")]
    [SerializeField] private Color frameColor = new Color(0.88f, 0.62f, 0.18f, 1f);
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private Vector2 sizeMultiplier = Vector2.one;
    [SerializeField] private Vector2 padding = new Vector2(18f, 14f);
    [SerializeField] private bool clampSizeToRoot = true;
    [SerializeField] private Vector2 clampRootPadding = new Vector2(4f, 4f);
    [SerializeField] private float cornerLength = 36f;
    [SerializeField] private float cornerThickness = 7f;
    [SerializeField] private bool keepOnTop = true;

    [Header("Movement")]
    [SerializeField] private bool hideWhenNoSelection = true;
    [SerializeField] private bool snapInstantly = false;
    [SerializeField] private float followSpeed = 18f;

    private readonly Vector3[] selectedWorldCorners = new Vector3[4];
    private RectTransform parentRect;
    private Image[] cornerBars;

    private void Awake()
    {
        if (frameRoot == null)
            frameRoot = transform as RectTransform;

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        parentRect = frameRoot != null && frameRoot.parent != null
            ? frameRoot.parent as RectTransform
            : null;

        ConfigureFrameRaycasts();
        ConfigureFrameRoot();
        EnsureCornerBars();
        SetFrameVisible(false);
    }

    private void LateUpdate()
    {
        if (!ControllerInputModeTracker.IsControllerMode)
        {
            SetFrameVisible(false);
            return;
        }

        if (EventSystem.current == null || frameRoot == null || parentRect == null)
        {
            SetFrameVisible(false);
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (!TryGetSelectedRect(selectedObject, out RectTransform selectedRect))
        {
            SetFrameVisible(false);
            return;
        }

        MoveToSelectedRect(selectedRect);
        ApplyCornerStyle();

        if (keepOnTop)
            frameRoot.SetAsLastSibling();

        SetFrameVisible(true);
    }

    private bool TryGetSelectedRect(GameObject selectedObject, out RectTransform selectedRect)
    {
        selectedRect = null;

        if (selectedObject == null || !selectedObject.activeInHierarchy)
            return false;

        Transform effectiveNavigationRoot = navigationRoot != null
            ? navigationRoot
            : targetCanvas != null
                ? targetCanvas.transform
                : null;

        if (effectiveNavigationRoot != null && !selectedObject.transform.IsChildOf(effectiveNavigationRoot))
            return false;

        Selectable selectable = selectedObject.GetComponent<Selectable>();
        if (selectable == null || !selectable.IsInteractable())
            return false;

        selectedRect = selectedObject.transform as RectTransform;
        return selectedRect != null;
    }

    private void MoveToSelectedRect(RectTransform selectedRect)
    {
        selectedRect.GetWorldCorners(selectedWorldCorners);

        Camera canvasCamera = GetCanvasCamera();
        Vector2 bottomLeftScreen = RectTransformUtility.WorldToScreenPoint(canvasCamera, selectedWorldCorners[0]);
        Vector2 topRightScreen = RectTransformUtility.WorldToScreenPoint(canvasCamera, selectedWorldCorners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, bottomLeftScreen, canvasCamera, out Vector2 localBottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, topRightScreen, canvasCamera, out Vector2 localTopRight);

        Vector2 targetPosition = (localBottomLeft + localTopRight) * 0.5f + offset;
        Vector2 targetSize = new Vector2(
            Mathf.Abs(localTopRight.x - localBottomLeft.x) + padding.x * 2f,
            Mathf.Abs(localTopRight.y - localBottomLeft.y) + padding.y * 2f);
        targetSize = new Vector2(targetSize.x * sizeMultiplier.x, targetSize.y * sizeMultiplier.y);
        targetSize = ClampTargetSizeToRoot(targetSize);

        if (snapInstantly)
        {
            frameRoot.anchoredPosition = targetPosition;
        }
        else
        {
            float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
            frameRoot.anchoredPosition = Vector2.Lerp(frameRoot.anchoredPosition, targetPosition, t);
        }

        frameRoot.sizeDelta = targetSize;
    }

    private Vector2 ClampTargetSizeToRoot(Vector2 targetSize)
    {
        if (!clampSizeToRoot)
            return targetSize;

        RectTransform clampRoot = sizeClampRoot != null
            ? sizeClampRoot
            : navigationRoot as RectTransform;

        if (clampRoot == null)
            return targetSize;

        Vector2 maxSize = new Vector2(
            Mathf.Max(1f, clampRoot.rect.width - clampRootPadding.x * 2f),
            Mathf.Max(1f, clampRoot.rect.height - clampRootPadding.y * 2f));

        return new Vector2(
            Mathf.Min(targetSize.x, maxSize.x),
            Mathf.Min(targetSize.y, maxSize.y));
    }

    private void ConfigureFrameRoot()
    {
        if (frameRoot == null)
            return;

        frameRoot.anchorMin = new Vector2(0.5f, 0.5f);
        frameRoot.anchorMax = new Vector2(0.5f, 0.5f);
        frameRoot.pivot = new Vector2(0.5f, 0.5f);
    }

    private void ConfigureFrameRaycasts()
    {
        if (frameRoot == null)
            return;

        Graphic[] graphics = frameRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }

    private void EnsureCornerBars()
    {
        if (frameRoot == null)
            return;

        cornerBars = new Image[8];
        CreateCornerBar(0, "TopLeft_H", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        CreateCornerBar(1, "TopLeft_V", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        CreateCornerBar(2, "TopRight_H", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        CreateCornerBar(3, "TopRight_V", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        CreateCornerBar(4, "BottomLeft_H", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        CreateCornerBar(5, "BottomLeft_V", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        CreateCornerBar(6, "BottomRight_H", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        CreateCornerBar(7, "BottomRight_V", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));

        ApplyCornerStyle();
    }

    private void CreateCornerBar(int index, string childName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        Transform existingChild = frameRoot.Find(childName);
        GameObject barObject = existingChild != null
            ? existingChild.gameObject
            : new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existingChild == null)
            barObject.transform.SetParent(frameRoot, false);

        RectTransform rectTransform = barObject.transform as RectTransform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;

        Image image = barObject.GetComponent<Image>();
        image.raycastTarget = false;
        cornerBars[index] = image;
    }

    private void ApplyCornerStyle()
    {
        if (cornerBars == null || cornerBars.Length != 8)
            return;

        for (int i = 0; i < cornerBars.Length; i++)
        {
            if (cornerBars[i] != null)
                cornerBars[i].color = frameColor;
        }

        SetBar(cornerBars[0], new Vector2(cornerLength, cornerThickness));
        SetBar(cornerBars[1], new Vector2(cornerThickness, cornerLength));
        SetBar(cornerBars[2], new Vector2(cornerLength, cornerThickness));
        SetBar(cornerBars[3], new Vector2(cornerThickness, cornerLength));
        SetBar(cornerBars[4], new Vector2(cornerLength, cornerThickness));
        SetBar(cornerBars[5], new Vector2(cornerThickness, cornerLength));
        SetBar(cornerBars[6], new Vector2(cornerLength, cornerThickness));
        SetBar(cornerBars[7], new Vector2(cornerThickness, cornerLength));
    }

    private void SetBar(Image image, Vector2 size)
    {
        if (image == null)
            return;

        RectTransform rectTransform = image.transform as RectTransform;
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private Camera GetCanvasCamera()
    {
        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return targetCanvas.worldCamera;
    }

    private void SetFrameVisible(bool visible)
    {
        if (!hideWhenNoSelection || cornerBars == null)
            return;

        for (int i = 0; i < cornerBars.Length; i++)
        {
            if (cornerBars[i] != null && cornerBars[i].enabled != visible)
                cornerBars[i].enabled = visible;
        }
    }
}
