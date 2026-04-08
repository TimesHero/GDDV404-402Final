using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;
using UnityEngine.UnityConsent;

public class AnalyticsManager : MonoBehaviour
{
    // async functions are started on a seperate thread. Important, but not important enough to wait for.
    async void Start()
    {
        await UnityServices.InitializeAsync();
    }

    //runs when user gives concent to use analytics and ads
    public void GrantConsent()
    {
        EndUserConsent.SetConsentState(new()
        {
            AdsIntent = ConsentStatus.Granted,
            AnalyticsIntent = ConsentStatus.Granted,
        });
        Debug.Log("Player Granted Consent");
    }

    //vice versa
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
        //uses the GemClickedEvent class
        AnalyticsService.Instance.RecordEvent(new GemClickedEvent()
        {
            GemColor = gemColor
        }); //this setup is called a closer in c#, this ends the operation, closing it.
    }
    
    ///
    ///Assignment 3 Events
    ///

    //activated when the player successfully buys gems
    public void SendBoughtGemsEvent(int gemsBought)
    {
        AnalyticsService.Instance.RecordEvent(new BoughtGemsEvent()
        {
            GemAmountPurchased = gemsBought
        }); //this setup is called a closer in c#, this ends the operation, closing it.
    }

    //activated when player watches an ad
    public void SendAdViewedEvent(string adType)
    {
        AnalyticsService.Instance.RecordEvent(new GemAdViewedEvent()
        {
            GemAdTypeWatched = adType
        }); //this setup is called a closer in c#, this ends the operation, closing it.
    }
}
