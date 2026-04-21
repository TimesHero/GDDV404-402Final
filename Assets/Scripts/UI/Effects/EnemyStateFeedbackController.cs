using System.Collections;
using UnityEngine;

public class EnemyStateFeedbackController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private EnemyStateFeedbackData feedbackData;

    [Header("Audio")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private bool useSharedMusicSource = true;

    private static AudioSource sharedMusicSource;

    public void PlayStateEntered(EnemyAIState state, Transform sourceTransform)
    {
        if (feedbackData == null)
            return;

        EnemyStateFeedbackEntry entry = feedbackData.GetEntry(state);
        if (entry == null)
            return;

        PlayVisual(entry, sourceTransform);
        PlayMusic(entry);
    }

    private void PlayVisual(EnemyStateFeedbackEntry entry, Transform sourceTransform)
    {
        if (entry == null || entry.effectPrefab == null || sourceTransform == null)
            return;

        Quaternion spawnRotation = Quaternion.Euler(entry.rotationEuler);

        GameObject effectInstance;
        if (entry.attachToSource)
        {
            effectInstance = Instantiate(entry.effectPrefab, sourceTransform);
            effectInstance.transform.localPosition = entry.positionOffset;
            effectInstance.transform.localRotation = spawnRotation;
        }
        else
        {
            Vector3 spawnPosition = sourceTransform.position + entry.positionOffset;
            effectInstance = Instantiate(entry.effectPrefab, spawnPosition, spawnRotation);
        }

        SanitizeVisualOnlyPrefab(effectInstance);
        effectInstance.transform.localScale = entry.effectScale;

        StartCoroutine(LiftAndDestroyRoutine(effectInstance, entry, entry.attachToSource));
    }

    private IEnumerator LiftAndDestroyRoutine(GameObject effectInstance, EnemyStateFeedbackEntry entry, bool useLocalPosition)
    {
        if (effectInstance == null || entry == null)
            yield break;

        Vector3 startPosition = useLocalPosition ? effectInstance.transform.localPosition : effectInstance.transform.position;
        Vector3 endPosition = startPosition + Vector3.up * entry.riseAmount;

        if (entry.liftDuration > 0f && entry.riseAmount != 0f)
        {
            float elapsed = 0f;

            while (elapsed < entry.liftDuration)
            {
                if (effectInstance == null)
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / entry.liftDuration);

                if (useLocalPosition)
                    effectInstance.transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
                else
                    effectInstance.transform.position = Vector3.Lerp(startPosition, endPosition, t);

                yield return null;
            }
        }

        if (effectInstance == null)
            yield break;

        if (useLocalPosition)
            effectInstance.transform.localPosition = endPosition;
        else
            effectInstance.transform.position = endPosition;

        float remainingTime = Mathf.Max(0f, entry.duration - entry.liftDuration);
        yield return new WaitForSeconds(remainingTime);

        if (effectInstance != null)
            Destroy(effectInstance);
    }

    private void SanitizeVisualOnlyPrefab(GameObject effectInstance)
    {
        if (effectInstance == null)
            return;

        Camera[] cameras = effectInstance.GetComponentsInChildren<Camera>(true);
        foreach (Camera effectCamera in cameras)
            effectCamera.enabled = false;

        AudioListener[] listeners = effectInstance.GetComponentsInChildren<AudioListener>(true);
        foreach (AudioListener listener in listeners)
            listener.enabled = false;

        AudioSource[] audioSources = effectInstance.GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource audioSource in audioSources)
            audioSource.enabled = false;

        Animator[] animators = effectInstance.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
            animator.enabled = false;

        Animation[] animations = effectInstance.GetComponentsInChildren<Animation>(true);
        foreach (Animation animation in animations)
            animation.enabled = false;
    }

    private void PlayMusic(EnemyStateFeedbackEntry entry)
    {
        if (entry == null || entry.musicClip == null)
            return;

        AudioSource source = ResolveMusicSource();
        if (source == null)
            return;

        source.volume = entry.musicVolume;
        source.loop = entry.loopMusic;

        if (entry.loopMusic)
        {
            if (!entry.restartIfAlreadyPlaying && source.isPlaying && source.clip == entry.musicClip)
                return;

            source.clip = entry.musicClip;
            source.Play();
            return;
        }

        if (!entry.restartIfAlreadyPlaying && source.isPlaying)
            return;

        source.PlayOneShot(entry.musicClip, entry.musicVolume);
    }

    private AudioSource ResolveMusicSource()
    {
        if (!useSharedMusicSource)
        {
            if (musicAudioSource == null)
                musicAudioSource = GetComponent<AudioSource>();

            if (musicAudioSource == null)
                musicAudioSource = gameObject.AddComponent<AudioSource>();

            return musicAudioSource;
        }

        if (sharedMusicSource != null)
            return sharedMusicSource;

        GameObject sourceObject = new GameObject("EnemyStateFeedbackAudio");
        sharedMusicSource = sourceObject.AddComponent<AudioSource>();
        sharedMusicSource.playOnAwake = false;
        return sharedMusicSource;
    }
}
