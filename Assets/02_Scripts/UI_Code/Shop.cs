using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;



public class UpgradeShop : MonoBehaviour
{
    [Serializable] public class UpgradeData
    {
        public string Name;
        public int Cost;
        public float CostMod;
        public int MaxLevel;
        public Button Button;
        public TextMeshProUGUI Label;
        public int Level;
        public bool IsMaxed => MaxLevel > 0 && Level >= MaxLevel;
        public int CurrentCost => Mathf.RoundToInt(Cost * Mathf.Pow(CostMod, Level));
    }
    [SerializeField] private InputActionReference shopAction;
    [SerializeField] private Money playerMoney;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private bool pauseWhenOpen = true;
    [SerializeField] private UpgradeData[] upgrades;

    private bool _isOpen;

    private void Start()
    {
        shopPanel.SetActive(false);

        foreach (var upgrade in upgrades)
        {
            upgrade.Button.onClick.AddListener(() => TryPurchase(upgrade));
            UpdateLabel(upgrade);
        }
    }

    private void OnEnable()
    {
        shopAction.action.performed += OnShopInput;
        shopAction.action.Enable();
    }

    private void OnDisable()
    {
        shopAction.action.performed -= OnShopInput;
        shopAction.action.Disable();
    }

    private void OnShopInput(InputAction.CallbackContext _)
    {
        if (_isOpen) CloseShop();
        else OpenShop();
    }

    private void OpenShop()
    {
        _isOpen = true;
        shopPanel.SetActive(true);

        if (pauseWhenOpen)
            Time.timeScale = 0f;

        RefreshAllButtons();
    }

    private void CloseShop()
    {
        _isOpen = false;
        shopPanel.SetActive(false);

        if (pauseWhenOpen)
            Time.timeScale = 1f;
    }

    private void TryPurchase(UpgradeData upgrade)
    {
        if (upgrade.IsMaxed)
        {
            Debug.Log($"[UpgradeShop] '{upgrade.Name}' maxed out {upgrade.Level}");
            return;
        }
        if (playerMoney.TrySpend(upgrade.Cost))
        {
            int paid = upgrade.Cost;
            upgrade.Level++;
            ApplyUpgrade(upgrade);
            Debug.Log($"[UpgradeShop] Purchased '{upgrade.Name}' for ${upgrade.Cost}, level: {upgrade.Level}");
        }
        else
        {
            Debug.Log($"[UpgradeShop] Can't afford '{upgrade.Name}' (need ${upgrade.Cost}, have ${playerMoney.Current})");
        }

        RefreshAllButtons();
    }

    private void RefreshAllButtons()
    {
        foreach (var upgrade in upgrades)
        {
            upgrade.Button.interactable = playerMoney.Current >= upgrade.Cost;
            UpdateLabel(upgrade);
        }
    }
    
    private void ApplyUpgrade(UpgradeData upgrade)
    {
        switch (upgrade.Name)
        {
            /*case "Max Health":
                playerStats.MaxHealth += 10;
                break;
 
            and so on */
            default:
                Debug.Log($"[UpgradeShop] apply effect for '{upgrade.Name}' at level {upgrade.Level}.");
                break;
        }
    }

    private void UpdateLabel(UpgradeData upgrade)
    {
        if (upgrade.Label == null) return;
 
        if (upgrade.IsMaxed) upgrade.Label.text = $"{upgrade.Name} (Lv {upgrade.Level}) - MAX";
        
        else if (upgrade.Level > 0) upgrade.Label.text = $"{upgrade.Name} (Lv {upgrade.Level}) - ${upgrade.CurrentCost}";
        
        else upgrade.Label.text = $"{upgrade.Name} - ${upgrade.CurrentCost}";
    }
}