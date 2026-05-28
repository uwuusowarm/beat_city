using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private InputBuffer inputBuffer;

    [Header("Punch Settings")]
    [SerializeField] private int punchDamage = 10;
    [SerializeField] private float punchBaseKnockback = 2f;
    [SerializeField] private float punchFinisherKnockup = 5f;

    [Header("Kick Settings")]
    [SerializeField] private int kickDamage = 15;
    [SerializeField] private float kickBaseKnockback = 3f;
    [SerializeField] private float kickFinisherKnockback = 8f;

    [Header("Combo Settings")]
    [SerializeField] private int maxComboSteps = 3;
    [SerializeField] private float comboResetTime = 0.8f;
    
    public event Action OnAttackStarted;
    public event Action OnAttackEnded;
    public event Action<GameObject> OnHitLanded;
    
    public Animator animator;

    private bool _isAttacking;
    private Coroutine _attackCoroutine;
    private bool _usePunch2;
    private int _comboStep;
    private float _lastAttackTime;

    private void Awake()
    {
        hitbox.OnHitLanded += target => OnHitLanded?.Invoke(target);
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (TryGetComponent<Health>(out var health))
        {
            health.OnHit += _ => ResetCombo();
        }
    }

    private void ResetCombo()
    {
        _comboStep = 0;
        _usePunch2 = false;
        if (_isAttacking && _attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _isAttacking = false;
            hitbox.Deactivate();
            PlayerStateManager.Instance.ResetToIdle();
        }
    }

    private void Update()
    {
        if (Time.time - _lastAttackTime > comboResetTime && !_isAttacking)
        {
            _comboStep = 0;
            _usePunch2 = false;
        }

        if (!PlayerStateManager.Instance.CanPerformAction() && PlayerStateManager.Instance.CurrentState != PlayerState.Attacking) return;

        if (inputBuffer.TryConsume(out CombatInputType input))
        {
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            
            switch (input)
            {
                case CombatInputType.Punch:
                    _attackCoroutine = StartCoroutine(DoAttack(CombatInputType.Punch));
                    break;
                case CombatInputType.Kick:
                    _attackCoroutine = StartCoroutine(DoAttack(CombatInputType.Kick));
                    break;
                case CombatInputType.Special:
                    //StartCoroutineSpecial - Need to be implemented yet
                    break;
            }
        }
    }

    private IEnumerator DoAttack(CombatInputType type)
    {
        _isAttacking = true;
        PlayerStateManager.Instance.SetState(PlayerState.Attacking);
        _comboStep++;
        _lastAttackTime = Time.time;
        
        hitbox.Deactivate();
        
        bool isComboEnd = _comboStep >= maxComboSteps;
        
        if (type == CombatInputType.Punch)
        {
            hitbox.Damage = punchDamage;
            if (isComboEnd)
            {
                hitbox.KnockbackForce = 0f;
                hitbox.KnockUpForce = punchFinisherKnockup;
                _comboStep = 0; 
            }
            else
            {
                hitbox.KnockbackForce = punchBaseKnockback;
                hitbox.KnockUpForce = 0f;
            }
            
            string punchAnimation = _usePunch2 ? "Punch2" : "Punch1";
            animator.Play(punchAnimation, 0, 0f);
            _usePunch2 = !_usePunch2;
        }
        else if (type == CombatInputType.Kick)
        {
            hitbox.Damage = kickDamage;
            if (isComboEnd)
            {
                hitbox.KnockbackForce = kickFinisherKnockback;
                hitbox.KnockUpForce = 0f;
                _comboStep = 0;
            }
            else
            {
                hitbox.KnockbackForce = kickBaseKnockback;
                hitbox.KnockUpForce = 0f;
            }
            
            animator.Play("Kick", 0, 0f);
        }

        hitbox.Activate();
        OnAttackStarted?.Invoke();
        
        yield return new WaitForSeconds(activeTime);

        hitbox.Deactivate();
        OnAttackEnded?.Invoke();
        _isAttacking = false;
        PlayerStateManager.Instance.ResetToIdle();
    }
}
