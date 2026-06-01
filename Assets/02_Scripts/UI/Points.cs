using UnityEngine;
using System;

public class Points : MonoBehaviour
{
    [SerializeField] private int basePointsPerHit = 100;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private HitCounter hitCounter;

    public int Current { get; private set; }

    public event Action<int> OnPointsChanged;

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
        int streak = hitCounter != null ? hitCounter.GetCurrentHits() : 1;
        int multiplier = Mathf.Max(1, streak);
        int earned = basePointsPerHit * multiplier;

        Current += earned;
        OnPointsChanged?.Invoke(Current);

        Debug.Log($"[Points] +{earned} (x{multiplier}) for Total: {Current}");
    }

    public void AddPoints(int amount)
    {
        Current += amount;
        OnPointsChanged?.Invoke(Current);
    }
}