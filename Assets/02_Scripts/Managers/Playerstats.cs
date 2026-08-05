using UnityEngine;
using UnityEngine.SceneManagement;

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
    public SpecialAttackId? equippedSpecialId1;
    public SpecialAttackId? equippedSpecialId2;

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

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyStatsToPlayer();
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
        if (player.TryGetComponent<PlayerCombat>(out var combat))
        {
            combat.equippedSpecial1 = equippedSpecialId1;
            combat.equippedSpecial2 = equippedSpecialId2;
        }
    }

    public bool IsSpecialUnlocked(SpecialAttackDef special)
    {
        if (special.unlockedByDefault) return true;
        return PlayerPrefs.GetInt("SpecialUnlocked_" + special.id, 0) == 1;
    }

    public bool BuySpecial(SpecialAttackDef special)
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

    public void EquipSpecial(SpecialAttackDef special, int slot)
    {
        if (IsSpecialUnlocked(special))
        {
            if (slot == 1)
            {
                if (equippedSpecialId2 == special.id) equippedSpecialId2 = null;
                equippedSpecialId1 = special.id;
            }
            else if (slot == 2)
            {
                if (equippedSpecialId1 == special.id) equippedSpecialId1 = null;
                equippedSpecialId2 = special.id;
            }

            PlayerPrefs.SetString("EquippedSpecial1", equippedSpecialId1?.ToString() ?? "");
            PlayerPrefs.SetString("EquippedSpecial2", equippedSpecialId2?.ToString() ?? "");
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

        equippedSpecialId1 = ParseSpecialId(PlayerPrefs.GetString("EquippedSpecial1", ""));
        equippedSpecialId2 = ParseSpecialId(PlayerPrefs.GetString("EquippedSpecial2", ""));
    }

    private static SpecialAttackId? ParseSpecialId(string value)
    {
        return System.Enum.TryParse<SpecialAttackId>(value, out var id) ? id : (SpecialAttackId?)null;
    }
}