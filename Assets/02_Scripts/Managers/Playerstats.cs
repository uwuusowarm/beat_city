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

    [Header("Special Attacks")]
    public SpecialAttackSO[] allSpecialAttacks; 
    public string equippedSpecialId = "";

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

    public SpecialAttackSO GetSpecialById(string searchId)
    {
        foreach(var special in allSpecialAttacks)
        {
            if (special.id == searchId) return special;
        }
        return null;
    }

    public bool IsSpecialUnlocked(SpecialAttackSO special)
    {
        if (special.unlockedByDefault) return true;
        return PlayerPrefs.GetInt("SpecialUnlocked_" + special.id, 0) == 1; 
    }

    public bool BuySpecial(SpecialAttackSO special)
    {
        if (IsSpecialUnlocked(special)) return false;
        
        if (coins >= special.shopCost)
        {
            coins -= special.shopCost;
            PlayerPrefs.SetInt("SpecialUnlocked_" + special.id, 1);
            SaveStats();
            return true;
        }
        return false;
    }

    public void EquipSpecial(SpecialAttackSO special)
    {
        if (IsSpecialUnlocked(special))
        {
            equippedSpecialId = special.id;
            PlayerPrefs.SetString("EquippedSpecial", equippedSpecialId);
            PlayerPrefs.Save();
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

    public void AddCoins(int amount)
    {
        coins += amount;
        SaveStats();
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

        equippedSpecialId = PlayerPrefs.GetString("EquippedSpecial", "");
    }
}