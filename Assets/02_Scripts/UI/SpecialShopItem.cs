using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SpecialShopItem : MonoBehaviour
{
    public SpecialAttackSO specialToSell;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI statusText; 
    public Button actionButton;

    private SpecialAttackSO _special;
    private ShopUI _shop;

    private void OnEnable() 
    { 
        UpdateUI(); 
    }

    public void Setup(SpecialAttackSO special, ShopUI shop)
    {
        _special = special;
        _shop = shop;
        
        if (nameText != null) nameText.text = _special.attackName;
        
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnButtonClicked);
        
        UpdateUI();
    }

    public void UpdateUI() 
    {
        if (PlayerStats.Instance == null || specialToSell == null) return;

        if (nameText != null) nameText.text = specialToSell.attackName;
        
        bool isUnlocked = PlayerStats.Instance.IsSpecialUnlocked(specialToSell);
        bool isEquipped = PlayerStats.Instance.equippedSpecialId == specialToSell.id;

        if (isEquipped) 
        {
            if (costText != null) costText.text = "";
            if (statusText != null) statusText.text = "Ausgerüstet";
            actionButton.interactable = false;
        } 
        else if (isUnlocked) 
        {
            if (costText != null) costText.text = "";
            if (statusText != null) statusText.text = "Ausrüsten";
            actionButton.interactable = true;
        } 
        else 
        {
            if (costText != null) costText.text = specialToSell.shopCost + " Münzen";
            if (statusText != null) statusText.text = "Kaufen";
            actionButton.interactable = PlayerStats.Instance.coins >= specialToSell.shopCost; 
        }
    }

    public void OnButtonClicked() 
    {
        bool isUnlocked = PlayerStats.Instance.IsSpecialUnlocked(specialToSell);
        
        if (!isUnlocked) 
        {
            PlayerStats.Instance.BuySpecial(specialToSell);
        } 
        else 
        {
            PlayerStats.Instance.EquipSpecial(specialToSell);
        }
        
        ShopUI mainShop = FindFirstObjectByType<ShopUI>();
        if (mainShop != null) mainShop.UpdateShopUI();
        
        foreach(var item in FindObjectsByType<SpecialShopItem>(FindObjectsSortMode.None)) 
        {
            item.UpdateUI();
        }
    }
}