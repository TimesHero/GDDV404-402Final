using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UnitRotationTransformPreset
{
    public int RotationY;
    public Vector3 VisualOffset;
    public Vector3 VisualRotationEuler;
    public Vector3 VisualScale = Vector3.one;
}

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Tactics/Units/Unit Data")]
public class UnitData : ScriptableObject
{
    [SerializeField] private string unitId;
    public string UnitId => unitId;

    [Header("Builder / Team")]
    public UnitTeam unitTeam = UnitTeam.Player;

    [Header("Identity")]
    public string unitName;
    [TextArea] public string description;
    public UnitRole role;
    public UnitTeam defaultTeam;

    [Header("Core Stats")]
    [Min(1)] public int maxHP = 10;
    [Min(0)] public int attackPower = 3;
    [Min(0)] public int defense = 0;
    [Min(1)] public int movementPoints = 5;
    [Min(1)] public int attackRange = 1;

    [Header("Movement")]
    [Min(0)] public int maxClimbHeight = 1;

    [Header("Combat")]
    public AttackType attackType = AttackType.Melee;
    public ElementType elementType = ElementType.None;

    [Header("Vision")]
    [Min(1)] public int visionRange = 5;
    [Range(0f, 360f)] public float visionAngle = 90f;

    [Header("AI")]
    public AIType aiType = AIType.None;

    [Header("Abilities")]
    public List<UnitAbilityData> abilities = new List<UnitAbilityData>();
    public UnitAbilityData ultimateAbility;

    [Header("Builder Footprint")]
    public Vector2Int footprintSize = Vector2Int.one;

    [Header("Rotation Transform Mode")]
    [SerializeField] private bool useRotationPresets = false;

    [Header("Optional Rotation Presets")]
    [SerializeField] private UnitRotationTransformPreset rotation0Preset;
    [SerializeField] private UnitRotationTransformPreset rotation90Preset;
    [SerializeField] private UnitRotationTransformPreset rotation180Preset;
    [SerializeField] private UnitRotationTransformPreset rotation270Preset;

    [Header("Transform")]
    public Vector3 localOffset = Vector3.zero;
    public Vector3 localRotationEuler = Vector3.zero;
    public Vector3 localScale = Vector3.one;

    [Header("Presentation")]
    public GameObject unitPrefab;
    public Sprite portrait;

    public bool UseRotationPresets => useRotationPresets;

    public UnitRotationTransformPreset GetTransformPresetForRotation(int rotationY)
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

        UnitRotationTransformPreset preset = GetTransformPresetForRotation(rotationY);
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

        UnitRotationTransformPreset preset = GetTransformPresetForRotation(rotationY);
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

        UnitRotationTransformPreset preset = GetTransformPresetForRotation(rotationY);
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