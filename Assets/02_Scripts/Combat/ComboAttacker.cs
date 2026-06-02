using UnityEngine;

public class ComboAttacker : MonoBehaviour
{
    [SerializeField] private int comboThreshold = 3;
    [SerializeField] private float knockbackForce = 5f; 
    [SerializeField] private float knockUpForce = 6f;   
    [SerializeField] private float comboResetTime = 2f;
    [SerializeField] private PlayerCombat playerCombat;

    private int _hitCount;
    private float _lastHitTime;

    private void Awake()
    {
        foreach (var hitbox in GetComponentsInChildren<Hitbox>(includeInactive: true))
            hitbox.OnHitLanded += OnHitLanded;

        if (playerCombat == null)
        {
            playerCombat = GetComponent<PlayerCombat>();
        }

        if (TryGetComponent<Health>(out var myHealth))
        {
            myHealth.OnHit += OnReceivedDamage;
        }
    }

    private void OnHitLanded(GameObject target)
    {
        if (playerCombat == null) return;

        Debug.Log($"[Combo] Step {playerCombat.CurrentComboStep}/{comboThreshold}, Type: {playerCombat.CurrentAttackType}");

        if (playerCombat.CurrentComboStep < comboThreshold) return;

        if (playerCombat.CurrentAttackType == CombatInputType.Punch)
        {
            KnockUp(target);
        }
        else if (playerCombat.CurrentAttackType == CombatInputType.Kick)
        {
            KnockBack(target);
        }
    }

    private void KnockUp(GameObject target)
    {
        ComboFinisher(target, 0f, knockUpForce);
        Debug.Log($"[Combo] Punch finisher KnockUp: {target.name}");
    }

    private void KnockBack(GameObject target)
    {
        ComboFinisher(target, knockbackForce , 0f);
        Debug.Log($"[Combo] Kick finisher KnockBack: {target.name}");
    }

    private void ComboFinisher(GameObject target, float horizontalForce, float verticalForce)
    {
        if (!target.TryGetComponent<Rigidbody>(out var rb)) return;

        rb.linearVelocity = Vector3.zero;

        var horizontal = (target.transform.position - transform.position).normalized;
        horizontal.y = 0f;

        if (horizontal.sqrMagnitude > 0.001f)
            horizontal.Normalize();
        else
            horizontal = transform.forward;

        rb.AddForce(horizontal * horizontalForce + Vector3.up * verticalForce, ForceMode.Impulse);

        if (target.TryGetComponent<EnemyMovement>(out var em))
        {
            em.ApplyImpulse(verticalForce, horizontal * horizontalForce);
        }

        if (target.TryGetComponent<Health>(out var health))
        {
            health.TakeDamage(new HitData
            {
                Damage = 0,
                KnockbackDirection = horizontal,
                KnockbackForce = horizontalForce,
                KnockUpForce = verticalForce,
                Source = gameObject
            });
        }
    }

    private void OnReceivedDamage(HitData data)
    {
        if (playerCombat != null && playerCombat.CurrentComboStep > 0)
        {
            playerCombat.ResetCombo();
        }
    }
}
