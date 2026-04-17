using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuAmbience : MonoBehaviour
{
    private AudioSource audioSource;
    public float targetVolume = 0.5f;
    public float duration = 2.0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0f; // Start with volume at 0
    }

    void Start()
    {
        StartCoroutine(FadeInAudio());
    }

    private System.Collections.IEnumerator FadeInAudio()
    {
        float currentTime = 0f;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0, targetVolume,currentTime / duration);
            yield return null;
        }

    }
}
