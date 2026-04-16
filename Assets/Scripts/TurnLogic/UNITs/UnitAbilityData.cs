using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Tactics/Units/Ability Data")]
public class UnitAbilityData : ScriptableObject
{
    [Header("Identity")]
    public string abilityName;
    [TextArea] public string description;

    [Header("Rules")]
    public int range = 1;
    public int power = 0;
    public int cooldown = 0;
    public bool endsTurn = true;
}