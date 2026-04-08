using JetBrains.Annotations;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;


public class AdManager : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    //Script handles showing ads to the player.
    //An ad must be loaded before it can be shown

    private string adUnitAffix;

    private int currentGemReward = 0;
    private string adToShow = "";
    private string adTypeToShow = "";
    private bool adCompleted = false;

    private const string INTERSTITIAL_AD_PREFIX = "Interstitial";
    private const string REWARDED_AD_PREFIX = "Rewarded";
    private const string BANNER_AD_PREFIX = "Banner";

    public AnalyticsManager analyticsManager;

    [Header("Banner Ads")]
    [SerializeField] BannerPosition _bannerPosition = BannerPosition.BOTTOM_CENTER;
    private bool bannerAdShown = false;
    public bool cycleBannerAds = true;
    public float secondsUntilFirstBannerAd = 10f;
    public float secondsUntilHideBannerAd = 10f;
    public float secondsBetweenBannerAds = 50f;

    

    //Pre Processor Instructions, in order to ensure the correct gameID is being used.
    //These are macros, telling the compiler which instruction to use.
    //Pre Processor saves time and space in the compiled build of our game.
    private void Awake()
    {
#if UNITY_IOS
        adUnitAffix = "_iOS";
#elif UNITY_ANDROID
        adUnitAffix = "_Android";
#elif UNITY_EDITOR
        adUnitAffix = "_Android";
#endif
        //ads are not loaded yet!!
    }

    /// <summary>
    /// Loading Ads
    /// </summary>
    public void LoadAd(string adUnitPrefix)
    {
        string adUnitID = adUnitPrefix + adUnitAffix;
        adToShow = adUnitID;
        Advertisement.Load(adUnitID, this);
    }

    //Interface from IUnityAdsLoadListener
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"{placementId} loaded successfully");
        AdLoaded();
    }

    //Interface from IUnityAdsLoadListener
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"{placementId} failed to load: {error} - {message}");
    }

    /// <summary>
    /// Showing Ads
    /// </summary>
    public void ShowAd(string adUnitPrefix)
    {
        string adUnitID = adUnitPrefix + adUnitAffix;
        Advertisement.Show(adUnitID, this);
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning($"{placementId} failed to show: {error} - {message}");
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log($"{placementId} started.");
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log($"{placementId} clicked!");
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        adCompleted = showCompletionState == UnityAdsShowCompletionState.COMPLETED;
        Debug.Log($"{placementId} completed. - {showCompletionState}");
        AdCompleted();

        //Sends an event to the unity dashboard
        analyticsManager.SendAdViewedEvent(adTypeToShow);
    }

    /////
    //Reward and Interstitial Ads
    /////
    ///

    //1. Load Reward Ad.
    public void PrepareRewardAd(int gemReward)
    {
        adTypeToShow = REWARDED_AD_PREFIX;
        LoadAd(REWARDED_AD_PREFIX);
        currentGemReward = gemReward;
    }

    public void PrepareInterstitialAd()
    {
        adTypeToShow = INTERSTITIAL_AD_PREFIX;
        LoadAd(INTERSTITIAL_AD_PREFIX);
        currentGemReward = 0;
    }

    //2. After Reward Ad is loaded, display reward ad.
    public void AdLoaded()
    {
        //After an ad is loaded, show it.
        ShowAd(adTypeToShow);

    }

    //3. Handle collection of gems for successful ad.
    public void AdCompleted()
    {
        //check if reward should be rewarded
        if (adTypeToShow == REWARDED_AD_PREFIX)
        {
            if (adCompleted)
            {
                //grant gems
                PurchaseFufillment fufillment = FindAnyObjectByType<PurchaseFufillment>();
                fufillment.GrantGems(currentGemReward);
            }
            else
            {
                Debug.LogWarning($"{adToShow} unsucessfully completed?");
            }
        }
        else
        {
            Debug.Log($"{adToShow} is not a rewarded ad.");
        }
    }
    ////////////////
    ///

    ///
    /// Banner Ads
    /// 

    //1. Load a Banner Ad.
    public void PrepareBannerAd()
    {
        // Set up options to notify the SDK of load events:
        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        };

        // mainly for recording what ad was watched.
        adTypeToShow = BANNER_AD_PREFIX;

        // Load the Ad Unit with banner content:
        Advertisement.Banner.Load(adUnitAffix, options);
    }

    //Occurs when the loadCallback event triggers:
    void OnBannerLoaded()
    {
        Debug.Log("Banner loaded");

        //now show da thing
        ShowBannerAd();

    }

    //Occurs when when the load errorCallback event triggers:
    void OnBannerError(string message)
    {
        Debug.Log($"Banner Load Error: {message}");
        // Optionally execute additional code, such as attempting to load another ad.
    }

    //Shows the loaded banner ad
    void ShowBannerAd()
    {
        //Set up options to notify the script(SDK) of show events
        BannerOptions options = new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback = OnBannerHidden,
            showCallback = OnBannerShown
        };

        //Show the loaded Banner Ad Unit
        Advertisement.Banner.Show(adUnitAffix, options);
    }
    void OnBannerClicked() { Debug.Log($"Banner Clicked"); }
    void OnBannerShown() { Debug.Log($"Banner Shown"); bannerAdShown = true; }
    void OnBannerHidden() { Debug.Log($"Banner Hidden"); }

    //2. Hide a Banner Ad.
    void HideBannerAd()
    {
        Advertisement.Banner.Hide();
    }

    //3. Set up a global timer to load and unload Banner Ads.
    public void Start()
    {
        // Set the banner position:
        Advertisement.Banner.SetPosition(_bannerPosition);

        StartCoroutine(BannerAdsCycle());

        //also sets the analytics manager
        if (analyticsManager == null) { analyticsManager = FindAnyObjectByType<AnalyticsManager>(); }
    }

    private IEnumerator BannerAdsCycle()
    {
        //initial wait
        if (!bannerAdShown) yield return new WaitForSeconds(secondsUntilFirstBannerAd);

        //show banner ad
        PrepareBannerAd();

        //wait after showing ad
        if (cycleBannerAds)
        {
            //hiding the ad
            yield return new WaitForSeconds(secondsUntilHideBannerAd);
            HideBannerAd();

            //wait for new ad
            yield return new WaitForSeconds(secondsBetweenBannerAds);
            StartCoroutine(BannerAdsCycle());
        }

        yield return null;
    }


}
