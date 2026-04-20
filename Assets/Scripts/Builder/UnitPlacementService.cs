using System.Collections.Generic;
using UnityEngine;

public class UnitPlacementService : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BuilderUnitRegistry builderUnitRegistry;
    [SerializeField] private Transform playerUnitParent;
    [SerializeField] private Transform enemyUnitParent;

    public bool TryPlaceUnit(UnitData unitData, GridTile originTile, int rotationY, BuilderUnitPaintTeam team)
    {
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

        gridUnit.transform.rotation = Quaternion.Euler(unitData.GetVisualRotationEulerForRotation(normalizedRotation));
        gridUnit.transform.position += unitData.GetVisualOffsetForRotation(normalizedRotation);
        gridUnit.transform.localScale = unitData.GetVisualScaleForRotation(normalizedRotation);

        foreach (GridTile tile in footprintTiles)
        {
            tile.SetOccupant(gridUnit.gameObject);
        }

        PlacedBuilderUnit placedBuilderUnit = new PlacedBuilderUnit
        {
            Unit = gridUnit,
            UnitData = unitData,
            Origin = originTile.GridPosition,
            FootprintSize = unitData.footprintSize,
            RotationY = normalizedRotation
        };

        placedBuilderUnit.OccupiedTiles.Clear();
        placedBuilderUnit.OccupiedTiles.AddRange(footprintTiles);

        if (builderUnitRegistry != null)
            builderUnitRegistry.RegisterPlacedUnit(placedBuilderUnit);

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
        placedUnit.Unit.transform.rotation = Quaternion.Euler(
            placedUnit.UnitData.GetVisualRotationEulerForRotation(rotationY)
        );
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