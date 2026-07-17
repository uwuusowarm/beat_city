using UnityEngine;

public class GameSettingsProvider : MonoBehaviour
{
    public static GameSettingsProvider Instance { get; private set; }

    [Header("Global Settings")]
    [SerializeField] private PlayerSettings playerSettings;
    [SerializeField] private EnemySettings enemySettings;

    public PlayerSettings PlayerSettings => playerSettings;
    public EnemySettings EnemySettings => enemySettings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameSettingsProvider] Multiple providers found. Keeping the first one.");
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}