using System;
using UnityEngine;

[Serializable]
public class InteractableRotationTransformPreset
{
    public int RotationY;
    public Vector3 VisualOffset;
    public Vector3 VisualRotationEuler;
    public Vector3 VisualScale = Vector3.one;
}

[CreateAssetMenu(fileName = "InteractableData", menuName = "Game/Interactables/Interactable Data")]
public class InteractableData : ScriptableObject
{
    [Header("Identity")]
    public string interactableId;
    public string displayName;
    public InteractableType interactableType = InteractableType.None;

    [Header("Rotation Transform Mode")]
    [SerializeField] private bool useRotationPresets = false;

    [Header("Optional Rotation Presets")]
    [SerializeField] private InteractableRotationTransformPreset rotation0Preset;
    [SerializeField] private InteractableRotationTransformPreset rotation90Preset;
    [SerializeField] private InteractableRotationTransformPreset rotation180Preset;
    [SerializeField] private InteractableRotationTransformPreset rotation270Preset;

    [Header("Transform")]
    public Vector3 localOffset = Vector3.zero;
    public Vector3 localRotationEuler = Vector3.zero;
    public Vector3 localScale = Vector3.one;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Grid Footprint")]
    public Vector2Int footprint = Vector2Int.one;
    public bool blocksMovement = true;

    public bool UseRotationPresets => useRotationPresets;

    public InteractableRotationTransformPreset GetTransformPresetForRotation(int rotationY)
    {
        rotationY = NormalizeRotation(rotationY);

        switch (rotationY)
        {
            case 90:
                return rotation90Preset;
            case 180:
                return rotation180Preset;
            case 270:
                return rotation270Preset;
            default:
                return rotation0Preset;
        }
    }

    public Vector3 GetVisualOffsetForRotation(int rotationY)
    {
        if (!useRotationPresets)
            return localOffset;

        InteractableRotationTransformPreset preset = GetTransformPresetForRotation(rotationY);
        if (preset != null)
            return preset.VisualOffset;

        return localOffset;
    }

    public Vector3 GetVisualRotationEulerForRotation(int rotationY)
    {
        if (!useRotationPresets)
        {
            Vector3 euler = localRotationEuler;
            euler.y += NormalizeRotation(rotationY);
            return euler;
        }

        InteractableRotationTransformPreset preset = GetTransformPresetForRotation(rotationY);
        if (preset != null)
            return preset.VisualRotationEuler;

        Vector3 fallbackEuler = localRotationEuler;
        fallbackEuler.y += NormalizeRotation(rotationY);
        return fallbackEuler;
    }

    public Vector3 GetVisualScaleForRotation(int rotationY)
    {
        if (!useRotationPresets)
            return localScale;

        InteractableRotationTransformPreset preset = GetTransformPresetForRotation(rotationY);
        if (preset != null)
            return preset.VisualScale;

        return localScale;
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
}