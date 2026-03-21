using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    [Header("Terrain Database")]
    [SerializeField] private List<TerrainTypeData> terrainTypes = new List<TerrainTypeData>();

    private Dictionary<TerrainType, TerrainTypeData> terrainLookup;

    private void Awake()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        terrainLookup = new Dictionary<TerrainType, TerrainTypeData>();

        foreach (TerrainTypeData data in terrainTypes)
        {
            if (data == null)
                continue;

            if (terrainLookup.ContainsKey(data.TerrainType))
            {
                Debug.LogWarning($"Duplicate TerrainTypeData found for {data.TerrainType}");
                continue;
            }

            terrainLookup.Add(data.TerrainType, data);
        }
    }

    public TerrainTypeData GetTerrainData(TerrainType terrainType)
    {
        if (terrainLookup == null)
            BuildLookup();

        if (terrainLookup.TryGetValue(terrainType, out TerrainTypeData data))
            return data;

        Debug.LogWarning($"No TerrainTypeData found for {terrainType}");
        return null;
    }
}