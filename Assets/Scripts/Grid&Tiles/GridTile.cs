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
    public bool isWalkable = true;
    public int movementCost = 1;
    public TerrainType terrainType = TerrainType.Ground;

    [Header("Occupancy")] 
    public bool isOccupied;
    public GameObject occupyingUnit;
    
    [Header("Visuals")]
    [SerializeField] private Renderer tileRenderer;
    private MaterialPropertyBlock propertyBlock;
    private int colorPropertyId = -1;
    
    public Vector2Int GridPosition => new Vector2Int(X, Y);

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<Renderer>();

        CacheColorProperty();
    }
    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;
        gameObject.name = $"Tile_{x}_{y}";
        RefreshTerrainDefaults();
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
        RefreshTerrainDefaults();
    }
    private void RefreshTerrainDefaults()
    {
        switch (terrainType)
        {
            case TerrainType.Ground:
                movementCost = 1;
                isWalkable = true;
                SetHighlight( Color.white );
                break;
            case TerrainType.Forest:
                movementCost = 1;
                isWalkable = true;
                SetHighlight( Color.green );
                break;
            case TerrainType.Water:
                movementCost = 3;
                isWalkable = true;
                SetHighlight( Color.cyan );
                break;
            case TerrainType.Hazard:
                movementCost = 5;
                isWalkable = false;
                SetHighlight( Color.yellow );
                break;
            case TerrainType.Blocked:
                movementCost = 999;
                isWalkable = false;
                SetHighlight( Color.red );
                break;
        }
    }
}
