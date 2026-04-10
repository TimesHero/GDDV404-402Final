using UnityEngine;
using UnityEngine.SceneManagement;

public class ConsentGateController : MonoBehaviour
{
    private const string ConsentStateKey = "ConsentGateController.ConsentState";
    private const int ConsentUnset = -1;
    private const int ConsentGranted = 1;

    [SerializeField] private GameObject consentPanel;
    [SerializeField] private GameObject[] gatedObjects;
    [SerializeField] private string deniedSceneName = string.Empty;
    
    private void Start()
    {
        ApplySavedConsentState();
    }

    public void GrantConsent()
    {
        SaveConsentState(ConsentGranted);
        ShowStoreContent();
    }

    public void DenyConsent()
    {
        HideStoreContent();

        if (!string.IsNullOrWhiteSpace(deniedSceneName))
        {
            SceneManager.LoadScene(deniedSceneName);
        }
    }

    public void ClearSavedConsent()
    {
        PlayerPrefs.DeleteKey(ConsentStateKey);
        PlayerPrefs.Save();
        ShowConsentPanel();
    }

    private void ApplySavedConsentState()
    {
        int savedState = PlayerPrefs.GetInt(ConsentStateKey, ConsentUnset);
        if (savedState == ConsentGranted)
        {
            ShowStoreContent();
            return;
        }

        ShowConsentPanel();
    }

    private void SaveConsentState(int consentState)
    {
        PlayerPrefs.SetInt(ConsentStateKey, consentState);
        PlayerPrefs.Save();
    }

    private void ShowConsentPanel()
    {
        SetActive(consentPanel, true);
        SetGatedObjectsActive(false);
    }

    private void ShowStoreContent()
    {
        SetActive(consentPanel, false);
        SetGatedObjectsActive(true);
    }

    private void HideStoreContent()
    {
        SetActive(consentPanel, true);
        SetGatedObjectsActive(false);
    }

    private void SetGatedObjectsActive(bool isActive)
    {
        foreach (GameObject gatedObject in gatedObjects)
        {
            SetActive(gatedObject, isActive);
        }
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
