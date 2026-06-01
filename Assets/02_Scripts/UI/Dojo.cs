using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;


public class Dojo : MonoBehaviour
{
    [Serializable] public class SkillData
    {
        public string Name;
        [TextArea] public string Description;
        public int Cost;
        public float CostMod;
        public int MaxLevel;
        public Button Button;
        public int Level;
        public bool IsMaxed => MaxLevel > 0 && Level >= MaxLevel;
        public int CurrentCost => Mathf.RoundToInt(Cost * Mathf.Pow(CostMod, Level));
    }
    [SerializeField] private InputActionReference shopAction;
    [SerializeField] private Money playerMoney;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private bool pauseWhenOpen = true;
    [SerializeField] private SkillData[] skills;
    
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button confirmButton;
    
    [SerializeField] private string noSelectionHint = "Select a skill to view its details.";

    private bool _isOpen;
    private SkillData _selected;

    private void Start()
    {
        shopPanel.SetActive(false);
        
        foreach (var skill in skills)
        {
            skill.Button.onClick.AddListener(() => Select(skill));
        }
        confirmButton.onClick.AddListener(ConfirmPurchase);

        ClearSelection();
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

        if (pauseWhenOpen) Time.timeScale = 0f;

        ClearSelection();
    }

    private void CloseShop()
    {
        _isOpen = false;
        shopPanel.SetActive(false);

        if (pauseWhenOpen) Time.timeScale = 1f;
    }
    
    private void Select(SkillData skill)
    {
        _selected = skill;
        RefreshSelectionDisplay();
    }
    
    private void ClearSelection()
    {
        _selected = null;

        if (nameText != null) nameText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = noSelectionHint;

        if (costText != null) costText.gameObject.SetActive(false);
        confirmButton.interactable = false;
    }
    
    private void RefreshSelectionDisplay()
    {
        if (_selected == null)
        {
            ClearSelection();
            return;
        }

        if (nameText != null)
        {
            nameText.text = _selected.Level > 0 ? $"{_selected.Name} (Lv {_selected.Level})" : _selected.Name;
        }

        if (descriptionText != null) descriptionText.text = _selected.Description;

        if (costText != null)
        {
            costText.gameObject.SetActive(true);
            costText.text = _selected.IsMaxed ? "MAX" : $"${_selected.CurrentCost}";
        }
        
        confirmButton.interactable = !_selected.IsMaxed && playerMoney.Current >= _selected.CurrentCost;
    }
    
    private void ConfirmPurchase()
    {
        if (_selected == null)
            return;

        if (_selected.IsMaxed)
        {
            Debug.Log($"[DojoShop] '{_selected.Name}' is already maxed out.");
            return;
        }

        if (playerMoney.TrySpend(_selected.CurrentCost))
        {
            int paid = _selected.CurrentCost;
            _selected.Level++;

            ApplyUpgrade(_selected);

            Debug.Log($"[DojoShop] Purchased '{_selected.Name}' for ${paid} (now level {_selected.Level}).");
        }
        else
        {
            Debug.Log($"[DojoShop] Can't afford '{_selected.Name}' (need ${_selected.CurrentCost}, have ${playerMoney.Current}).");
        }
        RefreshSelectionDisplay();
    }
    
    private void ApplyUpgrade(SkillData skill)
    {
        switch (skill.Name)
        {
             /*case "DP":
                playerAbilities.DP = true;
                break;
                and so on*/

            default:
                Debug.Log($"[DojoShop]effect for '{skill.Name}' at level {skill.Level}");
                break;
        }
    }
}