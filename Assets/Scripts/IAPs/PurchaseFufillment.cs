using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;
using TMPro;
public class PurchaseFufillment : MonoBehaviour
{
    public const string AvailableGemsPlayerPrefsKey = "AvailableGems";
    public const string RedGemsPlayerPrefsKey = "RedGemsUnlocked";

    public int availableGems;
    public int spentGems;
    public bool redGems = false;

    private const string GEMS_1 = "Buy1Gem";
    private const string GEMS_10 = "Buy10Gems";
    private const string RED_GEMS = "UpgradeToRedGems";

    public int gemsSpentUntilAd = 50;

    public AnalyticsManager analyticsManager;

    public TMP_Text gemsCountText;
    public Image gemsImage;

    public Sprite greenGemSprite;
    public Sprite redGemSprite;

    public Button UpgradeGemsButton;

    public Button rewardAdButton;

    private void Awake()
    {
        availableGems = GetStoredAvailableGems(availableGems);
        redGems = GetStoredRedGems(redGems);
    }

    public void Start()
    {
        if (analyticsManager == null) { analyticsManager = FindAnyObjectByType<AnalyticsManager>(); }

        UpdateGemsDisplay();
    }

    public void OnConfirmedOrder(ConfirmedOrder confirmedOrder)
    {
        var purchaseProductInfo = confirmedOrder.Info.PurchasedProductInfo;

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
        availableGems = AddStoredGems(gemAmount, availableGems);
        Debug.Log($"You purchased {gemAmount} gems!");
        Debug.Log("Total Gems: " + availableGems);
        UpdateGemsDisplay();
        analyticsManager?.SendBoughtGemsEvent(gemAmount);
    }

    public void UpgradeGems()
    {
        redGems = true;
        PlayerPrefs.SetInt(RedGemsPlayerPrefsKey, 1);
        PlayerPrefs.Save();
        Debug.Log($"You purchased Red Gems!");
        Debug.Log($"Thank you for upgrading!");
        UpdateGemsDisplay();
    }

    public void UpdateGemsDisplay()
    {
        if (gemsCountText != null)
        {
            gemsCountText.text = $"{availableGems}";
        }

        if (redGems)
        {
            if (gemsImage != null && redGemSprite != null)
            {
                gemsImage.sprite = redGemSprite;
            }

            if (UpgradeGemsButton != null)
            {
                UpgradeGemsButton.interactable = false;
            }
        }
        else
        {
            if (gemsImage != null && greenGemSprite != null)
            {
                gemsImage.sprite = greenGemSprite;
            }

            if (UpgradeGemsButton != null)
            {
                UpgradeGemsButton.interactable = true;
            }
        }
    }

    public void SpendGems(int gemsToSpend)
    {
        if (TrySpendStoredGems(gemsToSpend, out int updatedGems))
        {
            availableGems = updatedGems;
            spentGems += gemsToSpend;
            UpdateGemsDisplay();
        }
    }

    private void Update()
    {
        RewardAdButtonDisplay();

        InterstitialAdDisplay();
    }

    void RewardAdButtonDisplay()
    {
        if (rewardAdButton == null)
        {
            return;
        }

        rewardAdButton.interactable = availableGems <= 0;
    }

    void InterstitialAdDisplay()
    {
        if (spentGems >= gemsSpentUntilAd)
        {
            spentGems = 0;

            AdManager AM = FindAnyObjectByType<AdManager>();
            if (AM != null) { AM.PrepareInterstitialAd(); }
        }
    }

    public static int GetStoredAvailableGems(int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(AvailableGemsPlayerPrefsKey, Mathf.Max(0, defaultValue));
    }

    public static int AddStoredGems(int gemAmount, int defaultValue = 0)
    {
        int updatedGems = GetStoredAvailableGems(defaultValue) + Mathf.Max(0, gemAmount);
        PlayerPrefs.SetInt(AvailableGemsPlayerPrefsKey, updatedGems);
        PlayerPrefs.Save();
        return updatedGems;
    }

    public static bool TrySpendStoredGems(int gemsToSpend, out int updatedGems)
    {
        int cost = Mathf.Max(0, gemsToSpend);
        int currentGems = GetStoredAvailableGems();

        if (currentGems < cost)
        {
            updatedGems = currentGems;
            return false;
        }

        updatedGems = currentGems - cost;
        PlayerPrefs.SetInt(AvailableGemsPlayerPrefsKey, updatedGems);
        PlayerPrefs.Save();
        return true;
    }

    public static bool GetStoredRedGems(bool defaultValue = false)
    {
        return PlayerPrefs.GetInt(RedGemsPlayerPrefsKey, defaultValue ? 1 : 0) == 1;
    }
}
