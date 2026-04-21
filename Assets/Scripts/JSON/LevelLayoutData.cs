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
    public List<InteractableLayoutData> interactables = new List<InteractableLayoutData>();
    public List<ObjectiveLayoutData> objectives = new List<ObjectiveLayoutData>();
    public bool loseWhenSeen;
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
    public int rotationY;
}

[Serializable]
public class UnitLayoutData
{
    public string unitId;
    public int x;
    public int y;
    public int rotationY;
    public bool useCardinalFacing;
    public string team;
    public EnemyAIBehavior enemyBehavior;
    public bool hasPatrolRoute;
    public int patrolStartX;
    public int patrolStartY;
    public int patrolEndX;
    public int patrolEndY;
}

[Serializable]
public class InteractableLayoutData
{
    public string interactableId;
    public int x;
    public int y;
    public int rotationY;
}

[Serializable]
public class ObjectiveTargetTileData
{
    public int x;
    public int y;
}

[Serializable]
public class ObjectiveLayoutData
{
    public WinConditionType winConditionType;
    public int surviveTurnCount;

    // Legacy single-tile support
    public int targetX;
    public int targetY;

    public string targetInteractableId;

    // New multi-tile reach support
    public List<ObjectiveTargetTileData> targetTiles = new List<ObjectiveTargetTileData>();
}
