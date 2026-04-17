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

    private int currentIndex = 0;
    private string currentEquippedSkinID;

    private void Start()
    {
        // Loads previously equipped skin for persistence across scenes
        currentEquippedSkinID = PlayerPrefs.GetString("EquippedSkin", skins[0].skinID);
        UpdateUI();
    }

    public void NextSkin()
    {
        currentIndex++;
        if (currentIndex >= skins.Count) currentIndex = 0;
        UpdateUI();
    }

    public void PrevSkin()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = skins.Count - 1;
        UpdateUI();
    }

    public void PurchaseSkin()
    {
        SkinData currentSkin = skins[currentIndex];

        if (purchaseFulfillment.availableGems >= currentSkin.gemCost)
        {
            purchaseFulfillment.SpendGems(currentSkin.gemCost);
            PlayerPrefs.SetInt(currentSkin.skinID + "_unlocked", 1);
            PlayerPrefs.Save();
            UpdateUI();
        }
    }

    public void EquipSkin()
    {
        SkinData currentSkin = skins[currentIndex];

        currentEquippedSkinID = currentSkin.skinID;
        PlayerPrefs.SetString("EquippedSkin", currentEquippedSkinID);
        PlayerPrefs.Save();
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        SkinData currentSkin = skins[currentIndex];
        bool isUnlocked = currentSkin.isUnlockedByDefault || PlayerPrefs.GetInt(currentSkin.skinID + "_unlocked", 0) == 1;

        skinNameText.text = currentSkin.skinName;
        costText.text = isUnlocked ? "Unlocked" : $"{currentSkin.gemCost} Gems";

        // Previews the material on the target mesh
        characterRenderer.material = currentSkin.skinMaterial;

        // Toggles Purchase vs Equip buttons based on state
        purchaseButton.gameObject.SetActive(!isUnlocked);
        purchaseButton.interactable = purchaseFulfillment.availableGems >= currentSkin.gemCost;

        equipButton.gameObject.SetActive(isUnlocked);
        equipButton.interactable = (currentEquippedSkinID != currentSkin.skinID);
    }
}