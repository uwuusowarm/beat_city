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
        if (specialAttacksContainer == null || specialItemPrefab == null) return;

        for (int i = specialAttacksContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = specialAttacksContainer.GetChild(i).gameObject;
            child.transform.SetParent(null);
            Destroy(child);
        }

        foreach (var special in SpecialAttackCatalog.All)
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