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

public enum HiddenMovementPreset
{
    FullMovement,
    SeventyFivePercent,
    Half,
    OneThird,
    OneQuarter,
    CustomDivisor
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

    [Header("Core Stats")]
    [Min(1)] public int maxHP = 10;
    [Min(0)] public int attackPower = 3;
    [Min(0)] public int defense = 0;
    [Min(1)] public int movementPoints = 5;
    [Min(1)] public int attackRange = 1;

    [Header("Combat Actions")]
    [Tooltip("If disabled, this unit can only attack once per turn. Push and enemy barrel searches also count as attacks.")]
    public bool canAttackMultipleTimesPerTurn = false;
    [Tooltip("Only used when Can Attack Multiple Times Per Turn is enabled. This is the maximum number of attacks/actions that count as attacks in one turn.")]
    [Min(1)] public int attacksPerTurn = 1;

    [Header("Stealth Combat")]
    [Tooltip("If enabled, this unit can later use Backstab rules when attacking from stealth. Not fully implemented yet.")]
    public bool canBackstab = false;
    [Tooltip("Damage multiplier for future Backstab attacks. Example: 2 means double damage.")]
    [Min(1f)] public float backstabDamageMultiplier = 2f;
    [Tooltip("Flat bonus damage added to future Backstab attacks after the multiplier.")]
    [Min(0)] public int backstabBonusDamage = 0;

    [Header("Movement")]
    [Min(0)] public int maxClimbHeight = 1;

    [Header("Stealth")]
    [Tooltip("If disabled, this unit cannot enter barrels.")]
    public bool canHideInBarrel = true;
    [Tooltip("Preset used to reduce movement while carrying a barrel. Example: Half turns 6 remaining movement into 3.")]
    public HiddenMovementPreset hiddenMovementPreset = HiddenMovementPreset.Half;
    [Tooltip("Only used when Hidden Movement Preset is Custom Divisor. Remaining movement is divided by this value when entering a barrel.")]
    [Min(1f)] public float customHiddenMovementDivisor = 2f;
    [Tooltip("World movement animation speed while this unit is hidden inside a carried barrel. Set to 0 to keep the normal movement speed.")]
    [Min(0f)] public float movementSpeedWhileHidden = 0f;

    [Header("Push")]
    [Tooltip("If enabled, this unit can use the Push action from the world action menu.")]
    public bool canPush = true;
    [Tooltip("Base number of tiles this unit pushes a valid target before weight modifiers are applied.")]
    [Min(1)] public int pushDistance = 1;
    [Tooltip("If disabled, this unit cannot be moved by Push actions. Useful for heavy enemies, bosses, or fixed units.")]
    public bool canBePushed = true;
    [Tooltip("If enabled, Push compares the pusher weight against the target weight. The pusher must be heavier than the target. Larger weight differences can increase push distance using Push Distance Per Weight Difference.")]
    public bool usePushWeightSystem = false;
    [Tooltip("This unit's push weight. When the weight system is enabled on either unit, a pusher must have more weight than the target to push it.")]
    [Min(0)] public int pushWeight = 1;
    [Tooltip("Extra push distance gained per full amount of weight difference. Example: if this is 2 and the pusher is 4 weight heavier, push distance gains +2. Set to 0 to require weight advantage without increasing distance.")]
    [Min(0)] public int pushDistancePerWeightDifference = 0;

    [Header("Vision")]
    [Min(1)] public int visionRange = 5;
    [Range(0f, 360f)] public float visionAngle = 90f;

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

    [Header("Runtime Prefab")]
    public GameObject unitPrefab;

    [Header("Future / Not Used Yet - Identity")]
    [TextArea] public string description;
    public UnitRole role;
    public UnitTeam defaultTeam;

    [Header("Future / Not Used Yet - Combat Tags")]
    public AttackType attackType = AttackType.Melee;
    public ElementType elementType = ElementType.None;

    [Header("Future / Not Used Yet - AI")]
    public AIType aiType = AIType.None;

    [Header("Future / Not Used Yet - Abilities")]
    public List<UnitAbilityData> abilities = new List<UnitAbilityData>();
    public UnitAbilityData ultimateAbility;

    [Header("Future / Not Used Yet - Presentation")]
    public Sprite portrait;

    public bool UseRotationPresets => useRotationPresets;

    public float HiddenMovementMultiplier
    {
        get
        {
            switch (hiddenMovementPreset)
            {
                case HiddenMovementPreset.FullMovement:
                    return 1f;
                case HiddenMovementPreset.SeventyFivePercent:
                    return 0.75f;
                case HiddenMovementPreset.Half:
                    return 0.5f;
                case HiddenMovementPreset.OneThird:
                    return 1f / 3f;
                case HiddenMovementPreset.OneQuarter:
                    return 0.25f;
                case HiddenMovementPreset.CustomDivisor:
                    return 1f / Mathf.Max(1f, customHiddenMovementDivisor);
                default:
                    return 0.5f;
            }
        }
    }

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
        return GetVisualRotationEulerForRotation(rotationY, false);
    }

    public Vector3 GetVisualRotationEulerForRotation(int rotationY, bool useCardinalFacing)
    {
        int normalizedRotation = NormalizeRotation(rotationY);

        if (!useRotationPresets)
        {
            Vector3 euler = localRotationEuler;
            euler.y = useCardinalFacing
                ? normalizedRotation
                : euler.y + normalizedRotation;
            return euler;
        }

        UnitRotationTransformPreset preset = GetTransformPresetForRotation(normalizedRotation);
        if (preset != null)
        {
            if (useCardinalFacing)
            {
                Vector3 presetEuler = preset.VisualRotationEuler;
                presetEuler.y = normalizedRotation;
                return presetEuler;
            }

            return preset.VisualRotationEuler;
        }

        Vector3 fallbackEuler = localRotationEuler;
        fallbackEuler.y = useCardinalFacing
            ? normalizedRotation
            : fallbackEuler.y + normalizedRotation;
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
