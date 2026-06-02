using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private InputBuffer inputBuffer;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioManager audioManager;

    [Header("Combo")]
    [SerializeField] private float comboResetTime = 1.0f;

    public event Action OnAttackStarted;

    public event Action OnAttackEnded;

    public event Action<GameObject> OnHitLanded;

    public CombatInputType CurrentAttackType { get; private set; }
    public int CurrentComboStep => _comboStep;

    private bool _isAttacking;

    private int _comboStep;

    private float _lastAttackTime;

    private int _punchIndex;

    private float _fist1InitialZ;
    private float _fist2InitialZ;

    private void Awake()
    {
        if (audioManager == null)
        {
            audioManager = FindFirstObjectByType<AudioManager>();
        }
        hitbox.OnHitLanded += HandleHitLanded;
    }

    private void HandleHitLanded(GameObject target)
    {
        OnHitLanded?.Invoke(target);
        PlayAttackSfx(CurrentAttackType);
    }

    private void Update()
    {
        if (_isAttacking) return;
        if (!PlayerStateManager.Instance.CanPerformAction()) return;

        if (inputBuffer.TryConsume(out CombatInputType input))
        {
            switch (input)
            {
                case CombatInputType.Punch:
                    StartCoroutine(DoPunch());
                    break;
                case CombatInputType.Kick:
                    StartCoroutine(DoKick());
                    break;
                case CombatInputType.Special:
                    //StartCoroutineSpecial - Need to be implemented yet
                    break;
            }
        }
    }

    private void AdvanceCombo(CombatInputType input)
    {
        if (Time.time - _lastAttackTime > comboResetTime)
        {
            _comboStep = 0;
        }

        _comboStep++;

        if (_comboStep > 3)
        {
            _comboStep = 1;
        }

        _lastAttackTime = Time.time;
        CurrentAttackType = input;

        Debug.Log($"[PlayerCombat] Combo Step: {_comboStep}/3 Attack: {input}");

        if (animator != null)
        {
            animator.SetInteger("ComboStep", _comboStep);
            animator.SetInteger("AttackType", (int)input);
            animator.SetTrigger("Attack");
        }
    }

    private IEnumerator DoPunch()
    {
        _isAttacking = true;
        PlayerStateManager.Instance.SetState(PlayerState.Attacking);

        AdvanceCombo(CombatInputType.Punch);

        hitbox.Activate();
        OnAttackStarted?.Invoke();

        yield return new WaitForSeconds(activeTime);

        hitbox.Deactivate();
        OnAttackEnded?.Invoke();

        yield return new WaitForSeconds(attackCooldown);

        _isAttacking = false;
        PlayerStateManager.Instance.ResetToIdle();
    }

    private IEnumerator DoKick()
    {
        _isAttacking = true;
        PlayerStateManager.Instance.SetState(PlayerState.Attacking);

        AdvanceCombo(CombatInputType.Kick);

        hitbox.Activate();
        OnAttackStarted?.Invoke();

        yield return new WaitForSeconds(activeTime);

        hitbox.Deactivate();
        OnAttackEnded?.Invoke();

        yield return new WaitForSeconds(attackCooldown);

        _isAttacking = false;
        PlayerStateManager.Instance.ResetToIdle();
    }

    public void ResetCombo()
    {
        _comboStep = 0;
        _lastAttackTime = 0f;
        CurrentAttackType = CombatInputType.None;

        Debug.Log("[PlayerCombat] Combo reset");
    }

    private void PlayAttackSfx(CombatInputType input)
    {
        if (audioManager == null) return;

        int index = _comboStep - 1;

        switch (input)
        {
            case CombatInputType.Punch:
                audioManager.PlaySfx(SfxType.Punch, index);
                break;

            case CombatInputType.Kick:
                audioManager.PlaySfx(SfxType.Kick, index);
                break;
        }
    }
}
