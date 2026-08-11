using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyMovement), typeof(Health))]
public class EnemyCombat : MonoBehaviour
{
    private const float WindupRampFloor = 0.25f;

    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.5f;
    //[SerializeField] private float attackRange = 2f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float hitStunDuration = 0.5f;
    [Tooltip("Freeze-frame duration when this enemy's attack connects with the player. 0 disables it.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float hitStopDuration = 0.04f;
    [SerializeField] private float telegraphDuration = 0.35f;

    [Header("Hitbox")]
    [SerializeField] private Vector3 hitboxSize = new Vector3(1.2f, 1.6f, 1.2f);
    [SerializeField] private float hitboxForwardOffset = 0.75f;
    [SerializeField] private float hitboxHeight = 0.3f;

    private float _lastAttackTime;
    private Transform _player;
    private EnemyMovement _movement;
    private Health _health;
    private bool _isTelegraphing;
    private CharacterHighlightLayer _highlight;
    private Hitbox _attackHitbox;
    private Transform _attackHitboxTransform;

    public bool IsReadyToAttack => Time.time >= _lastAttackTime + attackCooldown;

    private float TelegraphDuration => (_movement != null && _movement.meleeSettings != null)
        ? _movement.meleeSettings.telegraphDuration
        : telegraphDuration;

    private Vector3 HitboxSize => (_movement != null && _movement.meleeSettings != null)
        ? _movement.meleeSettings.hitboxSize
        : hitboxSize;

    private float HitboxForwardOffset => (_movement != null && _movement.meleeSettings != null)
        ? _movement.meleeSettings.hitboxForwardOffset
        : hitboxForwardOffset;

    private float HitboxHeight => (_movement != null && _movement.meleeSettings != null)
        ? _movement.meleeSettings.hitboxHeight
        : hitboxHeight;

    private void Start()
    {
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<Health>();
        
        CharacterHighlightLayer.Ensure(gameObject);
        _highlight = GetComponent<CharacterHighlightLayer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        if (_health != null) _health.OnHit += HandleHit;

        var hitboxObj = new GameObject("MeleeHitbox");
        hitboxObj.transform.SetParent(transform, false);
        _attackHitbox = hitboxObj.AddComponent<Hitbox>();
        _attackHitbox.Owner = gameObject;
        _attackHitbox.Size = HitboxSize;
        _attackHitbox.Offset = Vector3.forward * HitboxForwardOffset;
        _attackHitbox.ApplyDamage = true;
        _attackHitbox.ShouldKnockdown = false;
        _attackHitboxTransform = hitboxObj.transform;
    }

    private void OnDestroy()
    {
        if (BeatEmUpDirector.Instance != null) BeatEmUpDirector.Instance.ReleaseToken(this);
    }

    private void OnDisable()
    {
        _isTelegraphing = false;
        ResetVisuals();
        if (_movement != null) _movement.SetAttackWindupActive(false);

        if (BeatEmUpDirector.Instance != null)
        {
            BeatEmUpDirector.Instance.ReleaseToken(this);
            BeatEmUpDirector.Instance.ReleaseFlankSlot(this);
        }
    }

    private void HandleHit(HitData data)
    {
        CancelAttackWindup();
        if (_movement != null) _movement.ForceRecalculateTactic();
    }

    public void CancelAttackWindup()
    {
        if (_isTelegraphing)
        {
            StopAllCoroutines();
            _isTelegraphing = false;
            ResetVisuals();
            if (_movement != null) _movement.SetAttackWindupActive(false);
        }
        if (BeatEmUpDirector.Instance != null) BeatEmUpDirector.Instance.ReleaseToken(this);
    }

    private void Update()
    {
        if (_player == null || _isTelegraphing) return;
        if (_health != null && _health.Current <= 0) return;
        if (_movement != null && !_movement.CanAct) return;

        bool isRanged = _movement != null && _movement.rangedSettings != null;

        Vector3 playerPos2D = new Vector3(_player.position.x, transform.position.y, _player.position.z);
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos2D);

        float attackRange;
        if (isRanged)
        {
            attackRange = _movement.RangedMeleeFallbackDistance;
            if (distanceToPlayer > attackRange) return; 
        }
        else
        {
            attackRange = _movement != null ? _movement.attackDistance : 2f;
        }

        if (distanceToPlayer <= attackRange && Time.time >= _lastAttackTime + attackCooldown)
        {
            if (BeatEmUpDirector.Instance != null && BeatEmUpDirector.Instance.RequestAttackToken(this))
            {
                StartCoroutine(TelegraphAndAttack());
            }
        }

    }

    public void TryRangedAttack(RangedEnemySettings rangedSettings)
    {
        Debug.Log($"TRY RANGED ATTACK: {name} | Time: {Time.time}");

        if (rangedSettings == null) return;
        if (_player == null) return;
        if (_health != null && _health.Current <= 0) return;
        if (_movement != null && !_movement.CanAct) return;
        if (!IsReadyToAttack) return;

        ShootRanged(rangedSettings);
        _lastAttackTime = Time.time;
    }

    private void ShootRanged(RangedEnemySettings rangedSettings)
    {
        Animator animator = _movement != null ? _movement.GetComponentInChildren<Animator>() : null;
        if (animator != null)
        {
            Debug.Log($"PLAY SHOOT: {name} | Time: {Time.time}");
            animator.Play("Shoot", 0, 0f);
        }

        Vector3 origin = transform.position + Vector3.up * 1f;

        float dirX = _player.position.x >= transform.position.x ? 1f : -1f;
        Vector3 direction = new Vector3(dirX, 0f, 0f);

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, rangedSettings.rangedShootRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (!hit.transform.CompareTag("Player")) continue; 

            Health playerHealth = hit.transform.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(new HitData
                {
                    Damage = rangedSettings.rangedDamage,
                    Source = gameObject,
                    HitPosition = hit.point
                });
            }

            break; 
        }

        Debug.DrawRay(origin, direction * rangedSettings.rangedShootRange, Color.red, 0.5f);
    }

    private IEnumerator TelegraphAndAttack()
    {
        _isTelegraphing = true;
        if (_movement != null) _movement.SetAttackWindupActive(true);
        try
        {
            if (_highlight != null) _highlight.SetWindup(true, WindupRampFloor);

            float duration = Mathf.Max(TelegraphDuration, 0.0001f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_movement != null && !_movement.CanAct)
                {
                    yield break;
                }

                if (_highlight != null)
                {
                    float t = elapsed / duration;
                    _highlight.SetWindupRamp(Mathf.Lerp(WindupRampFloor, 1f, t * t));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_health != null && _health.Current > 0 && _movement != null && _movement.CanAct)
            {
                Attack();
            }

            _lastAttackTime = Time.time;
        }
        finally
        {
            ResetVisuals();
            _isTelegraphing = false;
            if (_movement != null) _movement.SetAttackWindupActive(false);
            if (BeatEmUpDirector.Instance != null) BeatEmUpDirector.Instance.ReleaseToken(this);
            if (_movement != null) _movement.ForceRecalculateTactic();
        }
    }

    private void Attack()
    {
        Animator animator = null;

        bool isRanged = _movement != null && _movement.rangedSettings != null;

        if (_movement != null)
        {
            animator = _movement.GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            if (isRanged)
            {
                animator.Play("Attack", 0, 0f);
            }
            else
            {
                string[] attacks = { "Punch1", "Punch2", "Kick" };
                string randomAttack = attacks[Random.Range(0, attacks.Length)];
                animator.Play(randomAttack, 0, 0f);
            }
        }

        if (_attackHitbox == null || _player == null) return;

        float dirX = _player.position.x >= transform.position.x ? 1f : -1f;

        _attackHitboxTransform.position =
            transform.position + Vector3.up * HitboxHeight;

        _attackHitboxTransform.rotation =
            Quaternion.LookRotation(new Vector3(dirX, 0f, 0f));

        _attackHitbox.Size = HitboxSize;
        _attackHitbox.Offset = Vector3.forward * HitboxForwardOffset;
        _attackHitbox.Damage = attackDamage;
        _attackHitbox.KnockbackForce = knockbackForce;
        _attackHitbox.HitStunDuration = hitStunDuration;
        _attackHitbox.HitStopDuration = hitStopDuration;

        if (!isRanged)
        {
            _attackHitbox.Activate();
            _attackHitbox.Deactivate();
        }
        
    }

    public void EnableAttackHitbox()
    {
        if (_attackHitbox != null)
            _attackHitbox.Activate();
    }

    public void DisableAttackHitbox()
    {
        if (_attackHitbox != null)
            _attackHitbox.Deactivate();
    }



    private void ResetVisuals()
    {
        if (_highlight != null) _highlight.SetWindup(false);
    }

    public void ScaleDamage(float multiplier)
    {
        attackDamage = Mathf.RoundToInt(attackDamage * multiplier);
    }
}