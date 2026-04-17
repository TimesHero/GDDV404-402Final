using UnityEngine;

public class TileElevation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform elevationColumn;
    [SerializeField] private Transform topSurface;
    [SerializeField] private Transform highlightOverlay;
    [SerializeField] private BoxCollider tileCollider;
    [SerializeField] private Transform decorationAnchor;
    [SerializeField] private Renderer sideRenderer;

    [Header("Elevation")]
    [SerializeField] private int elevation = 0;
    [SerializeField] private float stepHeight = 1f;
    [SerializeField] private float topThickness = 0.2f;
    
    [Header("Side Tiling")]
    [SerializeField] private bool updateSideTextureTiling = true;
    [SerializeField] private float sideTextureTilesPerStep = 1f;

    public int Elevation => elevation;

    private void Awake()
    {
        ApplyElevationVisual();
    }

    public void SetElevation(int newElevation)
    {
        elevation = Mathf.Max(0, newElevation);
        ApplyElevationVisual();
    }

    public void ApplyElevationVisual()
    {
        float totalHeight = (elevation + 1) * stepHeight;
        
        if (decorationAnchor != null)
        {
            Vector3 pos = decorationAnchor.localPosition;
            pos.y = totalHeight + (topThickness * 0.5f);
            decorationAnchor.localPosition = pos;
        }
        
        if (elevationColumn != null)
        {
            Vector3 scale = elevationColumn.localScale;
            scale.y = totalHeight;
            elevationColumn.localScale = scale;

            Vector3 pos = elevationColumn.localPosition;
            pos.y = totalHeight * 0.5f;
            elevationColumn.localPosition = pos;
        }

        if (topSurface != null)
        {
            Vector3 pos = topSurface.localPosition;
            pos.y = totalHeight;
            topSurface.localPosition = pos;

            Vector3 scale = topSurface.localScale;
            scale.y = topThickness;
            topSurface.localScale = scale;
        }

        if (highlightOverlay != null)
        {
            Vector3 pos = highlightOverlay.localPosition;
            pos.y = totalHeight + (topThickness * 0.5f) + 0.01f;
            highlightOverlay.localPosition = pos;
        }

        if (tileCollider != null)
        {
            Vector3 size = tileCollider.size;
            size.y = totalHeight + topThickness;
            tileCollider.size = size;

            Vector3 center = tileCollider.center;
            center.y = (totalHeight + topThickness) * 0.5f;
            tileCollider.center = center;
        }
        
        UpdateSideTextureTiling();
    }
    
    private void UpdateSideTextureTiling()
    {
        if (!updateSideTextureTiling || sideRenderer == null)
            return;

        Material material = sideRenderer.material;
        if (material == null)
            return;

        float verticalTiles = Mathf.Max(1f, (elevation + 1) * sideTextureTilesPerStep);

        if (material.HasProperty("_BaseMap"))
        {
            material.mainTextureScale = new Vector2(1f, verticalTiles);
        }
        else if (material.HasProperty("_MainTex"))
        {
            material.mainTextureScale = new Vector2(1f, verticalTiles);
        }
    }
}