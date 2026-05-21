using UnityEngine;

[RequireComponent(typeof(EnemyMovement), typeof(Health))]
public class EnemyCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float hitStunDuration = 0.5f;

    private float _lastAttackTime;
    private Transform _player;
    private EnemyMovement _movement;
    private Health _health;

    private void Start()
    {
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<Health>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (_player == null) return;
        
        if (_health != null && _health.Current <= 0) return;
        if (_movement != null && _movement.IsStunned) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        if (distanceToPlayer <= attackRange)
        {
            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
    }

    private void Attack()
    {
        _lastAttackTime = Time.time;
        
        if (_player.TryGetComponent<Health>(out var playerHealth))
        {
            Vector3 knockbackDir = (_player.position - transform.position).normalized;
            knockbackDir.y = 0f;
            playerHealth.TakeDamage(new HitData
            {
                Damage = attackDamage,
                KnockbackDirection = knockbackDir,
                KnockbackForce = knockbackForce,
                KnockUpForce = 0f,
                HitStunDuration = hitStunDuration,
                Source = gameObject
            });

            Debug.Log($"[EnemyCombat] Attacked player for {attackDamage} damage with knockback {knockbackForce} and hit stun {hitStunDuration}");
        }
    }
}