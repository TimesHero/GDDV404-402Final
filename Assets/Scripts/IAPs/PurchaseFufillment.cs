using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;
public class PurchaseFufillment : MonoBehaviour
{
    public int availableGems;
    public int spentGems; //updates as gems are spent
    public bool redGems = false;

    private const string GEMS_1 = "Buy1Gem"; //this ideally would be loaded from a .json file for easy editing in the future.
    private const string GEMS_10 = "Buy10Gems";
    private const string RED_GEMS = "UpgradeToRedGems";

    public int gemsSpentUntilAd = 50;

    public AnalyticsManager analyticsManager;

    public Text gemsCountText;
    public Image gemsImage;

    //Irrelevant atm
    public Sprite greenGemSprite;
    public Sprite redGemSprite;

    //to be disabled after purchasing red gems
    public Button UpgradeGemsButton;

    //to be displayed when gems = 0
    public GameObject rewardAdButton;

    public void Start()
    {
        UpdateGemsDisplay();

        if (analyticsManager == null) { analyticsManager = FindAnyObjectByType<AnalyticsManager>(); }
    }

    public void OnConfirmedOrder(ConfirmedOrder confirmedOrder)
    {
        var purchaseProductInfo = confirmedOrder.Info.PurchasedProductInfo;

        //this is new, study this!!
        foreach (IPurchasedProductInfo info in purchaseProductInfo)
        {
            switch (info.productId)
            {
                case GEMS_1:
                    GrantGems(1);
                    break;
                case GEMS_10:
                    GrantGems(10);
                    break;
                case RED_GEMS:
                    UpgradeGems();
                    break;
            }
        }

    }

    


    public void OnFailedOrder(FailedOrder failedOrder)
    {
        var purchaseProductInfo = failedOrder.Info.PurchasedProductInfo;
        string items = string.Empty;

        foreach (IPurchasedProductInfo info in purchaseProductInfo)
        {
            items += info.productId;
        }

        Debug.Log($"Failed to purchase the following items: {items}");
        Debug.Log($"Reason: '{failedOrder.FailureReason}', " +
            $"Details: '{failedOrder.Details}'");
    }


    public void GrantGems(int gemAmount)
    {
        availableGems += gemAmount;
        Debug.Log($"You purchased {gemAmount} gems!");
        Debug.Log("Total Gems: " + availableGems);
        UpdateGemsDisplay();

        //Sends an event to the unity dashboard
        analyticsManager.SendBoughtGemsEvent(gemAmount);
    }

    public void UpgradeGems()
    {
        //Script for upgrading gems goes here
        redGems = true;
        Debug.Log($"You purchased Red Gems!");
        Debug.Log($"Thank you for upgrading!");
        UpdateGemsDisplay();
    }

    public void UpdateGemsDisplay()
    {
        //Update Gem Count.
        gemsCountText.text = $"{availableGems}";

        //Check if Gems are red.
        if (redGems)
        {
            gemsImage.sprite = redGemSprite;
            UpgradeGemsButton.interactable = false;
        }
        else
        {
            gemsImage.sprite = greenGemSprite;
            UpgradeGemsButton.interactable = true;
        }
    }

    public void SpendGems(int gemsToSpend)
    {
        if (availableGems < gemsToSpend) { return; }
        else
        {
            availableGems -= gemsToSpend;
            spentGems += gemsToSpend;
        }
        UpdateGemsDisplay();
    }

    private void Update()
    {
        RewardAdButtonDisplay();

        InterstitialAdDisplay();
    }

    void RewardAdButtonDisplay()
    {
        if (availableGems > 0) { rewardAdButton.SetActive(false); }
        else { rewardAdButton.SetActive(true); }
    }

    void InterstitialAdDisplay()
    {
        if (spentGems >= gemsSpentUntilAd)
        {
            spentGems = 0;

            //load and show an interstitial ad
            AdManager AM = FindAnyObjectByType<AdManager>();
            if (AM != null) { AM.PrepareInterstitialAd(); }
        }
    }
}
