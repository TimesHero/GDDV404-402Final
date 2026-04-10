using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeButtonAudioController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target")]
    [SerializeField] private Button targetButton;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioSource hoverAudioSource;
    [SerializeField] private AudioSource transitionAudioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    [Header("Reveal")]
    [SerializeField] private CanvasGroup revealCanvasGroup;
    [SerializeField] private float revealVisibleDuration = 1f;
    [SerializeField] private float revealFadeDuration = 0.5f;

    [Header("Crossfade")]
    [SerializeField] private float musicFadeDuration = 0.5f;
    [SerializeField] private float autoClickLeadTime = 0.15f;

    private Coroutine hoverCompletionCoroutine;
    private Coroutine clickSequenceCoroutine;
    private Coroutine revealCoroutine;
    private bool suppressNextClickSequence;
    private bool autoTriggeredPurchase;

    private bool isHovering;
    private bool hoverFinished;
    private float pausedHoverTime;
    private float defaultBackgroundVolume;
    private float defaultTransitionVolume;

    private void Awake()
    {
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }

        if (targetButton != null)
        {
            targetButton.onClick.AddListener(HandleButtonClicked);
        }

        if (backgroundMusicSource != null)
        {
            defaultBackgroundVolume = backgroundMusicSource.volume;
            backgroundMusicSource.loop = true;
        }

        if (transitionAudioSource != null)
        {
            defaultTransitionVolume = transitionAudioSource.volume;
        }

        if (revealCanvasGroup != null)
        {
            revealCanvasGroup.alpha = 0f;
            revealCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanPlayHover())
        {
            return;
        }

        isHovering = true;
        PlayHoverClip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanPlayHover())
        {
            return;
        }

        isHovering = false;
        PauseHoverClip();
    }

    private bool CanPlayHover()
    {
        return targetButton != null
            && targetButton.interactable
            && hoverAudioSource != null
            && hoverClip != null
            && !hoverFinished;
    }

    private void PlayHoverClip()
    {
        if (hoverAudioSource.clip != hoverClip)
        {
            hoverAudioSource.clip = hoverClip;
        }

        hoverAudioSource.loop = false;
        hoverAudioSource.time = Mathf.Clamp(pausedHoverTime, 0f, hoverClip.length);
        hoverAudioSource.Play();

        if (hoverCompletionCoroutine != null)
        {
            StopCoroutine(hoverCompletionCoroutine);
        }

        float remainingTime = hoverClip.length - pausedHoverTime;
        hoverCompletionCoroutine = StartCoroutine(WaitForHoverCompletion(remainingTime));
    }

    private void PauseHoverClip()
    {
        if (hoverAudioSource == null || hoverClip == null)
        {
            return;
        }

        pausedHoverTime = hoverAudioSource.isPlaying ? hoverAudioSource.time : pausedHoverTime;
        hoverAudioSource.Stop();

        if (hoverCompletionCoroutine != null)
        {
            StopCoroutine(hoverCompletionCoroutine);
            hoverCompletionCoroutine = null;
        }
    }

    private IEnumerator WaitForHoverCompletion(float remainingTime)
    {
        yield return new WaitForSeconds(remainingTime);

        hoverCompletionCoroutine = null;

        if (!isHovering || hoverFinished || targetButton == null || !targetButton.interactable)
        {
            yield break;
        }

        hoverFinished = true;
        pausedHoverTime = 0f;
        autoTriggeredPurchase = true;

        if (clickSequenceCoroutine != null)
        {
            StopCoroutine(clickSequenceCoroutine);
        }

        clickSequenceCoroutine = StartCoroutine(PlayAutoTriggeredSequence());
    }

    private void HandleButtonClicked()
    {
        if (suppressNextClickSequence)
        {
            suppressNextClickSequence = false;
            return;
        }

        if (autoTriggeredPurchase)
        {
            autoTriggeredPurchase = false;
            return;
        }

        isHovering = false;
        hoverFinished = true;
        pausedHoverTime = 0f;

        if (hoverAudioSource != null)
        {
            hoverAudioSource.Stop();
        }

        if (hoverCompletionCoroutine != null)
        {
            StopCoroutine(hoverCompletionCoroutine);
            hoverCompletionCoroutine = null;
        }

        if (clickSequenceCoroutine != null)
        {
            StopCoroutine(clickSequenceCoroutine);
        }

        clickSequenceCoroutine = StartCoroutine(PlayClickSequence());

        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }

        revealCoroutine = StartCoroutine(ShowReveal());
    }

    private IEnumerator PlayAutoTriggeredSequence()
    {
        isHovering = false;

        if (hoverCompletionCoroutine != null)
        {
            StopCoroutine(hoverCompletionCoroutine);
            hoverCompletionCoroutine = null;
        }

        if (transitionAudioSource != null && clickClip != null)
        {
            transitionAudioSource.Stop();
            transitionAudioSource.clip = clickClip;
            transitionAudioSource.loop = false;
            transitionAudioSource.volume = defaultTransitionVolume;
            transitionAudioSource.Play();
        }

        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }

        revealCoroutine = StartCoroutine(ShowReveal());

        suppressNextClickSequence = true;
        targetButton.onClick.Invoke();
        autoTriggeredPurchase = false;
        clickSequenceCoroutine = null;
        yield break;
    }

    private IEnumerator PlayClickSequence()
    {
        if (transitionAudioSource == null || clickClip == null)
        {
            clickSequenceCoroutine = null;
            yield break;
        }

        float fadeDuration = Mathf.Max(0.01f, musicFadeDuration);

        if (backgroundMusicSource != null)
        {
            yield return FadeAudio(backgroundMusicSource, backgroundMusicSource.volume, 0f, fadeDuration);
        }

        transitionAudioSource.Stop();
        transitionAudioSource.clip = clickClip;
        transitionAudioSource.loop = false;
        transitionAudioSource.volume = defaultTransitionVolume;
        transitionAudioSource.Play();

        float waitBeforeCrossfadeBack = Mathf.Max(0f, clickClip.length - fadeDuration);
        yield return new WaitForSeconds(waitBeforeCrossfadeBack);

        Coroutine fadeOutTransition = StartCoroutine(FadeAudio(transitionAudioSource, transitionAudioSource.volume, 0f, fadeDuration));
        Coroutine fadeInBackground = null;

        if (backgroundMusicSource != null)
        {
            if (!backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Play();
            }

            fadeInBackground = StartCoroutine(FadeAudio(backgroundMusicSource, backgroundMusicSource.volume, defaultBackgroundVolume, fadeDuration));
        }

        yield return fadeOutTransition;

        if (fadeInBackground != null)
        {
            yield return fadeInBackground;
        }

        transitionAudioSource.Stop();
        transitionAudioSource.volume = defaultTransitionVolume;
        clickSequenceCoroutine = null;
    }

    private IEnumerator PlayClickSequenceAndTriggerPurchase()
    {
        if (transitionAudioSource == null || clickClip == null)
        {
            suppressNextClickSequence = true;
            targetButton.onClick.Invoke();
            clickSequenceCoroutine = null;
            yield break;
        }

        float fadeDuration = Mathf.Max(0.01f, musicFadeDuration);

        if (backgroundMusicSource != null)
        {
            yield return FadeAudio(backgroundMusicSource, backgroundMusicSource.volume, 0f, fadeDuration);
        }

        transitionAudioSource.Stop();
        transitionAudioSource.clip = clickClip;
        transitionAudioSource.loop = false;
        transitionAudioSource.volume = defaultTransitionVolume;
        transitionAudioSource.Play();

        float triggerDelay = Mathf.Max(0f, clickClip.length - autoClickLeadTime);
        yield return new WaitForSeconds(triggerDelay);

        suppressNextClickSequence = true;
        targetButton.onClick.Invoke();

        float remainingClipTime = Mathf.Min(autoClickLeadTime, clickClip.length);
        if (remainingClipTime > 0f)
        {
            yield return new WaitForSeconds(remainingClipTime);
        }

        Coroutine fadeOutTransition = StartCoroutine(FadeAudio(transitionAudioSource, transitionAudioSource.volume, 0f, fadeDuration));
        Coroutine fadeInBackground = null;

        if (backgroundMusicSource != null)
        {
            if (!backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Play();
            }

            fadeInBackground = StartCoroutine(FadeAudio(backgroundMusicSource, backgroundMusicSource.volume, defaultBackgroundVolume, fadeDuration));
        }

        yield return fadeOutTransition;

        if (fadeInBackground != null)
        {
            yield return fadeInBackground;
        }

        transitionAudioSource.Stop();
        transitionAudioSource.volume = defaultTransitionVolume;
        clickSequenceCoroutine = null;
    }

    private IEnumerator ShowReveal()
    {
        if (revealCanvasGroup == null)
        {
            yield break;
        }

        revealCanvasGroup.gameObject.SetActive(true);
        revealCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(revealVisibleDuration);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, revealFadeDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            revealCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        revealCanvasGroup.alpha = 0f;
        revealCanvasGroup.gameObject.SetActive(false);
        revealCoroutine = null;
    }

    private static IEnumerator FadeAudio(AudioSource source, float from, float to, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        float elapsed = 0f;
        source.volume = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        source.volume = to;
    }
}
