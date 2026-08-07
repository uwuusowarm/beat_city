using UnityEngine;

public static class SettingsResolver
{
    public static PlayerSettings ResolvePlayerSettings(PlayerSettings preferred = null)
    {
        if (preferred != null) return preferred;

        if (GameSettingsProvider.Instance != null && GameSettingsProvider.Instance.PlayerSettings != null)
            return GameSettingsProvider.Instance.PlayerSettings;

        return Resources.Load<PlayerSettings>("PlayerSettings");
    }

    public static EnemySettings ResolveEnemySettings(EnemySettings preferred = null)
    {
        if (preferred != null) return preferred;

        if (GameSettingsProvider.Instance != null && GameSettingsProvider.Instance.EnemySettings != null)
            return GameSettingsProvider.Instance.EnemySettings;

        return Resources.Load<EnemySettings>("EnemySettings");
    }
}