using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;


public class AdManager : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    private string adUnitAffix;

    private int currentGemReward = 0;
    private string adToShow = "";
    private string adTypeToShow = "";
    private bool adCompleted = false;

    private const string INTERSTITIAL_AD_PREFIX = "Interstitial";
    private const string REWARDED_AD_PREFIX = "Rewarded";
    private const string BANNER_AD_PREFIX = "Banner";

    [Header("Banner Ads")]
    [SerializeField] BannerPosition _bannerPosition = BannerPosition.BOTTOM_CENTER;
    private bool bannerAdShown = false;
    public bool cycleBannerAds = true;
    public float secondsUntilFirstBannerAd = 10f;
    public float secondsUntilHideBannerAd = 10f;
    public float secondsBetweenBannerAds = 50f;
    private bool adsAvailable;

    private void Awake()
    {
#if UNITY_IOS
        adUnitAffix = "_iOS";
        adsAvailable = true;
#elif UNITY_ANDROID
        adUnitAffix = "_Android";
        adsAvailable = true;
#elif UNITY_EDITOR
        adUnitAffix = "_Android";
        adsAvailable = true;
#else
        adsAvailable = false;
        Debug.Log("Unity Ads demo disabled on this platform.");
        enabled = false;
        return;
#endif
    }
    public void LoadAd(string adUnitPrefix)
    {
        if (!adsAvailable || !Advertisement.isSupported || !Advertisement.isInitialized)
        {
            Debug.LogWarning("Unity Ads is unavailable or not initialized.");
            return;
        }

        string adUnitID = adUnitPrefix + adUnitAffix;
        adToShow = adUnitID;
        Advertisement.Load(adUnitID, this);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"{placementId} loaded successfully");
        AdLoaded();
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"{placementId} failed to load: {error} - {message}");
    }
    public void ShowAd(string adUnitPrefix)
    {
        if (!adsAvailable || !Advertisement.isSupported || !Advertisement.isInitialized)
        {
            Debug.LogWarning("Unity Ads is unavailable or not initialized.");
            return;
        }

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
        AdCompleted(placementId);
    }

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

    public void AdLoaded()
    {
        ShowAd(adTypeToShow);
    }

    public void AdCompleted(string placementId)
    {
        string completedAdType = GetAdTypeFromPlacementId(placementId);

        if (completedAdType == REWARDED_AD_PREFIX)
        {
            if (adCompleted)
            {
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
            Debug.Log($"{placementId} is not a rewarded ad.");
        }
    }
    public void PrepareBannerAd()
    {
        if (!adsAvailable || !Advertisement.isSupported || !Advertisement.isInitialized)
        {
            Debug.LogWarning("Banner ad skipped because Unity Ads is unavailable.");
            return;
        }

        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        };

        adTypeToShow = BANNER_AD_PREFIX;

        Advertisement.Banner.Load(adUnitAffix, options);
    }

    void OnBannerLoaded()
    {
        Debug.Log("Banner loaded");
        ShowBannerAd();
    }

    void OnBannerError(string message)
    {
        Debug.Log($"Banner Load Error: {message}");
    }

    void ShowBannerAd()
    {
        BannerOptions options = new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback = OnBannerHidden,
            showCallback = OnBannerShown
        };

        Advertisement.Banner.Show(adUnitAffix, options);
    }
    void OnBannerClicked() { Debug.Log($"Banner Clicked"); }
    void OnBannerShown() { Debug.Log($"Banner Shown"); bannerAdShown = true; }
    void OnBannerHidden() { Debug.Log($"Banner Hidden"); }

    void HideBannerAd()
    {
        if (!adsAvailable || !Advertisement.isSupported || !Advertisement.isInitialized)
        {
            return;
        }

        Advertisement.Banner.Hide();
    }

    public void Start()
    {
        if (!adsAvailable)
        {
            return;
        }

        Advertisement.Banner.SetPosition(_bannerPosition);

        StartCoroutine(BannerAdsCycle());
    }

    private IEnumerator BannerAdsCycle()
    {
        if (!bannerAdShown) yield return new WaitForSeconds(secondsUntilFirstBannerAd);

        PrepareBannerAd();

        if (cycleBannerAds)
        {
            yield return new WaitForSeconds(secondsUntilHideBannerAd);
            HideBannerAd();

            yield return new WaitForSeconds(secondsBetweenBannerAds);
            StartCoroutine(BannerAdsCycle());
        }

        yield return null;
    }

    private string GetAdTypeFromPlacementId(string placementId)
    {
        if (placementId.StartsWith(REWARDED_AD_PREFIX))
        {
            return REWARDED_AD_PREFIX;
        }

        if (placementId.StartsWith(INTERSTITIAL_AD_PREFIX))
        {
            return INTERSTITIAL_AD_PREFIX;
        }

        if (placementId.StartsWith(BANNER_AD_PREFIX))
        {
            return BANNER_AD_PREFIX;
        }

        return placementId;
    }

}
