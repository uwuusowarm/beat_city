using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private Hitbox specialHitbox;
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private float vfxSpawnDistance = 1.5f;
    [SerializeField] private InputBuffer inputBuffer;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GameObject specialVFXPrefab;
    [SerializeField] private Transform vfxSpawnPoint;
    [SerializeField] private Meter specialMeter;

    [Header("Combo")]
    private PlayerSettings _settings;

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
        _settings = Resources.Load<PlayerSettings>("PlayerSettings");

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
                    DoPunch();
                    break;
                case CombatInputType.Kick:
                    DoKick();
                    break;
                case CombatInputType.Special:
                    if (specialMeter.TrySpend(_settings.specialCost))
                    {
                        DoSpecial();
                    }
                    else
                    {
                        Debug.Log("Need more specialPoints");
                    }
                        break;
            }
        }
    }

    private void AdvanceCombo(CombatInputType input)
    {
        float resetTime = _settings != null ? _settings.comboResetTime : 1.0f;
        if (Time.time - _lastAttackTime > resetTime)
        {
            _comboStep = 0;
        }

        _comboStep++;

        int maxSteps = _settings != null ? _settings.maxComboSteps : 3;
        if (_comboStep > maxSteps)
        {
            _comboStep = 1;
        }

        _lastAttackTime = Time.time;
        CurrentAttackType = input;

        Debug.Log($"[PlayerCombat] Combo Step: {_comboStep}/{maxSteps} Attack: {input}");

        if (animator != null)
        {
            animator.SetInteger("ComboStep", _comboStep);
            animator.SetInteger("AttackType", (int)input);
            animator.SetTrigger("Attack");
        }
    }

    private void DoPunch()
    {
        _isAttacking = true;
        PlayerStateManager.Instance.SetState(PlayerState.Attacking);

        AdvanceCombo(CombatInputType.Punch);

        if (_settings != null)
        {
            hitbox.Damage = _settings.punchDamage;
            bool isFinisher = _comboStep >= _settings.maxComboSteps;
            hitbox.KnockbackForce = _settings.punchBaseKnockback; 
            hitbox.KnockUpForce = isFinisher ? _settings.punchFinisherKnockup : 0f;
            hitbox.JugglingForce = _settings.jugglingForce;
            hitbox.ShouldKnockdown = false;
            hitbox.IsLauncher = isFinisher;
        }
    }

    private void DoKick()
    {
        _isAttacking = true;
        PlayerStateManager.Instance.SetState(PlayerState.Attacking);

        AdvanceCombo(CombatInputType.Kick);

        if (_settings != null)
        {
            hitbox.Damage = _settings.kickDamage;
            bool isFinisher = _comboStep >= _settings.maxComboSteps;
            hitbox.KnockbackForce = isFinisher ? _settings.kickFinisherKnockback : _settings.kickBaseKnockback;
            hitbox.KnockUpForce = 0f;
            hitbox.JugglingForce = _settings.jugglingForce;
            hitbox.ShouldKnockdown = isFinisher;
        }
    }

    private void DoSpecial()
    {
        _isAttacking = true;
        PlayerStateManager.Instance.SetState(PlayerState.Attacking);

        AdvanceCombo(CombatInputType.Special);

        if (_settings != null)
        {
            specialHitbox.Damage = _settings.specialDamage + _settings.punchDamage;
            specialHitbox.KnockbackForce = 0;
            specialHitbox.KnockUpForce = _settings.specialKnockup;
            specialHitbox.JugglingForce = _settings.jugglingForce;
            specialHitbox.ShouldKnockdown = true;
            specialHitbox.IsLauncher = true;
        }
    }

    public void SpawnSpecialVfx()
    {
        if (specialVFXPrefab == null) return;

        GameObject vfx = Instantiate(specialVFXPrefab, vfxSpawnPoint.position, Quaternion.identity);
        Destroy(vfx, 0.5f);
    }

    private Hitbox GetCurrentHitbox()
    {
        if (CurrentAttackType == CombatInputType.Special && specialHitbox != null)
        {
            return specialHitbox;
        }
        return hitbox; 
    }

    public void EnableHitbox()
    {

        Debug.Log("[PlayerCombat] EnableHitbox called");

        Hitbox activeHitbox = GetCurrentHitbox();
        activeHitbox.Activate();
        OnAttackStarted?.Invoke();
    }

    public void DisableHitbox()
    {

        Debug.Log("[PlayerCombat] DisableHitbox called");

        Hitbox activeHitbox = GetCurrentHitbox();
        activeHitbox.Deactivate();
        OnAttackEnded?.Invoke();
    }

    public void FinishAttack()
    {
        Debug.Log("[PlayerCombat] FinishAttack called");

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
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }

        if (audioManager == null)
        {
            Debug.LogWarning("[PlayerCombat] PlayAttackSfx: audioManager is still null! Trying to find it now...");
            audioManager = FindFirstObjectByType<AudioManager>();
        }
        
        if (audioManager == null)
        {
            Debug.LogError("[PlayerCombat] PlayAttackSfx: NO AudioManager found in scene or via Instance!");
            return;
        }

        int index = _comboStep - 1;

        switch (input)
        {
            case CombatInputType.Punch:
                audioManager.PlaySfx(SfxType.Punch, index);
                break;

            case CombatInputType.Kick:
                audioManager.PlaySfx(SfxType.Kick, index);
                break;
            case CombatInputType.Special:
                break;
        }
    }
}
