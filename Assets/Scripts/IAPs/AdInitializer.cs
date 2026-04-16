using UnityEngine;
using UnityEngine.Advertisements;


public class AdInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] private string androidGameID;
    [SerializeField] private string iOSGameID;
    [SerializeField] private bool testMode = true;

    private string gameID;

    private void InitializeAds()
    {
#if UNITY_IOS
    gameID = iOSGameID;
#elif UNITY_ANDROID
    gameID = androidGameID;
#elif UNITY_EDITOR
    gameID = androidGameID;
#else
        Debug.Log("Unity Ads demo disabled on this platform.");
        return;
#endif

        if (string.IsNullOrWhiteSpace(gameID))
        {
            Debug.LogWarning("Unity Ads Game ID is empty.");
            return;
        }

        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(gameID, testMode, this);
        }
        else if (!Advertisement.isSupported)
        {
            Debug.Log("Unity Ads is not supported on this platform.");
        }
    }

    
    void Awake()
    {
        InitializeAds();
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads Successfully initialized");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogWarning($"Unity Ads initialization failed: {error} - {message}");
    }
}
