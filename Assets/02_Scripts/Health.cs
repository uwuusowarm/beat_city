using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;

    public int Current { get; private set; }
    public int Max => maxHealth;

    public event Action<HitData> OnHit;
    public event Action OnDeath;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(HitData hitData)
    {
        if (Current <= 0) return;

        Current = Mathf.Max(0, Current - hitData.Damage);
        Debug.Log($"[Health] {gameObject.name} hit {hitData.Damage} damaged by {hitData.Source?.name} | HP: {Current}/{maxHealth}");
        OnHit?.Invoke(hitData);

        if (Current <= 0)
        {
            Debug.Log($"[Health] {gameObject.name} is dead!");
            OnDeath?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        Current = Mathf.Min(maxHealth, Current + amount);
    }
}
