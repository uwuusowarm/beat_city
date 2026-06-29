using UnityEngine;
using System;

public class Meter : MonoBehaviour
{
    [SerializeField] private int baseMeterPerHit = 1;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private HitCounter hitCounter;

    public int Current { get; private set; }

    public int BaseMeterPerHit { get => baseMeterPerHit; set => baseMeterPerHit = value; }

    public event Action<int> OnMeterChanged;

    private void OnEnable()
    {
        if (playerCombat != null) playerCombat.OnHitLanded += HandleHitLanded;
    }

    private void OnDisable()
    {
        if (playerCombat != null) playerCombat.OnHitLanded -= HandleHitLanded;
    }

    private void HandleHitLanded(GameObject target)
    {
        int earned = baseMeterPerHit;
        int streak = hitCounter.GetCurrentHits();
        if (streak % 5 == 0) earned = streak * 2;
        
        Current += earned;
        OnMeterChanged?.Invoke(Current);

        Debug.Log($"[Meter] +{earned} for Total: {Current}");
    }
    
    public bool TrySpend(int amount)
    {
        if (Current < amount) return false;

        Current -= amount;
        OnMeterChanged?.Invoke(Current);
        return true;
    }

    public void AddMeter(int amount)
    {
        Current += amount;
        OnMeterChanged?.Invoke(Current);
    }
}