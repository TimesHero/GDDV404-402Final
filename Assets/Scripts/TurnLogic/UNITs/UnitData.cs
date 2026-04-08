using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Tactics/Units/Unit Data")]
public class UnitData : ScriptableObject
{
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

    [Header("Presentation")]
    public GameObject unitPrefab;
    public Sprite portrait;
}