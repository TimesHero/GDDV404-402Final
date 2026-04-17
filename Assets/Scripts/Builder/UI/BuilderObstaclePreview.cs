using UnityEngine;

public class BuilderObstaclePreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BuilderStateController builderStateController;
    [SerializeField] private BuilderInputController builderInputController;
    [SerializeField] private ObstacleManager obstacleManager;

    [Header("Preview Visual")]
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Color previewColor = new Color(0.3f, 0.7f, 1f, 0.35f);

    private GameObject currentPreviewInstance;
    private ObstacleData currentPreviewData;

    private void Update()
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (builderStateController == null || builderInputController == null || obstacleManager == null)
        {
            HidePreview();
            return;
        }

        if (builderStateController.CurrentToolMode != BuilderToolMode.ObstaclePaint)
        {
            HidePreview();
            return;
        }

        ObstacleData selectedObstacle = builderStateController.SelectedObstacleData;
        GridTile hoveredTile = builderInputController.CurrentHoveredTile;

        if (selectedObstacle == null || hoveredTile == null || selectedObstacle.ObstaclePrefab == null)
        {
            HidePreview();
            return;
        }

        if (currentPreviewInstance == null || currentPreviewData != selectedObstacle)
        {
            RebuildPreview(selectedObstacle);
        }

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

    private void RebuildPreview(ObstacleData selectedObstacle)
    {
        HidePreview();

        if (selectedObstacle == null || selectedObstacle.ObstaclePrefab == null)
            return;

        currentPreviewInstance = Instantiate(selectedObstacle.ObstaclePrefab);
        currentPreviewInstance.name = $"{selectedObstacle.name}_Preview";
        currentPreviewData = selectedObstacle;

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
        currentPreviewData = null;
    }
}