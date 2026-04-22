using System.Collections;
using UnityEngine;

public class MenuCameraController : MonoBehaviour
{
    public Transform mainCamera;
    public float transitionDuration = 1.5f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera Anchors")]
    public Transform mainMenuAnchor;
    public Transform storeAnchor;
    public Transform settingsAnchor;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject storePanel;
    public GameObject settingsPanel;

    private Coroutine currentTransition;

    private void Start()
    {
        // Enforces initial state
        SetInitialState(mainMenuAnchor, mainMenuPanel);
    }

    public void GoToMainMenu() => InitiateTransition(mainMenuAnchor, mainMenuPanel);
    public void GoToStore() => InitiateTransition(storeAnchor, storePanel);
    public void GoToSettings() => InitiateTransition(settingsAnchor, settingsPanel);

    private void InitiateTransition(Transform targetAnchor, GameObject targetPanel)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        // Hides all UI during camera movement
        mainMenuPanel.SetActive(false);
        storePanel.SetActive(false);
        settingsPanel.SetActive(false);

        currentTransition = StartCoroutine(TransitionRoutine(targetAnchor, targetPanel));
    }

    private IEnumerator TransitionRoutine(Transform targetAnchor, GameObject targetPanel)
    {
        Vector3 startPos = mainCamera.position;
        Quaternion startRot = mainCamera.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / transitionDuration);
            float curveT = transitionCurve.Evaluate(t);

            mainCamera.position = Vector3.Lerp(startPos, targetAnchor.position, curveT);
            mainCamera.rotation = Quaternion.Lerp(startRot, targetAnchor.rotation, curveT);

            yield return null;
        }

        mainCamera.position = targetAnchor.position;
        mainCamera.rotation = targetAnchor.rotation;

        // Displays target UI upon arrival
        targetPanel.SetActive(true);
    }

    private void SetInitialState(Transform targetAnchor, GameObject targetPanel)
    {
        mainCamera.position = targetAnchor.position;
        mainCamera.rotation = targetAnchor.rotation;

        mainMenuPanel.SetActive(false);
        storePanel.SetActive(false);
        settingsPanel.SetActive(false);

        targetPanel.SetActive(true);
    }

    //This is a test for a unity automatic refresh issue that has been occurring.
}