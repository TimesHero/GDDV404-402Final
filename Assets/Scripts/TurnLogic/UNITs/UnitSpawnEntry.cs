using UnityEngine;

[System.Serializable]
public class UnitSpawnEntry
{
    [Header("Unit To Spawn")]
    public UnitData unitData;

    [Header("Spawn Position")]
    public Vector2Int spawnGridPosition;
}