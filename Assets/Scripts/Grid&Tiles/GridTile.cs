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
    
    [Header("References")]
    [SerializeField] private TileManager tileManager;

    private MaterialPropertyBlock propertyBlock;
    private int colorPropertyId = -1;

    
    public Vector2Int GridPosition => new Vector2Int(X, Y);
    
    public TerrainType TerrainType
    {
        get => terrainType;
        set => terrainType = value;
    }

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<Renderer>();

        CacheColorProperty();
    }
    public void Initialize(int x, int y, TileManager manager)
    {
        X = x;
        Y = y;
        tileManager = manager;
        gameObject.name = $"Tile_{x}_{y}";
        ApplyTerrainSettings();
    }
    
    public void SetOccupant(GameObject unit)
    {
        occupyingUnit = unit;
        isOccupied = unit != null;
    }
    
    private void CacheColorProperty()
    {
        if (tileRenderer == null || tileRenderer.sharedMaterial == null) 
            return;
        Material material = tileRenderer.sharedMaterial;
        
        if (material.HasProperty("_BaseColor"))
        {
            colorPropertyId = Shader.PropertyToID("_BaseColor");
        }
        else if (material.HasProperty("_Color"))
        {
            colorPropertyId = Shader.PropertyToID("_Color");
        }
        else
        {
            Debug.LogWarning($"{name} material does not expose _BaseColor or _Color.");
        }
    }

    public void SetHighlight(Color color)
    {
        if (tileRenderer == null || colorPropertyId == -1) 
            return;
        
        tileRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyId, color);
        tileRenderer.SetPropertyBlock(propertyBlock);
    }
    public void ResetHighlight()
    {
        ApplyTerrainSettings();
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
        if (tileManager == null)
            return;

        TerrainTypeData data = tileManager.GetTerrainData(terrainType);

        if (data == null)
            return;

        SetHighlight(data.TileColor);
    }
    public void ShowAsPath()
    {
        SetHighlight(Color.darkBlue);
    }
    public void ShowAsStart()
    {
        SetHighlight(Color.darkGreen);
    }
    public void ShowAsTarget()
    {
        SetHighlight(Color.darkRed);
    }
    public void ShowAsReachable()
    {
        SetHighlight(new Color(0f, 1f, 1f, 0.6f));
    }

    public void ShowAsBlockedMove()
    {
        SetHighlight(Color.black);
    }
}
