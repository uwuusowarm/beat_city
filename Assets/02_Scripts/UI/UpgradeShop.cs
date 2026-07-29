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
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private bool pauseWhenOpen = true;
    [SerializeField] private UpgradeData[] upgrades;
    [SerializeField] private TextMeshProUGUI moneyText;

    [SerializeField] private Meter special;
    [SerializeField] private int specialBoost = 1;
    [SerializeField] private Health health;
    [SerializeField] private PlayerSettings _settings;
    [SerializeField] private int damageBoost = 1;

    public static bool IsOpen { get; private set; }

    private bool _isOpen;

    private void Awake()
    {
        if (_settings == null)
        {
            _settings = Resources.Load<PlayerSettings>("PlayerSettings");
        }

        if (special == null)
        {
            Debug.LogWarning("Meter script is missing in UpgradeShop script. Insert from Inspector");
        }
    }

    private void Start()
    {
        shopPanel.SetActive(false);
        IsOpen = false;
        CursorState.Refresh();

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
        IsOpen = true;
        shopPanel.SetActive(true);

        if (pauseWhenOpen)
            Time.timeScale = 0f;

        CursorState.Refresh();
        RefreshAllButtons();
    }

    private void CloseShop()
    {
        _isOpen = false;
        IsOpen = false;
        shopPanel.SetActive(false);

        if (pauseWhenOpen)
            Time.timeScale = 1f;

        CursorState.Refresh();
    }

    private void TryPurchase(UpgradeData upgrade)
    {
        if (upgrade.IsMaxed)
        {
            Debug.Log($"[UpgradeShop] '{upgrade.Name}' maxed out {upgrade.Level}");
            return;
        }
        if (PlayerStats.Instance.TrySpend(upgrade.Cost))
        {
            int paid = upgrade.Cost;
            upgrade.Level++;
            ApplyUpgrade(upgrade);
            Debug.Log($"[UpgradeShop] Purchased '{upgrade.Name}' for ${upgrade.Cost}, level: {upgrade.Level}");
        }
        else
        {
            Debug.Log($"[UpgradeShop] Can't afford '{upgrade.Name}' (need ${upgrade.Cost}, have ${PlayerStats.Instance.coins})");
        }

        RefreshAllButtons();
    }

    private void RefreshAllButtons()
    {
        foreach (var upgrade in upgrades)
        {
            upgrade.Button.interactable = PlayerStats.Instance.coins >= upgrade.Cost;
            UpdateLabel(upgrade);
        }

        if (moneyText != null)
        {
            moneyText.text = $"${PlayerStats.Instance.coins}";
        }
    }
    
    private void ApplyUpgrade(UpgradeData upgrade)
    {
        switch (upgrade.Name)
        {
            case "Max Health":
                health.Max += 10;
                Debug.Log($"[UpgradeShop] apply effect for '{upgrade.Name}' at level {upgrade.Level}. PlayerHealth now {health.Max}");
                UIManager.Instance.PlayerHealthUI.RefreshHealth();
                break;

            case "Damage Up":
                Debug.Log("DAMAGE UPGRADE WURDE AUSGEF�HRT");
                _settings.punchDamage += damageBoost;
                Debug.Log($"Damage now {_settings.punchDamage}");
                break;

            case "Special Up":
                Debug.Log("SPECIAL UPGRADE WURDE AUSGEF�HRT");
                special.BaseMeterPerHit += specialBoost;
                break;

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