using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    private Dictionary<TerrainType, TerrainTypeData> terrainLookup;

    private void Awake()
    {
        LoadTerrainData();
    }

    private void LoadTerrainData()
    {
        terrainLookup = new Dictionary<TerrainType, TerrainTypeData>();

        TerrainTypeData[] allTerrainData = Resources.LoadAll<TerrainTypeData>("TerrainTypes");

        if (allTerrainData.Length == 0)
        {
            Debug.LogError("TileManager: No TerrainTypeData found in Resources/TerrainTypes");
            return;
        }

        foreach (TerrainTypeData data in allTerrainData)
        {
            if (data == null)
                continue;

            if (terrainLookup.ContainsKey(data.TerrainType))
            {
                Debug.LogWarning($"Duplicate TerrainTypeData for {data.TerrainType}");
                continue;
            }

            terrainLookup.Add(data.TerrainType, data);
        }

        Debug.Log($"TileManager: Loaded {terrainLookup.Count} terrain types.");
    }

    public TerrainTypeData GetTerrainData(TerrainType terrainType)
    {
        if (terrainLookup == null)
            LoadTerrainData();

        if (terrainLookup.TryGetValue(terrainType, out TerrainTypeData data))
            return data;

        Debug.LogWarning($"No TerrainTypeData found for {terrainType}");
        return null;
    }
}