using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerSettings))]
public class PlayerSettingsEditor : Editor
{
    private const int CoinGrant = 500;

    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(14f);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "These buttons write to PlayerPrefs, not to this asset. They work in edit mode and in play mode.",
            MessageType.None);

        EditorGUILayout.LabelField("Coins", CurrentCoins().ToString());
        EditorGUILayout.LabelField("Unlocked specials", DescribeUnlockedSpecials());

        EditorGUILayout.Space(4f);

        if (GUILayout.Button($"Add {CoinGrant} coins"))
            AddCoins(CoinGrant);

        if (GUILayout.Button("Reset purchased specials"))
            ResetPurchasedSpecials();
    }

    private static void AddCoins(int amount)
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddCoins(amount);
        }
        else
        {
            PlayerPrefs.SetInt(PlayerStats.CoinsKey, PlayerPrefs.GetInt(PlayerStats.CoinsKey, 0) + amount);
            PlayerPrefs.Save();
        }

        Debug.Log($"[PlayerSettings] Added {amount} coins (now {CurrentCoins()}).");
    }

    private static void ResetPurchasedSpecials()
    {
        foreach (var def in SpecialAttackCatalog.All)
            PlayerPrefs.DeleteKey(PlayerStats.SpecialUnlockedKeyPrefix + def.id);

        PlayerPrefs.SetString(PlayerStats.EquippedSpecial1Key, string.Empty);
        PlayerPrefs.SetString(PlayerStats.EquippedSpecial2Key, string.Empty);
        PlayerPrefs.Save();

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.equippedSpecialId1 = null;
            PlayerStats.Instance.equippedSpecialId2 = null;
            PlayerStats.Instance.ApplyStatsToPlayer();
        }

        Debug.Log($"[PlayerSettings] Reset {SpecialAttackCatalog.All.Count} purchased specials and cleared both slots.");
    }

    private static int CurrentCoins()
    {
        return PlayerStats.Instance != null
            ? PlayerStats.Instance.coins
            : PlayerPrefs.GetInt(PlayerStats.CoinsKey, 0);
    }

    private static string DescribeUnlockedSpecials()
    {
        var unlocked = new System.Collections.Generic.List<string>();

        foreach (var def in SpecialAttackCatalog.All)
        {
            bool isUnlocked = def.unlockedByDefault ||
                              PlayerPrefs.GetInt(PlayerStats.SpecialUnlockedKeyPrefix + def.id, 0) == 1;

            if (isUnlocked) unlocked.Add(def.displayName);
        }

        return unlocked.Count == 0 ? "none" : string.Join(", ", unlocked);
    }
}
