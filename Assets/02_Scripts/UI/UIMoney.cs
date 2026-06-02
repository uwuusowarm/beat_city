using UnityEngine;
using TMPro;
 
public class UIMoney : MonoBehaviour
{
    [SerializeField] private Money money;
    [SerializeField] private TextMeshProUGUI moneyText;
 
    private void OnEnable()
    {
        if (money == null) return;
 
        money.OnMoneyChanged += HandleMoneyChanged;
        UpdateDisplay();
    }
 
    private void OnDisable()
    {
        if (money == null) return;
 
        money.OnMoneyChanged -= HandleMoneyChanged;
    }
 
    private void HandleMoneyChanged(int newTotal)
    {
        UpdateDisplay();
    }
 
    private void UpdateDisplay()
    {
        if (moneyText != null && money != null)
        {
            moneyText.text = $"${money.Current}";
        }
    }
}