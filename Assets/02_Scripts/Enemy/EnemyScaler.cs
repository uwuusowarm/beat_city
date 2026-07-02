using UnityEngine;

public class EnemyScaler : MonoBehaviour
{
    [SerializeField] private float metersPerDifficultyStep = 50f;
    [SerializeField] private float hpIncreasePerStep = 0.2f;
    [SerializeField] private float damageIncreasePerStep = 0.15f;

    private void Start()
    {
        if (EndlessManager.Instance == null) return;

        float currentMeters = EndlessManager.Instance.GetCurrentMeters();
        
        int difficultyStep = Mathf.FloorToInt(currentMeters / metersPerDifficultyStep);

        if (difficultyStep > 0)
        {
            if (TryGetComponent<Health>(out var health))
            {
                float hpMultiplier = 1f + (difficultyStep * hpIncreasePerStep);
                int newMaxHp = Mathf.RoundToInt(health.Max * hpMultiplier);
                health.SetMaxHealth(newMaxHp);
            }

            if (TryGetComponent<EnemyCombat>(out var combat))
            {
                float damageMultiplier = 1f + (difficultyStep * damageIncreasePerStep);
                combat.ScaleDamage(damageMultiplier);
            }

            Debug.Log($"[Scaler] Gegner skaliert auf Stufe {difficultyStep}! HP-Multi: {1f + (difficultyStep * hpIncreasePerStep)}");
        }
    }
}