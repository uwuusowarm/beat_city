using UnityEngine;
using TMPro; 

public class MainMenuCoinDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    private void Update()
    {
        if (PlayerStats.Instance != null && moneyText != null)
        {
            moneyText.text = $"COINS: {PlayerStats.Instance.coins}";
        }
    }
}