using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelLayoutData
{
    public int width;
    public int height;

    public List<TileLayoutData> tiles = new List<TileLayoutData>();
    public List<ObstacleLayoutData> obstacles = new List<ObstacleLayoutData>();
    public List<UnitLayoutData> units = new List<UnitLayoutData>();
}

[Serializable]
public class TileLayoutData
{
    public int x;
    public int y;
    public string terrainType;
    public int elevation;
}

[Serializable]
public class ObstacleLayoutData
{
    public string obstacleName;
    public int originX;
    public int originY;
}

[Serializable]
public class UnitLayoutData
{
    public string unitName;
    public int x;
    public int y;
    public string team;
}