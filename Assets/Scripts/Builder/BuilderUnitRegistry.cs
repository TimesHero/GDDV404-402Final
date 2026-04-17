using System.Collections.Generic;
using UnityEngine;

public class BuilderUnitRegistry : MonoBehaviour
{
    private readonly List<PlacedBuilderUnit> placedUnits = new List<PlacedBuilderUnit>();
    private readonly Dictionary<GridTile, PlacedBuilderUnit> tileToUnitMap = new Dictionary<GridTile, PlacedBuilderUnit>();

    public IReadOnlyList<PlacedBuilderUnit> GetPlacedUnits()
    {
        return placedUnits;
    }

    public PlacedBuilderUnit GetPlacedUnitAtTile(GridTile tile)
    {
        if (tile == null)
            return null;

        if (tileToUnitMap.TryGetValue(tile, out PlacedBuilderUnit placedUnit))
            return placedUnit;

        return null;
    }

    public void RegisterPlacedUnit(PlacedBuilderUnit placedUnit)
    {
        if (placedUnit == null || placedUnit.Unit == null)
            return;

        if (!placedUnits.Contains(placedUnit))
            placedUnits.Add(placedUnit);

        foreach (GridTile tile in placedUnit.OccupiedTiles)
        {
            if (tile == null)
                continue;

            tileToUnitMap[tile] = placedUnit;
        }
    }

    public void UnregisterPlacedUnit(PlacedBuilderUnit placedUnit)
    {
        if (placedUnit == null)
            return;

        foreach (GridTile tile in placedUnit.OccupiedTiles)
        {
            if (tile == null)
                continue;

            if (tileToUnitMap.TryGetValue(tile, out PlacedBuilderUnit current) && current == placedUnit)
                tileToUnitMap.Remove(tile);
        }

        placedUnits.Remove(placedUnit);
    }

    public void ClearAll()
    {
        tileToUnitMap.Clear();
        placedUnits.Clear();
    }
}