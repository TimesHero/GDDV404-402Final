using System.Collections.Generic;
using UnityEngine;

public class InteractablePlacementService : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private InteractableRegistry interactableRegistry;
    [SerializeField] private Transform interactableParent;

    public bool TryPlaceInteractable(InteractableData data, GridTile originTile, int rotationY)
    {
        if (data == null || originTile == null || gridManager == null || data.prefab == null)
            return false;

        List<GridTile> occupiedTiles = GetFootprintTiles(originTile, data.footprint, rotationY);
        if (occupiedTiles == null || occupiedTiles.Count == 0)
            return false;

        foreach (GridTile tile in occupiedTiles)
        {
            if (tile == null)
                return false;

            if (data.blocksMovement && (!tile.isWalkable || tile.isOccupied))
                return false;
        }

        int normalizedRotation = NormalizeRotation(rotationY);

        GameObject interactableObject = Instantiate(
            data.prefab,
            GetPreviewWorldPosition(data, originTile, normalizedRotation),
            Quaternion.Euler(data.GetVisualRotationEulerForRotation(normalizedRotation)),
            interactableParent
        );

        interactableObject.transform.localScale = data.GetVisualScaleForRotation(normalizedRotation);

        PlacedInteractable placedInteractable = interactableObject.GetComponent<PlacedInteractable>();
        if (placedInteractable == null)
            placedInteractable = interactableObject.AddComponent<PlacedInteractable>();

        placedInteractable.Data = data;
        placedInteractable.Origin = new Vector2Int(originTile.X, originTile.Y);
        placedInteractable.RotationY = normalizedRotation;
        placedInteractable.OccupiedGridPositions.Clear();

        foreach (GridTile tile in occupiedTiles)
        {
            placedInteractable.OccupiedGridPositions.Add(new Vector2Int(tile.X, tile.Y));

            if (data.blocksMovement)
                tile.isOccupied = true;
        }

        if (interactableRegistry != null)
            interactableRegistry.Register(placedInteractable);

        return true;
    }

    public bool RemoveInteractableAtTile(GridTile tile)
    {
        if (tile == null || interactableRegistry == null)
            return false;

        List<PlacedInteractable> placedInteractables = interactableRegistry.GetAllPlacedInteractables();
        if (placedInteractables == null)
            return false;

        for (int i = placedInteractables.Count - 1; i >= 0; i--)
        {
            PlacedInteractable placed = placedInteractables[i];
            if (placed == null)
                continue;

            bool containsTile = false;

            foreach (Vector2Int pos in placed.OccupiedGridPositions)
            {
                if (pos == new Vector2Int(tile.X, tile.Y))
                {
                    containsTile = true;
                    break;
                }
            }

            if (!containsTile)
                continue;

            ClearOccupiedTiles(placed);
            interactableRegistry.Unregister(placed);
            Destroy(placed.gameObject);
            return true;
        }

        return false;
    }

    public void ClearAllInteractables()
    {
        if (interactableRegistry == null)
            return;

        List<PlacedInteractable> placedInteractables = interactableRegistry.GetAllPlacedInteractables();
        if (placedInteractables == null)
            return;

        for (int i = placedInteractables.Count - 1; i >= 0; i--)
        {
            PlacedInteractable placed = placedInteractables[i];
            if (placed == null)
                continue;

            ClearOccupiedTiles(placed);
            Destroy(placed.gameObject);
        }

        interactableRegistry.ClearAll();
    }
    
    public PlacedInteractable GetPlacedInteractableAtTile(GridTile tile)
    {
        if (tile == null || interactableRegistry == null)
            return null;

        List<PlacedInteractable> placedInteractables = interactableRegistry.GetAllPlacedInteractables();
        if (placedInteractables == null)
            return null;

        Vector2Int targetPos = tile.GridPosition;

        foreach (PlacedInteractable placed in placedInteractables)
        {
            if (placed == null)
                continue;

            foreach (Vector2Int occupiedPos in placed.OccupiedGridPositions)
            {
                if (occupiedPos == targetPos)
                    return placed;
            }
        }

        return null;
    }

    public void RefreshPlacedInteractableTransform(PlacedInteractable placed)
    {
        if (placed == null || placed.Data == null || placed.gameObject == null || gridManager == null)
            return;

        GridTile originTile = gridManager.GetTileAt(placed.Origin);
        if (originTile == null)
            return;

        int rotationY = NormalizeRotation(placed.RotationY);

        placed.transform.position = GetPreviewWorldPosition(placed.Data, originTile, rotationY);
        placed.transform.rotation = Quaternion.Euler(placed.Data.GetVisualRotationEulerForRotation(rotationY));
        placed.transform.localScale = placed.Data.GetVisualScaleForRotation(rotationY);
    }
    
    public void SetInteractableElevation(PlacedInteractable placed, int newElevation)
    {
        if (placed == null || gridManager == null)
            return;

        foreach (Vector2Int gridPos in placed.OccupiedGridPositions)
        {
            GridTile tile = gridManager.GetTileAt(gridPos);
            if (tile == null)
                continue;

            TileElevation tileElevation = tile.GetComponent<TileElevation>();
            if (tileElevation != null)
                tileElevation.SetElevation(newElevation);
        }

        RefreshPlacedInteractableTransform(placed);
    }

    public Vector3 GetPreviewWorldPosition(InteractableData data, GridTile originTile, int rotationY)
    {
        if (data == null || originTile == null)
            return Vector3.zero;

        int normalizedRotation = NormalizeRotation(rotationY);
        return GetTileTopCenter(originTile) + data.GetVisualOffsetForRotation(normalizedRotation);
    }

    private void ClearOccupiedTiles(PlacedInteractable placed)
    {
        if (placed == null || placed.Data == null || !placed.Data.blocksMovement || gridManager == null)
            return;

        foreach (Vector2Int pos in placed.OccupiedGridPositions)
        {
            GridTile tile = gridManager.GetTileAt(pos);
            if (tile != null)
                tile.isOccupied = false;
        }
    }

    private List<GridTile> GetFootprintTiles(GridTile originTile, Vector2Int footprint, int rotationY)
    {
        List<GridTile> result = new List<GridTile>();
        List<Vector2Int> offsets = GetRotatedFootprintOffsets(footprint, rotationY);

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int pos = new Vector2Int(originTile.X + offset.x, originTile.Y + offset.y);
            GridTile tile = gridManager.GetTileAt(pos);

            if (tile == null)
                return null;

            result.Add(tile);
        }

        return result;
    }

    private List<Vector2Int> GetRotatedFootprintOffsets(Vector2Int footprint, int rotationY)
    {
        List<Vector2Int> offsets = new List<Vector2Int>();
        int normalizedRotation = NormalizeRotation(rotationY);

        for (int x = 0; x < Mathf.Max(1, footprint.x); x++)
        {
            for (int y = 0; y < Mathf.Max(1, footprint.y); y++)
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