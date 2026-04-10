using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using UnityEngine.UnityConsent;

public class AnalyticsManager : MonoBehaviour
{
    async void Start()
    {
        await UnityServices.InitializeAsync();
    }

    public void GrantConsent()
    {
        EndUserConsent.SetConsentState(new()
        {
            AdsIntent = ConsentStatus.Granted,
            AnalyticsIntent = ConsentStatus.Granted,
        });
        Debug.Log("Player Granted Consent");
    }

    public void DenyConsent()
    {
        EndUserConsent.SetConsentState(new()
        {
            AdsIntent = ConsentStatus.Denied,
            AnalyticsIntent = ConsentStatus.Denied,
        });
        Debug.Log("Player Denied Consent");
    }

    public void SendGemClickedEvent(string gemColor)
    {
        AnalyticsService.Instance.RecordEvent(new GemClickedEvent()
        {
            GemColor = gemColor
        });
    }

    public void SendBoughtGemsEvent(int gemsBought)
    {
        AnalyticsService.Instance.RecordEvent(new BoughtGemsEvent()
        {
            GemAmountPurchased = gemsBought
        });
    }

    public void SendAdViewedEvent(string adType)
    {
        AnalyticsService.Instance.RecordEvent(new GemAdViewedEvent()
        {
            GemAdTypeWatched = adType
        });
    }
}
