using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;
using TMPro;
public class PurchaseFufillment : MonoBehaviour
{
    public int availableGems;
    public int spentGems;
    public bool redGems = false;

    private const string GEMS_1 = "Buy1Gem";
    private const string GEMS_10 = "Buy10Gems";
    private const string RED_GEMS = "UpgradeToRedGems";

    public TMP_Text gemsCountText;
    public Image gemsImage;

    public Sprite greenGemSprite;
    public Sprite redGemSprite;

    public Button UpgradeGemsButton;

    public void Start()
    {
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
        availableGems += gemAmount;
        Debug.Log($"You purchased {gemAmount} gems!");
        Debug.Log("Total Gems: " + availableGems);
        UpdateGemsDisplay();
    }

    public void UpgradeGems()
    {
        redGems = true;
        Debug.Log($"You purchased Red Gems!");
        Debug.Log($"Thank you for upgrading!");
        UpdateGemsDisplay();
    }

    public void UpdateGemsDisplay()
    {
        gemsCountText.text = $"{availableGems}";

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
        }
        UpdateGemsDisplay();
    }
}
