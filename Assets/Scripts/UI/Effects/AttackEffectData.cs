using UnityEngine;

[CreateAssetMenu(fileName = "AttackEffect_", menuName = "Units/Attack Effect Data")]
public class AttackEffectData : ScriptableObject
{
    [Header("Prefab")]
    [SerializeField] private GameObject effectPrefab;

    [Header("Transform")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 effectScale = Vector3.one;

    [Header("Animation")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float popInDuration = 0.1f;
    [SerializeField] private float riseAmount = 0.25f;

    public GameObject EffectPrefab => effectPrefab;
    public Vector3 PositionOffset => positionOffset;
    public Vector3 EffectScale => effectScale;
    public float Duration => duration;
    public float PopInDuration => popInDuration;
    public float RiseAmount => riseAmount;
}