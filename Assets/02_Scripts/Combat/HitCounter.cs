using UnityEngine;
using TMPro;

public class HitCounter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hitText;
    
    private PlayerSettings _settings;
    private int _currentHits;
    private float _lastHitTime;

    private void Awake()
    {
        _settings = Resources.Load<PlayerSettings>("PlayerSettings");
        
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

    private void Update()
    {
        float resetTime = _settings != null ? _settings.comboResetTime : 2.0f;
        if (_currentHits > 0 && Time.time - _lastHitTime > resetTime)
        {
            ResetStreak();
        }
    }

    private void OnHitLanded(GameObject target)
    {
        _currentHits++;
        _lastHitTime = Time.time;
        UpdateUI();
        Debug.Log($"[HitCounter] Total Hits: {_currentHits}");
    }

    private void OnPlayerHit(HitData data)
    {
        if (_currentHits > 0)
        {
            ResetStreak();
        }
    }

    private void ResetStreak()
    {
        Debug.Log($"[HitCounter] Streak broken at {_currentHits} hits!");
        _currentHits = 0;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (hitText != null)
        {
            Debug.Log($"[HitCounter] Hit Text: {hitText.text}");
            string color = _currentHits > 5 ? "red" : "white";
            hitText.text = _currentHits > 0 ? $"<color={color}><align=center>HITS<br>{_currentHits}" : string.Empty;
        }
    }
    
    public int GetCurrentHits() => _currentHits;
}
