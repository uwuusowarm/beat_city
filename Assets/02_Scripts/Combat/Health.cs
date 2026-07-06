using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth;
    [SerializeField] private UIHealth uiHealth;
    [SerializeField] private GameObject hitVfxPrefab;

    public int Current { get; private set; }
    public int Max
    {
        get => maxHealth;
        set => maxHealth = value;
    }


    public event Action<HitData> OnHit;
    public event Action OnDeath;

    private void Awake()
    {
        Current = maxHealth;
    }

    private void Start()
    {
        if (uiHealth == null && CompareTag("Player"))
        {
            uiHealth = UIManager.Instance?.PlayerHealthUI;
            if (uiHealth != null)
            {
                uiHealth.RefreshHealth();
            }
        }
    }

    public void TakeDamage(HitData hitData)
    {
        if (Current <= 0) return;

        Current = Mathf.Max(0, Current - hitData.Damage);
        SpawnHitVfx(hitData);
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
        if (uiHealth != null)
        {
            uiHealth.RefreshHealth();
        }
    }

    public void SetMaxHealth(int newMax)
    {
        maxHealth = newMax;
        Current = newMax;
    }

    private void SpawnHitVfx(HitData hitData)
    {
        if (hitVfxPrefab == null) return;

        GameObject vfx = Instantiate(hitVfxPrefab, hitData.HitPosition, Quaternion.identity);
        Destroy(vfx, 0.5f);
    }

    public void SetMaxHealth(int value)
    {
        maxHealth = value;
        Current = maxHealth;

        if (uiHealth != null)
        {
            uiHealth.RefreshHealth();
        }
    }
}
