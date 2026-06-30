using System;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private GameObject owner;
    [SerializeField] private Vector3 size = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 offset = Vector3.zero;
    [SerializeField] private int damage = 10;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockUpForce = 0f;
    [SerializeField] private float jugglingForce = 0f;
    [SerializeField] private float hitStunDuration = 0.3f;
    [SerializeField] private float hitStopDuration = 0.08f;
    [SerializeField] private bool shouldKnockdown = false;
    [SerializeField] private bool isLauncher = false;
    [SerializeField] private JuggleType juggleType = JuggleType.None;
    [SerializeField] private bool applyDamage = true;
    [SerializeField] private LayerMask targetLayer = ~0;

    public int Damage { get => damage; set => damage = value; }
    public float KnockbackForce { get => knockbackForce; set => knockbackForce = value; }
    public float KnockUpForce { get => knockUpForce; set => knockUpForce = value; }
    public float JugglingForce { get => jugglingForce; set => jugglingForce = value; }
    public bool ShouldKnockdown { get => shouldKnockdown; set => shouldKnockdown = value; }
    public bool IsLauncher { get => isLauncher; set => isLauncher = value; }
    public JuggleType JuggleType { get => juggleType; set => juggleType = value; }

    public bool ApplyDamage { get => applyDamage; set => applyDamage = value; }
    public Vector3 Size => size;
    public Vector3 Offset => offset;

    public event Action<GameObject> OnHitLanded;

    private readonly HashSet<IDamageable> _hitTargets = new();
    private bool _isActive;

    private void Awake()
    {
        if (owner == null)
            owner = transform.root.gameObject;
    }

    public void Activate()
    {
        _isActive = true;
        _hitTargets.Clear();
        Fire();
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    private void Fire()
    {
        var worldCenter = transform.TransformPoint(offset);
        var hits = Physics.OverlapBox(worldCenter, size * 0.5f, transform.rotation);

        Debug.Log($"[Hitbox] Fire! {hits.Length} Collisions found at {worldCenter}");

        foreach (var col in hits)
        {
            if (!col.TryGetComponent<Hurtbox>(out var hurtbox))
            {
                Debug.Log($"[Hitbox] -> {col.gameObject.name}: No Hurtbox-Component");
                continue;
            }
            if (hurtbox.Owner == owner)
            {
                Debug.Log($"[Hitbox] -> Self-Hit ignored ({owner.name})");
                continue;
            }

            var damageable = hurtbox.Owner.GetComponent<IDamageable>();
            if (damageable == null || !_hitTargets.Add(damageable)) continue;

            if (applyDamage)
            {
                var knockbackDir = (hurtbox.Owner.transform.position - owner.transform.position).normalized;
                knockbackDir.y = 0f;

                float effectiveKnockUp = knockUpForce;
                JuggleType effectiveJuggleType = juggleType;
                
                var enemyMovement = hurtbox.Owner.GetComponent<EnemyMovement>();
                if (enemyMovement != null && enemyMovement.IsInThrowState)
                {
                    effectiveKnockUp = jugglingForce;
                    if (effectiveJuggleType == JuggleType.None)
                        effectiveJuggleType = JuggleType.Juggle;
                }

                damageable.TakeDamage(new HitData
                {
                    Damage = damage,
                    KnockbackDirection = knockbackDir,
                    KnockbackForce = knockbackForce,
                    KnockUpForce = effectiveKnockUp,
                    HitStunDuration = hitStunDuration,
                    ShouldKnockdown = shouldKnockdown,
                    IsLauncher = isLauncher,
                    JuggleType = effectiveJuggleType,
                    Source = owner,
                    HitPosition = col.ClosestPoint(worldCenter)
                });

                if (owner.CompareTag("Player") && damageable is Health enemyHealth)
                {
                    Debug.Log($"[DEBUG_LOG] Hitbox: Player hit enemy {hurtbox.Owner.name}.");
                    UIManager manager = UIManager.Instance;
                    if (manager != null)
                    {
                        manager.UpdateEnemyHealthFocus(enemyHealth);
                    }
                }
            }

            OnHitLanded?.Invoke(hurtbox.Owner);
            HitStop.Instance?.Do(hitStopDuration);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        if (_isActive)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawCube(offset, size);
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
            Gizmos.DrawCube(offset, size);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        }

        Gizmos.DrawWireCube(offset, size);
    }
}
