using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinStoreController : MonoBehaviour
{
    public PurchaseFufillment purchaseFulfillment;
    public Renderer characterRenderer;

    public List<SkinData> skins = new List<SkinData>();

    public TMP_Text skinNameText;
    public TMP_Text costText;
    public Button nextButton;
    public Button prevButton;
    public Button purchaseButton;
    public Button equipButton;
    public Button resetCurrentSkinButton;

    private int currentIndex = 0;
    private string currentEquippedSkinID;

    private void Start()
    {
        if (purchaseFulfillment == null)
        {
            purchaseFulfillment = FindAnyObjectByType<PurchaseFufillment>();
        }

        if (skins.Count == 0)
        {
            Debug.LogWarning("SkinStoreController has no skins configured.");
            UpdateButtonsForMissingSkins();
            return;
        }

        if (resetCurrentSkinButton != null)
        {
            resetCurrentSkinButton.onClick.RemoveAllListeners();
            resetCurrentSkinButton.onClick.AddListener(ResetCurrentSkinPurchase);
        }

        // Loads previously equipped skin for persistence across scenes
        currentEquippedSkinID = PlayerPrefs.GetString("EquippedSkin", skins[0].skinID);
        UpdateUI();
    }

    public void NextSkin()
    {
        if (skins.Count == 0)
            return;

        currentIndex++;
        if (currentIndex >= skins.Count) currentIndex = 0;
        UpdateUI();
    }

    public void ResetCurrentSkinPurchase()
    {
        if (skins.Count == 0)
            return;

        SkinData currentSkin = skins[currentIndex];
        if (currentSkin == null || currentSkin.isUnlockedByDefault)
        {
            Debug.Log("Default skins stay unlocked and cannot be reset.");
            UpdateUI();
            return;
        }

        PlayerPrefs.DeleteKey(GetSkinUnlockedKey(currentSkin));

        if (currentEquippedSkinID == currentSkin.skinID)
        {
            SkinData fallbackSkin = GetDefaultSkin();
            currentEquippedSkinID = fallbackSkin != null ? fallbackSkin.skinID : string.Empty;

            if (!string.IsNullOrEmpty(currentEquippedSkinID))
                PlayerPrefs.SetString("EquippedSkin", currentEquippedSkinID);
            else
                PlayerPrefs.DeleteKey("EquippedSkin");
        }

        PlayerPrefs.Save();
        Debug.Log($"Reset purchase state for skin: {currentSkin.skinName}");
        UpdateUI();
    }

    public void PrevSkin()
    {
        if (skins.Count == 0)
            return;

        currentIndex--;
        if (currentIndex < 0) currentIndex = skins.Count - 1;
        UpdateUI();
    }

    public void PurchaseSkin()
    {
        if (skins.Count == 0)
            return;

        SkinData currentSkin = skins[currentIndex];

        if (TrySpendGems(currentSkin.gemCost))
        {
            PlayerPrefs.SetInt(currentSkin.skinID + "_unlocked", 1);
            PlayerPrefs.Save();
            UpdateUI();
        }
    }

    public void EquipSkin()
    {
        if (skins.Count == 0)
            return;

        SkinData currentSkin = skins[currentIndex];

        currentEquippedSkinID = currentSkin.skinID;
        PlayerPrefs.SetString("EquippedSkin", currentEquippedSkinID);
        PlayerPrefs.Save();
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (skins.Count == 0)
        {
            UpdateButtonsForMissingSkins();
            return;
        }

        SkinData currentSkin = skins[currentIndex];
        bool isUnlocked = IsSkinUnlocked(currentSkin);

        if (skinNameText != null)
        {
            skinNameText.text = currentSkin.skinName;
        }

        if (costText != null)
        {
            costText.text = isUnlocked ? "Unlocked" : $"{currentSkin.gemCost} Gems";
        }

        // Previews the material on the target mesh
        if (characterRenderer != null && currentSkin.skinMaterial != null)
        {
            characterRenderer.material = currentSkin.skinMaterial;
        }

        // Toggles Purchase vs Equip buttons based on state
        if (purchaseButton != null)
        {
            purchaseButton.gameObject.SetActive(!isUnlocked);
            purchaseButton.interactable = GetAvailableGems() >= currentSkin.gemCost;
        }

        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(isUnlocked);
            equipButton.interactable = (currentEquippedSkinID != currentSkin.skinID);
        }

        if (resetCurrentSkinButton != null)
        {
            resetCurrentSkinButton.gameObject.SetActive(!currentSkin.isUnlockedByDefault);
            resetCurrentSkinButton.interactable = !currentSkin.isUnlockedByDefault && isUnlocked;
        }
    }

    private int GetAvailableGems()
    {
        return purchaseFulfillment != null
            ? purchaseFulfillment.availableGems
            : PurchaseFufillment.GetStoredAvailableGems();
    }

    private bool TrySpendGems(int gemCost)
    {
        if (purchaseFulfillment != null)
        {
            if (purchaseFulfillment.availableGems < gemCost)
            {
                return false;
            }

            purchaseFulfillment.SpendGems(gemCost);
            return true;
        }

        return PurchaseFufillment.TrySpendStoredGems(gemCost, out _);
    }

    private void UpdateButtonsForMissingSkins()
    {
        if (nextButton != null) nextButton.interactable = false;
        if (prevButton != null) prevButton.interactable = false;
        if (purchaseButton != null) purchaseButton.interactable = false;
        if (equipButton != null) equipButton.interactable = false;
        if (resetCurrentSkinButton != null) resetCurrentSkinButton.interactable = false;
    }

    private bool IsSkinUnlocked(SkinData skin)
    {
        return skin != null &&
               (skin.isUnlockedByDefault || PlayerPrefs.GetInt(GetSkinUnlockedKey(skin), 0) == 1);
    }

    private string GetSkinUnlockedKey(SkinData skin)
    {
        return skin != null ? skin.skinID + "_unlocked" : string.Empty;
    }

    private SkinData GetDefaultSkin()
    {
        for (int i = 0; i < skins.Count; i++)
        {
            if (skins[i] != null && skins[i].isUnlockedByDefault)
                return skins[i];
        }

        return skins.Count > 0 ? skins[0] : null;
    }
}
