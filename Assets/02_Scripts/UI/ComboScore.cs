using System;
using System.Linq;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(HitCounter))]
public class ComboScore : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI inGameScoreText;
    [SerializeField] private TextMeshProUGUI winScoreText;
    [SerializeField] private TextMeshProUGUI loseScoreText;

    [Header("Combo Settings")]
    [SerializeField] private int basePointsPerHit = 10;
    [SerializeField]
    private ComboTier[] comboTiers = new ComboTier[]
    {
        new ComboTier { minHits = 0,  multiplier = 1f },
        new ComboTier { minHits = 10, multiplier = 10f },
        new ComboTier { minHits = 20, multiplier = 50f },
    };

    private HitCounter _hitCounter;
    private int _comboScore; 
    private int _totalScore; 

    public event Action<int> OnScoreAdded;
    public event Action<int> OnComboLost;

    [Serializable]
    public struct ComboTier
    {
        public int minHits;
        public float multiplier;
    }

    private void Awake()
    {
        _hitCounter = GetComponent<HitCounter>();
        comboTiers = comboTiers.OrderBy(t => t.minHits).ToArray();

        UpdateUI();
    }

    private void OnEnable()
    {
        _hitCounter.OnHitLanded += HandleHitLanded;
        _hitCounter.OnStreakReset += HandleStreakReset;
    }

    private void OnDisable()
    {
        _hitCounter.OnHitLanded -= HandleHitLanded;
        _hitCounter.OnStreakReset -= HandleStreakReset;
    }

    private void HandleHitLanded(int currentHits)
    {
        float multiplier = GetMultiplierForHits(currentHits);
        int points = Mathf.RoundToInt(basePointsPerHit * multiplier);

        _comboScore += points;
        _totalScore += points;

        OnScoreAdded?.Invoke(points);
        UpdateUI();

        Debug.Log($"[ComboScore] +{points} (x{multiplier}) | Combo: {_comboScore} | Total: {_totalScore}");
    }

    private void HandleStreakReset(int hitsBeforeReset)
    {
        Debug.Log($"[ComboScore] Combo lost: {_comboScore}");
        OnComboLost?.Invoke(_comboScore);

        _comboScore = 0;
        UpdateUI();
    }

    private float GetMultiplierForHits(int hits)
    {
        float multiplier = 1f;
        foreach (var tier in comboTiers)
        {
            if (hits >= tier.minHits)
                multiplier = tier.multiplier;
            else
                break;
        }
        return multiplier;
    }

    private void UpdateUI()
    {
        string text = $"SCORE: {_totalScore}";

        if (inGameScoreText != null)
            inGameScoreText.text = text;

        if (winScoreText != null)
            winScoreText.text = text;

        if (loseScoreText != null)
            loseScoreText.text = text;
    }

    public int GetComboScore() => _comboScore;
    public int GetTotalScore() => _totalScore;
}