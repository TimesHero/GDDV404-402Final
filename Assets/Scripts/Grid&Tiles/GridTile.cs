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
    
    [Header("Visuals")]
    [SerializeField] private Renderer tileRenderer;
    [SerializeField] private Renderer highlightOverlayRenderer;
    
    [Header("References")]
    [SerializeField] private TileManager tileManager;

    private MaterialPropertyBlock baseBlock;
    private MaterialPropertyBlock overlayBlock;
    private int baseColorPropertyId = -1;
    private int overlayColorPropertyId = -1;
    
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
    }
    private void ApplyTerrainVisualOnly()
    {
        if (tileManager == null || tileRenderer == null || baseColorPropertyId == -1)
            return;

        TerrainTypeData data = tileManager.GetTerrainData(terrainType);
        if (data == null)
            return;

        tileRenderer.GetPropertyBlock(baseBlock);
        baseBlock.SetColor(baseColorPropertyId, data.TileColor);
        tileRenderer.SetPropertyBlock(baseBlock);
    }
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

        int cost = data.MovementCost;

        if (!isFinalDestination)
            cost += data.MovementPenaltyOnEntry;

        return cost;
    }
}
