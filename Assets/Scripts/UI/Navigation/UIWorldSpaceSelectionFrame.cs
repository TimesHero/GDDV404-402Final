using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIWorldSpaceSelectionFrame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform frameRoot;
    [SerializeField] private Transform navigationRoot;

    [Header("Scene Corner Objects")]
    [SerializeField] private bool updateCornerBarSizesFromInspector = true;
    [SerializeField] private bool updateCornerBarScaleFromInspector = true;

    [Header("Look")]
    [SerializeField] private Color frameColor = new Color(0.88f, 0.62f, 0.18f, 1f);
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private bool resizeToSelected = true;
    [SerializeField] private Vector2 sizeMultiplier = Vector2.one;
    [SerializeField] private Vector2 padding = new Vector2(0.05f, 0.05f);
    [SerializeField] private float cornerLength = 12f;
    [SerializeField] private float cornerThickness = 3f;
    [SerializeField] private float cornerVisualScale = 0.25f;
    [SerializeField] private Vector3 cornerLocalScale = new Vector3(0.03f, 0.03f, 0.03f);
    [SerializeField] private bool keepOnTop = true;

    [Header("Movement")]
    [SerializeField] private bool hideWhenNoSelection = true;
    [SerializeField] private bool snapInstantly = true;
    [SerializeField] private float followSpeed = 18f;

    [Header("Layout Safety")]
    [SerializeField] private bool ignoreParentLayout = true;

    private readonly Vector3[] selectedWorldCorners = new Vector3[4];
    private RectTransform parentRect;
    private Image[] cornerBars;

    private void Awake()
    {
        CacheReferences();
        EnsureIgnoredByLayout();
        SetFrameVisible(false);
    }

    private void Reset()
    {
        frameRoot = transform as RectTransform;
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

        if (keepOnTop)
            frameRoot.SetAsLastSibling();

        SetFrameVisible(true);
    }

    private void CacheReferences()
    {
        if (frameRoot == null)
            frameRoot = transform as RectTransform;

        parentRect = frameRoot != null && frameRoot.parent != null
            ? frameRoot.parent as RectTransform
            : null;

        CacheCornerBars();
    }

    private void EnsureIgnoredByLayout()
    {
        if (!ignoreParentLayout || frameRoot == null)
            return;

        LayoutElement layoutElement = frameRoot.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = frameRoot.gameObject.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = true;
    }

    [ContextMenu("Generate Corner Bars In Scene")]
    public void GenerateCornerBarsInScene()
    {
        if (frameRoot == null)
            frameRoot = transform as RectTransform;

        parentRect = frameRoot != null && frameRoot.parent != null
            ? frameRoot.parent as RectTransform
            : null;

        ConfigureFrameRoot();
        EnsureCornerBars();
        ApplyCornerStyle();
    }

    [ContextMenu("Refresh Existing Corner References")]
    public void CacheCornerBars()
    {
        if (frameRoot == null)
            return;

        cornerBars = new Image[8];
        cornerBars[0] = GetCornerImage("TopLeft_H");
        cornerBars[1] = GetCornerImage("TopLeft_V");
        cornerBars[2] = GetCornerImage("TopRight_H");
        cornerBars[3] = GetCornerImage("TopRight_V");
        cornerBars[4] = GetCornerImage("BottomLeft_H");
        cornerBars[5] = GetCornerImage("BottomLeft_V");
        cornerBars[6] = GetCornerImage("BottomRight_H");
        cornerBars[7] = GetCornerImage("BottomRight_V");
    }

    private Image GetCornerImage(string childName)
    {
        Transform child = frameRoot.Find(childName);
        if (child == null)
            return null;

        Image image = child.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;

        return image;
    }

    private bool TryGetSelectedRect(GameObject selectedObject, out RectTransform selectedRect)
    {
        selectedRect = null;

        if (selectedObject == null || !selectedObject.activeInHierarchy)
            return false;

        Transform effectiveRoot = navigationRoot != null
            ? navigationRoot
            : frameRoot != null && frameRoot.parent != null
                ? frameRoot.parent
                : null;

        if (effectiveRoot != null && !selectedObject.transform.IsChildOf(effectiveRoot))
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

        Vector2 localBottomLeft = parentRect.InverseTransformPoint(selectedWorldCorners[0]);
        Vector2 localTopRight = parentRect.InverseTransformPoint(selectedWorldCorners[2]);

        Vector2 targetPosition = (localBottomLeft + localTopRight) * 0.5f + offset;
        Vector2 targetSize = new Vector2(
            Mathf.Abs(localTopRight.x - localBottomLeft.x) + padding.x * 2f,
            Mathf.Abs(localTopRight.y - localBottomLeft.y) + padding.y * 2f);
        targetSize = new Vector2(targetSize.x * sizeMultiplier.x, targetSize.y * sizeMultiplier.y);

        if (snapInstantly)
        {
            frameRoot.anchoredPosition = targetPosition;
        }
        else
        {
            float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
            frameRoot.anchoredPosition = Vector2.Lerp(frameRoot.anchoredPosition, targetPosition, t);
        }

        if (resizeToSelected)
            frameRoot.sizeDelta = targetSize;
    }

    private void ConfigureFrameRoot()
    {
        if (frameRoot == null)
            return;

        frameRoot.anchorMin = new Vector2(0.5f, 0.5f);
        frameRoot.anchorMax = new Vector2(0.5f, 0.5f);
        frameRoot.pivot = new Vector2(0.5f, 0.5f);
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
            {
                cornerBars[i].color = frameColor;
                if (updateCornerBarScaleFromInspector)
                    cornerBars[i].transform.localScale = cornerLocalScale;
            }
        }

        if (!updateCornerBarSizesFromInspector)
            return;

        float scaledLength = cornerLength * cornerVisualScale;
        float scaledThickness = cornerThickness * cornerVisualScale;

        SetBar(cornerBars[0], new Vector2(scaledLength, scaledThickness));
        SetBar(cornerBars[1], new Vector2(scaledThickness, scaledLength));
        SetBar(cornerBars[2], new Vector2(scaledLength, scaledThickness));
        SetBar(cornerBars[3], new Vector2(scaledThickness, scaledLength));
        SetBar(cornerBars[4], new Vector2(scaledLength, scaledThickness));
        SetBar(cornerBars[5], new Vector2(scaledThickness, scaledLength));
        SetBar(cornerBars[6], new Vector2(scaledLength, scaledThickness));
        SetBar(cornerBars[7], new Vector2(scaledThickness, scaledLength));
    }

    private void SetBar(Image image, Vector2 size)
    {
        if (image == null)
            return;

        RectTransform rectTransform = image.transform as RectTransform;
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
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
