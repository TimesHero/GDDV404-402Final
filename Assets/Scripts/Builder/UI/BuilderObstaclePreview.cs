using UnityEngine;

public class BuilderObstaclePreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BuilderStateController builderStateController;
    [SerializeField] private BuilderInputController builderInputController;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private InteractablePlacementService interactablePlacementService;
    [SerializeField] private UnitPlacementService unitPlacementService;

    [Header("Preview Visual")]
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Color previewColor = new Color(0.3f, 0.7f, 1f, 0.35f);

    private GameObject currentPreviewInstance;
    private Object currentPreviewSource;
    private BuilderToolMode currentPreviewMode;
    private bool currentPreviewUsesCardinalFacing;

    private void Update()
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (builderStateController == null || builderInputController == null)
        {
            HidePreview();
            return;
        }

        GridTile hoveredTile = builderInputController.CurrentHoveredTile;
        if (hoveredTile == null)
        {
            HidePreview();
            return;
        }

        switch (builderStateController.CurrentToolMode)
        {
            case BuilderToolMode.ObstaclePaint:
                UpdateObstaclePreview(hoveredTile);
                break;

            case BuilderToolMode.InteractablePaint:
                UpdateInteractablePreview(hoveredTile);
                break;

            case BuilderToolMode.UnitPaint:
                UpdateUnitPreview(hoveredTile);
                break;

            default:
                HidePreview();
                break;
        }
    }

    private void UpdateObstaclePreview(GridTile hoveredTile)
    {
        if (obstacleManager == null)
        {
            HidePreview();
            return;
        }

        ObstacleData selectedObstacle = builderStateController.SelectedObstacleData;
        if (selectedObstacle == null || selectedObstacle.ObstaclePrefab == null)
        {
            HidePreview();
            return;
        }

        if (NeedsRebuild(selectedObstacle, BuilderToolMode.ObstaclePaint))
            RebuildPreview(selectedObstacle.ObstaclePrefab, selectedObstacle, BuilderToolMode.ObstaclePaint);

        if (currentPreviewInstance == null)
            return;

        int rotationY = builderStateController.SelectedObstacleRotationY;

        Vector3 previewPosition = obstacleManager.GetPreviewWorldPosition(
            selectedObstacle,
            hoveredTile.GridPosition,
            rotationY
        ) + selectedObstacle.GetVisualOffsetForRotation(rotationY);

        currentPreviewInstance.transform.position = previewPosition;
        currentPreviewInstance.transform.rotation = Quaternion.Euler(
            selectedObstacle.GetVisualRotationEulerForRotation(rotationY)
        );
        currentPreviewInstance.transform.localScale = selectedObstacle.GetVisualScaleForRotation(rotationY);
        currentPreviewInstance.SetActive(true);
    }

    private void UpdateInteractablePreview(GridTile hoveredTile)
    {
        if (interactablePlacementService == null)
        {
            HidePreview();
            return;
        }

        InteractableData selectedInteractable = builderStateController.SelectedInteractableData;
        if (selectedInteractable == null || selectedInteractable.prefab == null)
        {
            HidePreview();
            return;
        }

        if (NeedsRebuild(selectedInteractable, BuilderToolMode.InteractablePaint))
            RebuildPreview(selectedInteractable.prefab, selectedInteractable, BuilderToolMode.InteractablePaint);

        if (currentPreviewInstance == null)
            return;

        int rotationY = builderStateController.SelectedInteractableRotationY;

        currentPreviewInstance.transform.position =
            interactablePlacementService.GetPreviewWorldPosition(selectedInteractable, hoveredTile, rotationY);

        currentPreviewInstance.transform.rotation = Quaternion.Euler(
            selectedInteractable.GetVisualRotationEulerForRotation(rotationY)
        );
        currentPreviewInstance.transform.localScale =
            selectedInteractable.GetVisualScaleForRotation(rotationY);

        currentPreviewInstance.SetActive(true);
    }

    private void UpdateUnitPreview(GridTile hoveredTile)
    {
        if (unitPlacementService == null)
        {
            HidePreview();
            return;
        }

        UnitData selectedUnit = builderStateController.SelectedUnitData;
        if (selectedUnit == null || selectedUnit.unitPrefab == null)
        {
            HidePreview();
            return;
        }

        bool useCardinalFacing = builderStateController.SelectedUnitUsesCardinalFacing;

        if (NeedsRebuild(selectedUnit, BuilderToolMode.UnitPaint) || currentPreviewUsesCardinalFacing != useCardinalFacing)
            RebuildPreview(selectedUnit.unitPrefab, selectedUnit, BuilderToolMode.UnitPaint);

        if (currentPreviewInstance == null)
            return;

        currentPreviewUsesCardinalFacing = useCardinalFacing;

        int rotationY = builderStateController.SelectedUnitRotationY;

        currentPreviewInstance.transform.position =
            unitPlacementService.GetPreviewWorldPosition(selectedUnit, hoveredTile, rotationY);

        GridUnit previewUnit = currentPreviewInstance.GetComponent<GridUnit>();
        if (previewUnit != null)
        {
            unitPlacementService.ApplyUnitRotation(previewUnit, selectedUnit, rotationY, useCardinalFacing);
        }
        else
        {
            currentPreviewInstance.transform.rotation = Quaternion.Euler(
                selectedUnit.GetVisualRotationEulerForRotation(rotationY, useCardinalFacing)
            );
        }

        currentPreviewInstance.transform.localScale =
            selectedUnit.GetVisualScaleForRotation(rotationY);

        currentPreviewInstance.SetActive(true);
    }

    private bool NeedsRebuild(Object source, BuilderToolMode mode)
    {
        return currentPreviewInstance == null || currentPreviewSource != source || currentPreviewMode != mode;
    }

    private void RebuildPreview(GameObject prefab, Object source, BuilderToolMode mode)
    {
        HidePreview();

        if (prefab == null)
            return;

        currentPreviewInstance = Instantiate(prefab);
        currentPreviewInstance.name = $"{prefab.name}_Preview";
        currentPreviewSource = source;
        currentPreviewMode = mode;
        currentPreviewUsesCardinalFacing = false;

        ApplyPreviewVisuals(currentPreviewInstance);
    }

    private void ApplyPreviewVisuals(GameObject previewRoot)
    {
        if (previewRoot == null)
            return;

        Collider[] colliders = previewRoot.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            col.enabled = false;

        Renderer[] renderers = previewRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (previewMaterial != null)
            {
                Material[] mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = previewMaterial;

                renderer.materials = mats;
            }

            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", previewColor);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", previewColor);
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void HidePreview()
    {
        if (currentPreviewInstance != null)
            Destroy(currentPreviewInstance);

        currentPreviewInstance = null;
        currentPreviewSource = null;
        currentPreviewUsesCardinalFacing = false;
    }
}
