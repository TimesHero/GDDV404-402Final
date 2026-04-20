using System.Collections.Generic;
using UnityEngine;

public class PlacedInteractable : MonoBehaviour
{
    public InteractableData Data;
    public Vector2Int Origin;
    public int RotationY;
    public List<Vector2Int> OccupiedGridPositions = new List<Vector2Int>();
}