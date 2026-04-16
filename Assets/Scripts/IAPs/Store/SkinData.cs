using UnityEngine;

[CreateAssetMenu(fileName = "NewSkinData", menuName = "Store/Skin Data")]
public class SkinData : ScriptableObject
{
    public string skinID; 
    public string skinName;
    public int gemCost;
    public Material skinMaterial;
    public bool isUnlockedByDefault;
}