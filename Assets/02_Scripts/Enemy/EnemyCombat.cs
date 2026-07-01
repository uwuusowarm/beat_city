using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyMovement), typeof(Health))]
public class EnemyCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float hitStunDuration = 0.5f;
    [SerializeField] private float telegraphDuration = 0.35f;

    private float _lastAttackTime;
    private Transform _player;
    private EnemyMovement _movement;
    private Health _health;
    private bool _isTelegraphing;
    private Renderer[] _renderers;
    private Color[] _originalColors;

    public bool IsReadyToAttack => Time.time >= _lastAttackTime + attackCooldown;

    private void Start()
    {
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<Health>();
        _renderers = GetComponentsInChildren<Renderer>();
        
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].material.HasProperty("_Color"))
                _originalColors[i] = _renderers[i].material.color;
        }
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        if (_health != null) _health.OnHit += HandleHit;
    }

    private void OnDestroy()
    {
        if (BeatEmUpDirector.Instance != null) BeatEmUpDirector.Instance.ReleaseToken(this);
    }

    private void OnDisable()
    {
        if (BeatEmUpDirector.Instance != null)
        {
            BeatEmUpDirector.Instance.ReleaseToken(this);
            BeatEmUpDirector.Instance.ReleaseFlankSlot(this);
        }
    }

    private void HandleHit(HitData data)
    {
        if (_isTelegraphing)
        {
            StopAllCoroutines();
            _isTelegraphing = false;
            ResetVisuals();
        }
        if (BeatEmUpDirector.Instance != null) BeatEmUpDirector.Instance.ReleaseToken(this);

        if (_movement != null) _movement.ForceRecalculateTactic();
    }

    private void Update()
    {
        if (_player == null || _isTelegraphing) return;
        if (_health != null && _health.Current <= 0) return;
        if (_movement != null && _movement.IsStunned) return;

        Vector3 playerPos2D = new Vector3(_player.position.x, transform.position.y, _player.position.z);
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos2D);

        if (distanceToPlayer <= attackRange && Time.time >= _lastAttackTime + attackCooldown)
        {
            if (BeatEmUpDirector.Instance != null && BeatEmUpDirector.Instance.RequestAttackToken(this))
            {
                StartCoroutine(TelegraphAndAttack());
            }
        }
    }

    private IEnumerator TelegraphAndAttack()
    {
        _isTelegraphing = true;
        foreach (var r in _renderers)
        {
            if (r != null && r.material.HasProperty("_Color"))
                r.material.color = Color.red;
        }

        yield return new WaitForSeconds(telegraphDuration);
        if (_health != null && _health.Current > 0 && !_movement.IsStunned)
        {
            Attack();
        }

        ResetVisuals();
        _isTelegraphing = false;
        _lastAttackTime = Time.time;
        if (BeatEmUpDirector.Instance != null) BeatEmUpDirector.Instance.ReleaseToken(this);

        if (BeatEmUpDirector.Instance != null) BeatEmUpDirector.Instance.ReleaseToken(this);
        if (_movement != null) _movement.ForceRecalculateTactic();
    }

    private void Attack()
    {
        Animator animator = null;
        if (_movement != null)
        {
            animator = _movement.GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            string[] attacks = { "Punch1", "Punch2", "Kick" };
            string randomAttack = attacks[Random.Range(0, attacks.Length)];
            animator.Play(randomAttack, 0, 0f);
        }

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
                ShouldKnockdown = false,
                Source = gameObject
            });
        }
    }

    private void ResetVisuals()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material.HasProperty("_Color"))
                _renderers[i].material.color = _originalColors[i];
        }
    }

    public void ScaleDamage(float multiplier)
    {
        attackDamage = Mathf.RoundToInt(attackDamage * multiplier);
    }
}