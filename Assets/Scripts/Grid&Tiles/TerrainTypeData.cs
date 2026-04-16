using UnityEngine;

[CreateAssetMenu(fileName = "TerrainType_", menuName = "Grid/Terrain Type Data")]
public class TerrainTypeData : ScriptableObject
{
    [Header("ID")]
    [SerializeField] private TerrainType terrainType;

    [Header("Gameplay")]
    [SerializeField] private int movementCost = 1;
    [SerializeField] private int damageOnEnter = 0;
    [SerializeField] private int movementPenaltyOnStop = 0;
    [SerializeField] private int damageOnStop = 0;
    [SerializeField] private int movementPenaltyOnEntry = 0;
    [SerializeField] private bool isWalkable = true;

    [Header("Visual")]
    [SerializeField] private Color tileColor = Color.white;
    
    [SerializeField] private GameObject tileDecorationPrefab;
    [SerializeField] private Vector3 decorationOffset = Vector3.zero;
    
    [Header("Materials")]
    [SerializeField] private Material topMaterialOverride;
    [SerializeField] private Material sideMaterialOverride;

    public Material TopMaterialOverride => topMaterialOverride;
    public Material SideMaterialOverride => sideMaterialOverride;
    
    [Header("Surface Visuals")]
    [SerializeField] private Texture2D debugPreviewTexture;

    public TerrainType TerrainType => terrainType;
    public int MovementCost => movementCost;
    public int DamageOnEnter => damageOnEnter;
    public int MovementPenaltyOnStop => movementPenaltyOnStop;
    public int DamageOnStop => damageOnStop;
    public int MovementPenaltyOnEntry => movementPenaltyOnEntry;
    public bool IsWalkable => isWalkable;
    public Color TileColor => tileColor;
    
    public GameObject TileDecorationPrefab => tileDecorationPrefab;
    public Vector3 DecorationOffset => decorationOffset;
    
    
    public Texture2D DebugPreviewTexture => debugPreviewTexture;
}