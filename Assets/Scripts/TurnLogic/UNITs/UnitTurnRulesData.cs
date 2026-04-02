using UnityEngine;

[CreateAssetMenu(fileName = "UnitTurnRules_", menuName = "Units/Unit Turn Rules")]
public class UnitTurnRulesData : ScriptableObject
{
    [Header("Turn Rules")]
    [SerializeField] private bool canAttackAfterMoving = true;
    [SerializeField] private bool canMoveAfterAttacking = false;
    
    [Header("Selection Behaviour for Player Units")]
    [SerializeField] private bool autoDeselectWhenOutOfActions = true;

    public bool AutoDeselectWhenOutOfActions => autoDeselectWhenOutOfActions;

    public bool CanAttackAfterMoving => canAttackAfterMoving;
    public bool CanMoveAfterAttacking => canMoveAfterAttacking;
}