using UnityEngine;
using TMPro;

public class HitCounter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hitText;
    
    private int _currentHits;

    private void Awake()
    {
        foreach (var hitbox in GetComponentsInChildren<Hitbox>(includeInactive: true))
        {
            hitbox.OnHitLanded += OnHitLanded;
        }

        if (TryGetComponent<Health>(out var health))
        {
            health.OnHit += OnPlayerHit;
        }
        
        UpdateUI();
    }

    private void OnHitLanded(GameObject target)
    {
        _currentHits++;
        UpdateUI();
        Debug.Log($"[HitCounter] Total Hits: {_currentHits}");
    }

    private void OnPlayerHit(HitData data)
    {
        if (_currentHits > 0)
        {
            Debug.Log($"[HitCounter] Streak broken at {_currentHits} hits!");
            _currentHits = 0;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        
    }
    
    public int GetCurrentHits() => _currentHits;
}
