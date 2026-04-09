using UnityEngine;
using UnityEngine.Advertisements;


public class AdInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    //This script checks what GameID we're using
    //initializes the Adverisement class
    //and listens to and handles events that happen from Advertisement upon initialization.


    [SerializeField] private string androidGameID;
    [SerializeField] private string iOSGameID;
    [SerializeField] private bool testMode = true;

    private string gameID;

    //Pre Processor Instructions, in order to ensure the correct gameID is being used.
    //These are macros, telling the compiler which instruction to use.
    //Pre Processor saves time and space in the compiled build of our game.
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

        //if Advertisement class isn't initialized, and is supported, initializes it under our current settings.
        //"this" means that this class will be listening to the events shot out by Advertisement.
        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(gameID, testMode, this);
        }
        else if (!Advertisement.isSupported)
        {
            Debug.Log("Unity Ads is not supported on this platform.");
        }
    }


    // Awake happens before Start
    void Awake()
    {
        InitializeAds();
    }

    //Interface from IUnityAdsInitializationListener
    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads Successfully initialized");
    }

    //Interface from IUnityAdsInitializationListener
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogWarning($"Unity Ads initialization failed: {error} - {message}");
    }
}
