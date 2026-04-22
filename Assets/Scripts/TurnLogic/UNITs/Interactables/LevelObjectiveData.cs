using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelObjectiveData
{
    public WinConditionType winConditionType = WinConditionType.None;

    [Header("Survive")]
    public int surviveTurnCount = 0;

    [Header("Reach")]
    public List<Vector2Int> targetGridPositions = new List<Vector2Int>();

    [Header("Interact")]
    public string targetInteractableId;
}