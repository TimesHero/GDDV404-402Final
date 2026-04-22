using UnityEngine;

public enum TerrainType
{
    Ground,
    Forest,
    Water,
    Hazard,
    Blocked
}

public class GridTile : MonoBehaviour
{
    private GameObject spawnedDecoration;
    
    [Header("Grid Coordinates")] 
    public int X;
    public int Y;
    
    [Header("Tile Data")]
    //public bool isWalkable = true;
    //public int movementCost = 1;
    public TerrainType terrainType = TerrainType.Ground;

    public bool isWalkable { get; private set; } = true;
    public int movementCost { get; private set; } = 1;
    
    [Header("Occupancy")] 
    public bool isOccupied;
    public GameObject occupyingUnit;
    
    public GameObject OccupyingUnit => occupyingUnit;
    
    [Header("Visuals")]
    [SerializeField] private Renderer tileRenderer;
    [SerializeField] private Renderer highlightOverlayRenderer;
    [SerializeField] private Renderer elevationColumnRenderer;
    
    [Header("References")]
    [SerializeField] private TileManager tileManager;
    [SerializeField] private Transform decorationAnchor;

    private MaterialPropertyBlock baseBlock;
    private MaterialPropertyBlock overlayBlock;
    private int baseColorPropertyId = -1;
    private int overlayColorPropertyId = -1;
    
    private Material originalBaseMaterial;
    private Material originalSideMaterial;
    
    public TerrainTypeData CurrentTerrainData
    {
        get
        {
            if (tileManager == null)
                return null;

            return tileManager.GetTerrainData(terrainType);
        }
    }
    
    public Vector2Int GridPosition => new Vector2Int(X, Y);
    
    public TerrainType TerrainType
    {
        get => terrainType;
        set => terrainType = value;
    }

    private void Awake()
    {
        baseBlock = new MaterialPropertyBlock();
        overlayBlock = new MaterialPropertyBlock();

        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<Renderer>();
        
        if (tileRenderer != null)
            originalBaseMaterial = tileRenderer.sharedMaterial;
        
        if (elevationColumnRenderer != null)
            originalSideMaterial = elevationColumnRenderer.sharedMaterial;

        CacheColorProperties();
        HideOverlay();
    }
    public void Initialize(int x, int y, TileManager manager)
    {
        X = x;
        Y = y;
        tileManager = manager;
        gameObject.name = $"Tile_{x}_{y}";
        ApplyTerrainSettings();
        HideOverlay();
    }
    
    public void SetOccupant(GameObject unit)
    {
        occupyingUnit = unit;
        isOccupied = unit != null;
    }
    
    private void CacheColorProperties()
    {
        if (tileRenderer == null || tileRenderer.sharedMaterial == null) 
            return;
        if (tileRenderer != null && tileRenderer.sharedMaterial != null)
        {
            Material material = tileRenderer.sharedMaterial;

            if (material.HasProperty("_BaseColor"))
                baseColorPropertyId = Shader.PropertyToID("_BaseColor");
            else if (material.HasProperty("_Color"))
                baseColorPropertyId = Shader.PropertyToID("_Color");
        }

        if (highlightOverlayRenderer != null && highlightOverlayRenderer.sharedMaterial != null)
        {
            Material material = highlightOverlayRenderer.sharedMaterial;

            if (material.HasProperty("_BaseColor"))
                overlayColorPropertyId = Shader.PropertyToID("_BaseColor");
            else if (material.HasProperty("_Color"))
                overlayColorPropertyId = Shader.PropertyToID("_Color");
        }
    }

    public void SetHighlight(Color color)
    {
        if (tileRenderer == null || baseColorPropertyId == -1) 
            return;
        
        tileRenderer.GetPropertyBlock(baseBlock);
        baseBlock.SetColor(baseColorPropertyId, color);
        tileRenderer.SetPropertyBlock(baseBlock);
    }
    public void ResetHighlight()
    {
        HideOverlay();
    }

    public void ApplyTerrainSettings()
    {
        if (tileManager == null)
        {
            Debug.LogWarning($"{name}: TileManager reference is missing.");
            return;
        }

        TerrainTypeData data = tileManager.GetTerrainData(terrainType);

        if (data == null)
        {
            Debug.LogWarning($"{name}: No TerrainTypeData found for {terrainType}");
            return;
        }

        movementCost = data.MovementCost;
        isWalkable = data.IsWalkable;
        
        ApplyTerrainVisualOnly();
        RefreshDecoration();
    }
    private void ApplyTerrainVisualOnly()
    {
        if (tileManager == null)
            return;

        TerrainTypeData data = tileManager.GetTerrainData(terrainType);
        if (data == null)
            return;

        ApplyTopVisual(data);
        ApplySideVisual(data);
    }
    private void ApplyTopVisual(TerrainTypeData data)
    {
        if (tileRenderer == null)
            return;

        if (data.TopMaterialOverride != null)
        {
            tileRenderer.sharedMaterial = data.TopMaterialOverride;
            tileRenderer.SetPropertyBlock(null);
            return;
        }

        if (originalBaseMaterial != null)
            tileRenderer.sharedMaterial = originalBaseMaterial;

        int colorPropertyId = GetRendererColorPropertyId(tileRenderer);
        if (colorPropertyId != -1)
        {
            tileRenderer.GetPropertyBlock(baseBlock);
            baseBlock.SetColor(colorPropertyId, data.TileColor);
            tileRenderer.SetPropertyBlock(baseBlock);
        }
    }

    private void ApplySideVisual(TerrainTypeData data)
    {
        if (elevationColumnRenderer == null)
            return;

        if (data.SideMaterialOverride != null)
        {
            elevationColumnRenderer.sharedMaterial = data.SideMaterialOverride;
            elevationColumnRenderer.SetPropertyBlock(null);
            return;
        }

        if (originalSideMaterial != null)
            elevationColumnRenderer.sharedMaterial = originalSideMaterial;

        int colorPropertyId = GetRendererColorPropertyId(elevationColumnRenderer);
        if (colorPropertyId != -1)
        {
            MaterialPropertyBlock sideBlock = new MaterialPropertyBlock();
            elevationColumnRenderer.GetPropertyBlock(sideBlock);
            sideBlock.SetColor(colorPropertyId, data.TileColor);
            elevationColumnRenderer.SetPropertyBlock(sideBlock);
        }
    }

    private int GetRendererColorPropertyId(Renderer targetRenderer)
    {
        if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            return -1;

        Material material = targetRenderer.sharedMaterial;

        if (material.HasProperty("_BaseColor"))
            return Shader.PropertyToID("_BaseColor");

        if (material.HasProperty("_Color"))
            return Shader.PropertyToID("_Color");

        return -1;
    }
    //OLD///
    /*private void ApplyRendererVisual(Renderer targetRenderer, TerrainTypeData data)
    {
        if (targetRenderer == null)
            return;

        int colorPropertyId = -1;

        if (targetRenderer.sharedMaterial != null)
        {
            if (targetRenderer.sharedMaterial.HasProperty("_BaseColor"))
                colorPropertyId = Shader.PropertyToID("_BaseColor");
            else if (targetRenderer.sharedMaterial.HasProperty("_Color"))
                colorPropertyId = Shader.PropertyToID("_Color");
        }

        if (data.TileMaterialOverride != null)
            targetRenderer.sharedMaterial = data.TileMaterialOverride;
        else if (originalBaseMaterial != null && targetRenderer == tileRenderer)
            targetRenderer.sharedMaterial = originalBaseMaterial;

        if (colorPropertyId != -1)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(block);
            block.SetColor(colorPropertyId, data.TileColor);
            targetRenderer.SetPropertyBlock(block);
        }
    }*/
    private void ShowOverlay(Color color)
    {
        if (highlightOverlayRenderer == null || overlayColorPropertyId == -1)
            return;

        highlightOverlayRenderer.gameObject.SetActive(true);

        highlightOverlayRenderer.GetPropertyBlock(overlayBlock);
        overlayBlock.SetColor(overlayColorPropertyId, color);
        highlightOverlayRenderer.SetPropertyBlock(overlayBlock);
    }

    public void HideOverlay()
    {
        if (highlightOverlayRenderer == null)
            return;
        
        highlightOverlayRenderer.SetPropertyBlock(null);
        
        highlightOverlayRenderer.gameObject.SetActive(false);
    }
    
    public void ShowOverlayColor(Color color)
    {
        ShowOverlay(color);
    }
    
    public void SetHoverHighlight(Color color)
    {
        ShowOverlay(color);
    }
    public int GetTraversalCost(bool isFinalDestination)
    {
        TerrainTypeData data = CurrentTerrainData;

        if (data == null)
            return movementCost;

        return data.MovementCost + data.MovementPenaltyOnEntry;
    }
    
    private void RefreshDecoration()
    {
        if (spawnedDecoration != null)
        {
            Destroy(spawnedDecoration);
            spawnedDecoration = null;
        }

        TerrainTypeData data = CurrentTerrainData;

        if (data == null)
            return;

        if (data.TileDecorationPrefab == null)
            return;

        Vector3 spawnPosition = transform.position + data.DecorationOffset;

        if (decorationAnchor != null)
            spawnPosition = decorationAnchor.position + data.DecorationOffset;

        spawnedDecoration = Instantiate(
            data.TileDecorationPrefab,
            spawnPosition,
            Quaternion.identity,
            transform
        );
    }
    public void ForceSetWalkable(bool value)
    {
        isWalkable = value;
    }
    public Renderer GetTopRenderer()
    {
        return tileRenderer;
    }
}
