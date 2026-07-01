using UnityEngine;

public class LevelStartTrigger : MonoBehaviour
{
    void Start()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ApplyStatsToPlayer();
        }
    }
}