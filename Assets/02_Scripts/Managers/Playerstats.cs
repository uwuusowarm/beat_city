using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Currency")]
    public int coins;

    [Header("Upgrade Levels")]
    public int healthLevel = 1;
    public int damageLevel = 1;

    [Header("Stat Balances pro Level")]
    [SerializeField] private int hpBonusPerLevel = 20;
    [SerializeField] private float damageBonusPerLevel = 0.15f; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadStats();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ApplyStatsToPlayer();
    }

    public void ApplyStatsToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (player.TryGetComponent<Health>(out var health))
        {
            int baseHp = health.Max;
            int upgradedHp = baseHp + ((healthLevel - 1) * hpBonusPerLevel);
            health.SetMaxHealth(upgradedHp);
        }
    }

    public float GetDamageMultiplier()
    {
        return 1f + ((damageLevel - 1) * damageBonusPerLevel);
    }

    public int GetUpgradeCost(int currentLevel)
    {
        return currentLevel * 75; 
    }

    public void ResetCoins()
    {
        coins = 0;
        SaveStats();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        SaveStats();
    }

    public bool TrySpend(int amount)
    {
        if (coins < amount) return false;

        coins -= amount;
        SaveStats();
        return true;
    }

    public bool BuyHealthUpgrade()
    {
        int cost = GetUpgradeCost(healthLevel);
        if (coins >= cost)
        {
            coins -= cost;
            healthLevel++;
            SaveStats();
            return true;
        }
        return false;
    }

    public bool BuyDamageUpgrade()
    {
        int cost = GetUpgradeCost(damageLevel);
        if (coins >= cost)
        {
            coins -= cost;
            damageLevel++;
            SaveStats();
            return true;
        }
        return false;
    }

    public void SaveStats()
    {
        PlayerPrefs.SetInt("PlayerCoins", coins);
        PlayerPrefs.SetInt("StatHealthLevel", healthLevel);
        PlayerPrefs.SetInt("StatDamageLevel", damageLevel);
        PlayerPrefs.Save();
    }

    public void LoadStats()
    {
        coins = PlayerPrefs.GetInt("PlayerCoins", 0);
        healthLevel = PlayerPrefs.GetInt("StatHealthLevel", 1);
        damageLevel = PlayerPrefs.GetInt("StatDamageLevel", 1);
    }
}