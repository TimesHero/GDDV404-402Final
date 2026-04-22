using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISelectionGridIndicator : MonoBehaviour
{
    private enum IndicatorPlacementMode
    {
        CanvasWorldCorners,
        CameraScreenPlane
    }

    [Header("References")]
    [SerializeField] private Transform indicatorRoot;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Transform navigationRoot;
    [SerializeField] private Camera placementCamera;

    [Header("Placement")]
    [SerializeField] private IndicatorPlacementMode placementMode = IndicatorPlacementMode.CameraScreenPlane;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationEuler = Vector3.zero;
    [SerializeField] private float cameraPlaneDistance = 2f;
    [SerializeField] private bool facePlacementCamera = true;
    [SerializeField] private bool hideWhenNoSelection = true;
    [SerializeField] private bool snapInstantly = false;
    [SerializeField] private float followSpeed = 18f;

    [Header("Size")]
    [SerializeField] private bool scaleToSelectedRect = true;
    [SerializeField] private Vector2 baseWorldSize = Vector2.one;
    [SerializeField] private Vector2 padding = new Vector2(1.15f, 1.35f);
    [SerializeField] private float cameraScreenScaleMultiplier = 12f;
    [SerializeField] private float minimumCameraScreenScale = 0.5f;
    [SerializeField] private bool preserveOriginalZScale = true;

    private readonly Vector3[] selectedCorners = new Vector3[4];
    private readonly Vector3[] selectedScreenCorners = new Vector3[4];
    private Vector3 originalScale = Vector3.one;
    private Renderer[] indicatorRenderers;

    private void Awake()
    {
        if (indicatorRoot == null)
            indicatorRoot = transform;

        originalScale = indicatorRoot.localScale;
        indicatorRenderers = indicatorRoot.GetComponentsInChildren<Renderer>(true);
        SetIndicatorVisible(false);
    }

    private void LateUpdate()
    {
        if (!ControllerInputModeTracker.IsControllerMode)
        {
            SetIndicatorVisible(false);
            return;
        }

        if (EventSystem.current == null || indicatorRoot == null)
        {
            SetIndicatorVisible(false);
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (!TryGetSelectedRect(selectedObject, out RectTransform selectedRect))
        {
            SetIndicatorVisible(false);
            return;
        }

        selectedRect.GetWorldCorners(selectedCorners);

        if (placementMode == IndicatorPlacementMode.CameraScreenPlane)
            PlaceOnCameraScreenPlane();
        else
            PlaceOnCanvasWorldCorners();

        SetIndicatorVisible(true);
    }

    private bool TryGetSelectedRect(GameObject selectedObject, out RectTransform selectedRect)
    {
        selectedRect = null;

        if (selectedObject == null || !selectedObject.activeInHierarchy)
            return false;

        if (navigationRoot != null && !selectedObject.transform.IsChildOf(navigationRoot))
            return false;

        Selectable selectable = selectedObject.GetComponent<Selectable>();
        if (selectable == null || !selectable.IsInteractable())
            return false;

        selectedRect = selectedObject.transform as RectTransform;
        return selectedRect != null;
    }

    private void MoveIndicator(Vector3 targetPosition)
    {
        if (snapInstantly)
        {
            indicatorRoot.position = targetPosition;
            return;
        }

        float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
        indicatorRoot.position = Vector3.Lerp(indicatorRoot.position, targetPosition, t);
    }

    private void PlaceOnCanvasWorldCorners()
    {
        Vector3 center = (selectedCorners[0] + selectedCorners[2]) * 0.5f + worldOffset;
        MoveIndicator(center);
        ResizeIndicatorFromWorldCorners();
        indicatorRoot.rotation = Quaternion.Euler(rotationEuler);
    }

    private void PlaceOnCameraScreenPlane()
    {
        Camera cameraToUse = GetPlacementCamera();
        if (cameraToUse == null)
            return;

        UpdateScreenCorners(cameraToUse);

        Vector3 screenCenter = (selectedScreenCorners[0] + selectedScreenCorners[2]) * 0.5f;
        Vector3 targetPosition = cameraToUse.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, cameraPlaneDistance)) + worldOffset;
        MoveIndicator(targetPosition);
        ResizeIndicatorFromScreenCorners(cameraToUse);

        indicatorRoot.rotation = facePlacementCamera
            ? cameraToUse.transform.rotation * Quaternion.Euler(rotationEuler)
            : Quaternion.Euler(rotationEuler);
    }

    private void ResizeIndicatorFromWorldCorners()
    {
        if (!scaleToSelectedRect || baseWorldSize.x <= 0f || baseWorldSize.y <= 0f)
            return;

        float width = Vector3.Distance(selectedCorners[0], selectedCorners[3]);
        float height = Vector3.Distance(selectedCorners[0], selectedCorners[1]);
        Vector3 targetScale = originalScale;

        targetScale.x = originalScale.x * width / baseWorldSize.x * padding.x;
        targetScale.y = originalScale.y * height / baseWorldSize.y * padding.y;

        if (!preserveOriginalZScale)
            targetScale.z = originalScale.z * Mathf.Max(width / baseWorldSize.x, height / baseWorldSize.y);

        indicatorRoot.localScale = targetScale;
    }

    private void ResizeIndicatorFromScreenCorners(Camera cameraToUse)
    {
        if (!scaleToSelectedRect || baseWorldSize.x <= 0f || baseWorldSize.y <= 0f || Screen.height <= 0)
            return;

        float pixelWidth = Vector3.Distance(selectedScreenCorners[0], selectedScreenCorners[3]);
        float pixelHeight = Vector3.Distance(selectedScreenCorners[0], selectedScreenCorners[1]);
        float worldUnitsPerPixel = GetWorldUnitsPerPixel(cameraToUse);

        Vector3 targetScale = originalScale;
        targetScale.x = originalScale.x * pixelWidth * worldUnitsPerPixel / baseWorldSize.x * padding.x;
        targetScale.y = originalScale.y * pixelHeight * worldUnitsPerPixel / baseWorldSize.y * padding.y;
        targetScale.x *= cameraScreenScaleMultiplier;
        targetScale.y *= cameraScreenScaleMultiplier;

        if (!preserveOriginalZScale)
            targetScale.z = originalScale.z * Mathf.Max(targetScale.x / originalScale.x, targetScale.y / originalScale.y);

        targetScale.x = Mathf.Max(targetScale.x, minimumCameraScreenScale);
        targetScale.y = Mathf.Max(targetScale.y, minimumCameraScreenScale);

        indicatorRoot.localScale = targetScale;
    }

    private void UpdateScreenCorners(Camera cameraToUse)
    {
        Camera canvasCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;

        for (int i = 0; i < selectedCorners.Length; i++)
        {
            selectedScreenCorners[i] = targetCanvas != null && targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? selectedCorners[i]
                : RectTransformUtility.WorldToScreenPoint(canvasCamera != null ? canvasCamera : cameraToUse, selectedCorners[i]);
        }
    }

    private float GetWorldUnitsPerPixel(Camera cameraToUse)
    {
        if (cameraToUse.orthographic)
            return cameraToUse.orthographicSize * 2f / Screen.height;

        float worldHeightAtDistance = 2f * cameraPlaneDistance * Mathf.Tan(cameraToUse.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return worldHeightAtDistance / Screen.height;
    }

    private Camera GetPlacementCamera()
    {
        if (placementCamera != null)
            return placementCamera;

        if (targetCanvas != null && targetCanvas.worldCamera != null)
            return targetCanvas.worldCamera;

        return Camera.main;
    }

    private void SetIndicatorVisible(bool visible)
    {
        if (!hideWhenNoSelection || indicatorRoot == null)
            return;

        if (indicatorRenderers == null || indicatorRenderers.Length == 0)
            indicatorRenderers = indicatorRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < indicatorRenderers.Length; i++)
        {
            if (indicatorRenderers[i] != null)
                indicatorRenderers[i].enabled = visible;
        }
    }
}
