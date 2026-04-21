using System.Collections.Generic;
using UnityEngine;

public class UnitPlacementService : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BuilderUnitRegistry builderUnitRegistry;
    [SerializeField] private Transform playerUnitParent;
    [SerializeField] private Transform enemyUnitParent;

    [Header("Builder Patrol Marker")]
    [SerializeField] private Transform patrolMarkerParent;
    [Tooltip("Assign the same material used by BuilderObstaclePreview to make patrol end markers look exactly like normal builder previews.")]
    [SerializeField] private Material patrolEndMarkerMaterial;
    [SerializeField] private Color patrolEndMarkerColor = new Color(0.3f, 0.7f, 1f, 0.35f);
    [SerializeField] private BuilderObstaclePreview builderObstaclePreview;

    public bool TryPlaceUnit(UnitData unitData, GridTile originTile, int rotationY, BuilderUnitPaintTeam team, bool useCardinalFacing = false)
    {
        return TryPlaceUnit(unitData, originTile, rotationY, team, useCardinalFacing, out _);
    }

    public bool TryPlaceUnit(UnitData unitData, GridTile originTile, int rotationY, BuilderUnitPaintTeam team, bool useCardinalFacing, out PlacedBuilderUnit placedBuilderUnit)
    {
        placedBuilderUnit = null;

        if (unitData == null || unitData.unitPrefab == null || originTile == null || gridManager == null)
            return false;

        List<GridTile> footprintTiles = GetFootprintTiles(originTile, unitData.footprintSize, rotationY);
        if (footprintTiles == null || footprintTiles.Count == 0)
            return false;

        foreach (GridTile tile in footprintTiles)
        {
            if (tile == null || !tile.isWalkable || tile.isOccupied)
                return false;
        }

        Transform targetParent = team == BuilderUnitPaintTeam.Player ? playerUnitParent : enemyUnitParent;

        GameObject spawnedObject = Instantiate(
            unitData.unitPrefab,
            Vector3.zero,
            Quaternion.identity,
            targetParent
        );

        GridUnit gridUnit = spawnedObject.GetComponent<GridUnit>();
        if (gridUnit == null)
        {
            Destroy(spawnedObject);
            return false;
        }

        gridUnit.InitializeFromData(unitData);
        gridUnit.PlaceOnTile(originTile);

        int normalizedRotation = NormalizeRotation(rotationY);

        ApplyUnitRotation(gridUnit, unitData, normalizedRotation, useCardinalFacing);
        gridUnit.transform.position = GetPreviewWorldPosition(unitData, originTile, normalizedRotation);
        gridUnit.transform.localScale = unitData.GetVisualScaleForRotation(normalizedRotation);

        foreach (GridTile tile in footprintTiles)
        {
            tile.SetOccupant(gridUnit.gameObject);
        }

        PlacedBuilderUnit newPlacedBuilderUnit = new PlacedBuilderUnit
        {
            Unit = gridUnit,
            UnitData = unitData,
            PaintTeam = team,
            Origin = originTile.GridPosition,
            PatrolStart = originTile.GridPosition,
            FootprintSize = unitData.footprintSize,
            RotationY = normalizedRotation,
            UseCardinalFacing = useCardinalFacing
        };

        newPlacedBuilderUnit.OccupiedTiles.Clear();
        newPlacedBuilderUnit.OccupiedTiles.AddRange(footprintTiles);

        if (builderUnitRegistry != null)
            builderUnitRegistry.RegisterPlacedUnit(newPlacedBuilderUnit);

        placedBuilderUnit = newPlacedBuilderUnit;
        return true;
    }
    
    public Vector3 GetPreviewWorldPosition(UnitData unitData, GridTile originTile, int rotationY)
    {
        if (unitData == null || originTile == null)
            return Vector3.zero;

        int normalizedRotation = NormalizeRotation(rotationY);
        return GetTileTopCenter(originTile) + unitData.GetVisualOffsetForRotation(normalizedRotation);
    }
    
    public void RefreshPlacedUnitTransform(PlacedBuilderUnit placedUnit)
    {
        if (placedUnit == null || placedUnit.Unit == null || placedUnit.UnitData == null)
            return;

        GridTile originTile = placedUnit.Unit.CurrentTile;
        if (originTile == null)
            return;

        int rotationY = NormalizeRotation(placedUnit.RotationY);

        placedUnit.Unit.PlaceOnTile(originTile);
        ApplyUnitRotation(placedUnit.Unit, placedUnit.UnitData, rotationY, placedUnit.UseCardinalFacing);
        placedUnit.Unit.transform.position = GetPreviewWorldPosition(
            placedUnit.UnitData,
            originTile,
            rotationY
        );
        placedUnit.Unit.transform.localScale = placedUnit.UnitData.GetVisualScaleForRotation(rotationY);
    }

    private List<GridTile> GetFootprintTiles(GridTile originTile, Vector2Int footprintSize, int rotationY)
    {
        List<GridTile> result = new List<GridTile>();
        List<Vector2Int> offsets = GetRotatedFootprintOffsets(footprintSize, rotationY);

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int gridPos = new Vector2Int(originTile.X + offset.x, originTile.Y + offset.y);
            GridTile tile = gridManager.GetTileAt(gridPos);

            if (tile == null)
                return null;

            result.Add(tile);
        }

        return result;
    }

    private List<Vector2Int> GetRotatedFootprintOffsets(Vector2Int footprintSize, int rotationY)
    {
        List<Vector2Int> offsets = new List<Vector2Int>();
        int normalizedRotation = NormalizeRotation(rotationY);

        for (int x = 0; x < Mathf.Max(1, footprintSize.x); x++)
        {
            for (int y = 0; y < Mathf.Max(1, footprintSize.y); y++)
            {
                Vector2Int offset = new Vector2Int(x, y);

                switch (normalizedRotation)
                {
                    case 90:
                        offset = new Vector2Int(y, -x);
                        break;
                    case 180:
                        offset = new Vector2Int(-x, -y);
                        break;
                    case 270:
                        offset = new Vector2Int(-y, x);
                        break;
                }

                offsets.Add(offset);
            }
        }

        return offsets;
    }

    private int NormalizeRotation(int rotationY)
    {
        rotationY %= 360;
        if (rotationY < 0)
            rotationY += 360;

        if (rotationY >= 315 || rotationY < 45) return 0;
        if (rotationY >= 45 && rotationY < 135) return 90;
        if (rotationY >= 135 && rotationY < 225) return 180;
        return 270;
    }

    public void ApplyUnitRotation(GridUnit gridUnit, UnitData unitData, int rotationY, bool useCardinalFacing)
    {
        if (gridUnit == null || unitData == null)
            return;

        int normalizedRotation = NormalizeRotation(rotationY);
        gridUnit.transform.rotation = Quaternion.Euler(
            unitData.GetVisualRotationEulerForRotation(normalizedRotation, useCardinalFacing)
        );

        if (useCardinalFacing)
            gridUnit.RestoreVisualRotation(Quaternion.Euler(0f, normalizedRotation, 0f));
    }

    public bool CreatePatrolEndMarker(PlacedBuilderUnit placedUnit, GridTile endTile)
    {
        if (placedUnit == null || placedUnit.UnitData == null || placedUnit.UnitData.unitPrefab == null || endTile == null)
            return false;

        DestroyPatrolEndMarker(placedUnit);

        Transform parent = patrolMarkerParent != null
            ? patrolMarkerParent
            : enemyUnitParent;

        GameObject markerRoot = new GameObject($"{placedUnit.UnitData.unitName}_PatrolEndMarker");
        markerRoot.transform.SetParent(parent, false);

        GameObject markerVisual = Instantiate(placedUnit.UnitData.unitPrefab, markerRoot.transform);
        markerVisual.name = $"{placedUnit.UnitData.unitPrefab.name}_PatrolEndPreview";

        GridUnit markerUnit = markerVisual.GetComponent<GridUnit>();
        if (markerUnit != null)
            ApplyUnitRotation(markerUnit, placedUnit.UnitData, placedUnit.RotationY, placedUnit.UseCardinalFacing);
        else
            markerVisual.transform.rotation = Quaternion.Euler(
                placedUnit.UnitData.GetVisualRotationEulerForRotation(placedUnit.RotationY, placedUnit.UseCardinalFacing)
            );

        markerVisual.transform.position = GetPreviewWorldPosition(placedUnit.UnitData, endTile, placedUnit.RotationY);
        markerVisual.transform.localScale = placedUnit.UnitData.GetVisualScaleForRotation(placedUnit.RotationY);

        DisableMarkerGameplay(markerVisual);
        ApplyPatrolEndMarkerVisual(markerVisual);

        placedUnit.PatrolEndMarker = markerRoot;
        endTile.SetOccupant(markerRoot);
        return true;
    }

    public void DestroyPatrolEndMarker(PlacedBuilderUnit placedUnit)
    {
        if (placedUnit == null || placedUnit.PatrolEndMarker == null)
            return;

        ClearMarkerOccupancy(placedUnit.PatrolEndMarker);
        Destroy(placedUnit.PatrolEndMarker);
        placedUnit.PatrolEndMarker = null;
    }

    private void DisableMarkerGameplay(GameObject markerVisual)
    {
        if (markerVisual == null)
            return;

        Collider[] colliders = markerVisual.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            collider.enabled = false;

        Behaviour[] behaviours = markerVisual.GetComponentsInChildren<Behaviour>(true);
        foreach (Behaviour behaviour in behaviours)
            behaviour.enabled = false;
    }

    private void ApplyPatrolEndMarkerVisual(GameObject markerVisual)
    {
        if (markerVisual == null)
            return;

        Renderer[] renderers = markerVisual.GetComponentsInChildren<Renderer>(true);
        Material resolvedPreviewMaterial = ResolvePatrolEndMarkerMaterial();
        Color resolvedPreviewColor = ResolvePatrolEndMarkerColor();

        foreach (Renderer renderer in renderers)
        {
            if (resolvedPreviewMaterial != null)
            {
                Material[] mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = resolvedPreviewMaterial;

                renderer.materials = mats;
            }

            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", resolvedPreviewColor);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", resolvedPreviewColor);
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private Material ResolvePatrolEndMarkerMaterial()
    {
        if (patrolEndMarkerMaterial != null)
            return patrolEndMarkerMaterial;

        if (builderObstaclePreview == null)
            builderObstaclePreview = FindFirstObjectByType<BuilderObstaclePreview>();

        return builderObstaclePreview != null ? builderObstaclePreview.PreviewMaterial : null;
    }

    private Color ResolvePatrolEndMarkerColor()
    {
        if (builderObstaclePreview == null)
            builderObstaclePreview = FindFirstObjectByType<BuilderObstaclePreview>();

        return builderObstaclePreview != null
            ? builderObstaclePreview.PreviewColor
            : patrolEndMarkerColor;
    }

    private void ClearMarkerOccupancy(GameObject marker)
    {
        if (marker == null || gridManager == null || gridManager.Grid == null)
            return;

        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                GridTile tile = gridManager.Grid[x, y];
                if (tile != null && tile.OccupyingUnit == marker)
                    tile.SetOccupant(null);
            }
        }
    }
     
    private Vector3 GetTileTopCenter(GridTile tile)
    {
        if (tile == null)
            return Vector3.zero;

        Renderer topRenderer = tile.GetTopRenderer();
        if (topRenderer != null)
            return topRenderer.bounds.center + Vector3.up * topRenderer.bounds.extents.y;

        return tile.transform.position;
    }
}
