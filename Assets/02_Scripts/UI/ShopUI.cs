using UnityEngine;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI damageText;

    [SerializeField] private TextMeshProUGUI healthCostText;
    [SerializeField] private TextMeshProUGUI damageCostText;

    private void OnEnable()
    {
        UpdateShopUI();
    }

    public void UpdateShopUI()
    {
        if (PlayerStats.Instance == null) return;
        if (coinText != null) coinText.text = $"Coins: {PlayerStats.Instance.coins}";
        if (healthText != null) healthText.text = $"Health: Lvl {PlayerStats.Instance.healthLevel}";
        if (damageText != null) damageText.text = $"Damage: Lvl {PlayerStats.Instance.damageLevel}";
        if (healthCostText != null) 
            healthCostText.text = $"{PlayerStats.Instance.GetUpgradeCost(PlayerStats.Instance.healthLevel)} Coins";
        
        if (damageCostText != null) 
            damageCostText.text = $"{PlayerStats.Instance.GetUpgradeCost(PlayerStats.Instance.damageLevel)} Coins";
    }

    public void BuyHealth()
    {
        if (PlayerStats.Instance.BuyHealthUpgrade())
        {
            UpdateShopUI();
        }
    }

    public void BuyDamage()
    {
        if (PlayerStats.Instance.BuyDamageUpgrade())
        {
            UpdateShopUI();
        }
    }
}