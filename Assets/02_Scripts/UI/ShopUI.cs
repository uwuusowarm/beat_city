using UnityEngine;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI damageText;

    [SerializeField] private TextMeshProUGUI healthCostText;
    [SerializeField] private TextMeshProUGUI damageCostText;

    [SerializeField] private Transform specialAttacksContainer; 
    [SerializeField] private GameObject specialItemPrefab;

    private void OnEnable()
    {
        GenerateSpecialAttackList();
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
        
        if (specialAttacksContainer != null)
        {
            foreach (Transform child in specialAttacksContainer)
            {
                if (child.TryGetComponent<SpecialShopItem>(out var item))
                {
                    item.UpdateUI();
                }
            }
        }
    }

    private void GenerateSpecialAttackList()
    {
        if (PlayerStats.Instance == null || specialAttacksContainer == null || specialItemPrefab == null) return;

        foreach (Transform child in specialAttacksContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var special in PlayerStats.Instance.allSpecialAttacks)
        {
            GameObject newBtn = Instantiate(specialItemPrefab, specialAttacksContainer);
            if (newBtn.TryGetComponent<SpecialShopItem>(out var shopItem))
            {
                shopItem.Setup(special, this);
            }
        }
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