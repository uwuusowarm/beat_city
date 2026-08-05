using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SpecialShopItem : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;

    public Button buyButton;
    public Button equipSlot1Button;
    public Button equipSlot2Button;

    private SpecialAttackDef _special;
    private ShopUI _shop;

    private void OnEnable()
    {
        UpdateUI();
    }

    public void Setup(SpecialAttackDef special, ShopUI shop)
    {
        _special = special;
        _shop = shop;

        if (nameText != null) nameText.text = _special.displayName;

        SetButtonText(buyButton, "Kaufen");
        SetButtonText(equipSlot1Button, "Input 1");
        SetButtonText(equipSlot2Button, "Input 2");
        
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
        
        equipSlot1Button.onClick.RemoveAllListeners();
        equipSlot1Button.onClick.AddListener(() => OnEquipClicked(1));

        equipSlot2Button.onClick.RemoveAllListeners();
        equipSlot2Button.onClick.AddListener(() => OnEquipClicked(2));
        
        UpdateUI();
    }

    public void UpdateUI() 
    {
        if (PlayerStats.Instance == null || _special == null) return;

        if (nameText != null) nameText.text = _special.displayName;

        bool isUnlocked = PlayerStats.Instance.IsSpecialUnlocked(_special);
        bool isEquippedSlot1 = PlayerStats.Instance.equippedSpecialId1 == _special.id;
        bool isEquippedSlot2 = PlayerStats.Instance.equippedSpecialId2 == _special.id;

        if (!isUnlocked) 
        {
            buyButton.gameObject.SetActive(true);
            equipSlot1Button.gameObject.SetActive(false);
            equipSlot2Button.gameObject.SetActive(false);
            
            if (costText != null) costText.text = _special.shopCost + " Coins";
            buyButton.interactable = PlayerStats.Instance.coins >= _special.shopCost; 
        }
        else
        {
            buyButton.gameObject.SetActive(false);
            equipSlot1Button.gameObject.SetActive(true);
            equipSlot2Button.gameObject.SetActive(true);
            
            if (costText != null) costText.text = "Gekauft";

            SetButtonText(equipSlot1Button, isEquippedSlot1 ? "Auf Slot 1" : "Input 1");
            SetButtonText(equipSlot2Button, isEquippedSlot2 ? "Auf Slot 2" : "Input 2");

            equipSlot1Button.interactable = !isEquippedSlot1;
            equipSlot2Button.interactable = !isEquippedSlot2;
        }
    }

    private void SetButtonText(Button btn, string text)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    private void OnBuyClicked() 
    {
        PlayerStats.Instance.BuySpecial(_special);
        _shop.UpdateShopUI(); 
    }

    private void OnEquipClicked(int slot)
    {
        PlayerStats.Instance.EquipSpecial(_special, slot);
        _shop.UpdateShopUI();
    }
}