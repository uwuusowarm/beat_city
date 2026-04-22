using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;



public class UpgradeShop : MonoBehaviour
{
    [Serializable] public struct UpgradeData
    {
        public string Name;
        public int Cost;
        public Button Button;
        public TextMeshProUGUI Label;
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
        if (playerMoney.TrySpend(upgrade.Cost))
        {
            Debug.Log($"[UpgradeShop] Purchased '{upgrade.Name}' for ${upgrade.Cost}");
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

    private void UpdateLabel(UpgradeData upgrade)
    {
        if (upgrade.Label != null)
        {
            upgrade.Label.text = $"{upgrade.Name} - ${upgrade.Cost}";
        }
    }
}