using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class EnemyStateFeedbackEntry
{
    public EnemyAIState state = EnemyAIState.Alert;

    [Header("Visual Effect")]
    public GameObject effectPrefab;
    public Vector3 positionOffset = new Vector3(0f, 2f, 0f);
    public Vector3 rotationEuler = Vector3.zero;
    public Vector3 effectScale = Vector3.one;

    [Header("Attachment")]
    [Tooltip("When enabled, the effect becomes a child of the enemy so it follows the enemy while the enemy moves.")]
    public bool attachToSource = true;

    [Header("Display Timing")]
    [Min(0f)] public float duration = 0.5f;
    [FormerlySerializedAs("popInDuration")]
    [Tooltip("How long the prefab takes to float upward. Set this to 0 if you want it to snap up instantly.")]
    [Min(0f)] public float liftDuration = 0.1f;
    public float riseAmount = 0.25f;

    [Header("Audio")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float musicVolume = 1f;
    public bool loopMusic;
    public bool restartIfAlreadyPlaying = true;
}

[CreateAssetMenu(fileName = "EnemyStateFeedback_", menuName = "Units/Enemy State Feedback Data")]
public class EnemyStateFeedbackData : ScriptableObject
{
    [SerializeField] private List<EnemyStateFeedbackEntry> entries = new List<EnemyStateFeedbackEntry>();

    public EnemyStateFeedbackEntry GetEntry(EnemyAIState state)
    {
        foreach (EnemyStateFeedbackEntry entry in entries)
        {
            if (entry != null && entry.state == state)
                return entry;
        }

        return null;
    }
}
