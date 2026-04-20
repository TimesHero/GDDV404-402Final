using UnityEngine;

[CreateAssetMenu(fileName = "ReachTileMarkerData", menuName = "Game/Objectives/Reach Tile Marker Data")]
public class ReachTileMarkerData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject markerPrefab;

    [Header("Transform")]
    public Vector3 localOffset = Vector3.zero;
    public Vector3 localRotationEuler = Vector3.zero;
    public Vector3 localScale = Vector3.one;
}