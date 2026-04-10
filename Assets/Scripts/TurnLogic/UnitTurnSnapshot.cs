using UnityEngine;

[System.Serializable]
public class UnitTurnSnapshot
{
    public GridUnit unit;
    public Vector2Int gridPosition;
    public int currentHP;
    public bool wasDead;
    public bool hasMovedThisTurn;
    public bool hasAttackedThisTurn;
    public Quaternion visualRotation;
}