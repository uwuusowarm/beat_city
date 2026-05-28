using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private InputBuffer inputBuffer;

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
        if (settings == null)
            settings = Resources.Load<PlayerSettings>("PlayerSettings");

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
        if (Time.time - _lastAttackTime > settings.comboResetTime && !_isAttacking)
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
        
        bool isComboEnd = _comboStep >= settings.maxComboSteps;
        
        if (type == CombatInputType.Punch)
        {
            hitbox.Damage = settings.punchDamage;
            if (isComboEnd)
            {
                hitbox.KnockbackForce = 0f;
                hitbox.KnockUpForce = settings.punchFinisherKnockup;
                _comboStep = 0; 
            }
            else
            {
                hitbox.KnockbackForce = settings.punchBaseKnockback;
                hitbox.KnockUpForce = 0f;
            }
            
            string punchAnimation = _usePunch2 ? "Punch2" : "Punch1";
            animator.Play(punchAnimation, 0, 0f);
            _usePunch2 = !_usePunch2;
        }
        else if (type == CombatInputType.Kick)
        {
            hitbox.Damage = settings.kickDamage;
            if (isComboEnd)
            {
                hitbox.KnockbackForce = settings.kickFinisherKnockback;
                hitbox.KnockUpForce = 0f;
                _comboStep = 0;
            }
            else
            {
                hitbox.KnockbackForce = settings.kickBaseKnockback;
                hitbox.KnockUpForce = 0f;
            }
            
            animator.Play("Kick", 0, 0f);
        }

        hitbox.Activate();
        OnAttackStarted?.Invoke();
        
        yield return new WaitForSeconds(settings.attackActiveTime);

        hitbox.Deactivate();
        OnAttackEnded?.Invoke();
        _isAttacking = false;
        PlayerStateManager.Instance.ResetToIdle();
    }
}
