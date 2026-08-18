using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private Hitbox specialHitbox;
    [SerializeField] private Hitbox dashStrikeHitbox;
    [SerializeField] private Hitbox specialKickHitbox;
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private float vfxSpawnDistance = 1.5f;
    [SerializeField] private InputBuffer inputBuffer;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GameObject specialVFXPrefab;
    [SerializeField] private GameObject moonKickVFXPrefab;
    [SerializeField] private Transform vfxSpawnPoint;
    [SerializeField] private Transform moonKickVfxSpawnPoint;
    [SerializeField] private Meter specialMeter;

    public SpecialAttackId? equippedSpecial1;
    public SpecialAttackId? equippedSpecial2;

    [Header("Special Move")]
    [SerializeField] private SpecialMove specialMove;
    [SerializeField] private DashStrike dashStrike;

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

    private bool _hitLanded;

    private bool _inSpecialChain;
    private SpecialAttackId? _currentSpecialId;

    private Health _health;

    private bool _inHitStun;
    private float _hitStunEndTime;
    private int _hitStunEndFrame;

    private bool _watchdogArmed;
    private float _watchdogDeadline;

    private const float DefaultHitStunDuration = 0.35f;
    private const float DefaultWatchdogTimeout = 2f;

    private void Awake()
    {
        _settings = SettingsResolver.ResolvePlayerSettings();

        if (_settings == null)
        {
            Debug.LogWarning("[PlayerCombat] No PlayerSettings found (provider/resources).");
        }

        if (audioManager == null)
        {
            audioManager = FindFirstObjectByType<AudioManager>();
        }

        hitbox.OnHitLanded += HandleHitLanded;
        specialHitbox.OnHitLanded += HandleHitLanded;

        if (dashStrikeHitbox != null)
            dashStrikeHitbox.OnHitLanded += HandleHitLanded;
        if (specialKickHitbox != null)
        {
            specialKickHitbox.OnHitLanded += HandleHitLanded;
        }

        _health = GetComponent<Health>();
        if (_health != null)
        {
            _health.OnHit += HandleHitTaken;
        }
        else
        {
            Debug.LogWarning("[PlayerCombat] No Health on this object, attacks will not be cancelled when the player is hit.");
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnHit -= HandleHitTaken;
    }

    private void HandleHitLanded(GameObject target)
    {
        _hitLanded = true;
        OnHitLanded?.Invoke(target);
        PlayAttackSfx(CurrentAttackType);
    }

    private void HandleHitTaken(HitData hitData)
    {
        PlayerStateManager state = PlayerStateManager.Instance;
        if (state == null) return;

        if (_health != null && _health.Current <= 0) return;

        if (state.CurrentState != PlayerState.Idle &&
            state.CurrentState != PlayerState.Attacking &&
            state.CurrentState != PlayerState.SpecialAttacking &&
            state.CurrentState != PlayerState.Stunned)
        {
            return;
        }

        Debug.Log($"[PlayerCombat] Hit taken while {state.CurrentState}, cancelling attack");

        CancelCurrentAttack();
        EnterHitStun();
    }

    private void CancelCurrentAttack()
    {
        _isAttacking = false;
        _watchdogArmed = false;

        DeactivateAllHitboxes();
        ClearCurrentSpecial();
    }

    private void EnterHitStun()
    {
        float duration = _settings != null ? _settings.hitStunDuration : DefaultHitStunDuration;

        _inHitStun = true;
        _hitStunEndTime = Mathf.Max(_hitStunEndTime, Time.time + Mathf.Max(duration, 0f));
        _hitStunEndFrame = Time.frameCount + 1;

        PlayerStateManager.Instance.SetState(PlayerState.Stunned);
    }

    private void TickHitStun()
    {
        if (!_inHitStun) return;

        // The special/dash coroutines poll PlayerState.Stunned after Update, so hold the
        // state for one full frame even at duration 0 - otherwise they never see it.
        if (Time.frameCount <= _hitStunEndFrame) return;
        if (Time.time < _hitStunEndTime) return;

        _inHitStun = false;

        PlayerStateManager state = PlayerStateManager.Instance;
        if (state != null && state.CurrentState == PlayerState.Stunned)
            state.ResetToIdle();
    }

    private void TickAttackWatchdog()
    {
        if (!_watchdogArmed) return;

        if (!_isAttacking || _inSpecialChain)
        {
            _watchdogArmed = false;
            return;
        }

        if (Time.time < _watchdogDeadline) return;

        Debug.LogWarning("[PlayerCombat] Attack watchdog fired - no FinishAttack event arrived, forcing recovery.");

        DeactivateAllHitboxes();
        FinishAttack();
    }

    private void DeactivateAllHitboxes()
    {
        if (hitbox != null) hitbox.Deactivate();
        if (specialHitbox != null) specialHitbox.Deactivate();
        if (dashStrikeHitbox != null) dashStrikeHitbox.Deactivate();
        if (specialKickHitbox != null) specialKickHitbox.Deactivate();
    }

    private void Update()
    {
        TickHitStun();
        TickAttackWatchdog();

        if (_isAttacking || _inSpecialChain) return;
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
                    if (equippedSpecial1.HasValue) PerformSpecial(equippedSpecial1.Value);
                    break;
                case CombatInputType.SpecialChain:
                    if (equippedSpecial2.HasValue) PerformSpecial(equippedSpecial2.Value);
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

        float watchdogTimeout = _settings != null ? _settings.attackWatchdogTimeout : DefaultWatchdogTimeout;
        _watchdogArmed = watchdogTimeout > 0f;
        _watchdogDeadline = Time.time + watchdogTimeout;

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

    private void PerformSpecial(SpecialAttackId id)
    {
        SpecialAttackDef def = SpecialAttackCatalog.Get(id);
        if (def == null) return;

        if (!TrySpendMeter(def)) return;

        SetCurrentSpecial(id);

        switch (id)
        {
            case SpecialAttackId.ChainAttack:
                if (specialMove == null)
                {
                    Debug.LogWarning("[PlayerCombat] No SpecialMove component assigned, cannot perform Chain Attack.");
                    ClearCurrentSpecial();
                    RefundMeter(id);
                    break;
                }

                _isAttacking = true;
                if (!specialMove.TryActivate())
                {
                    _isAttacking = false;
                    ClearCurrentSpecial();
                    RefundMeter(id);
                }
                break;
            case SpecialAttackId.GroundSlam:
                DoGroundSlam();
                break;
            case SpecialAttackId.MoonKick:
                if (!DoMoonKick())
                {
                    ClearCurrentSpecial();
                    RefundMeter(id);
                }
                break;
            case SpecialAttackId.DashStrike:
                if (dashStrike == null)
                {
                    Debug.LogWarning("[PlayerCombat] No DashStrike component assigned, cannot perform Dash Strike.");
                    ClearCurrentSpecial();
                    RefundMeter(id);
                    break;
                }

                _isAttacking = true;
                if (!dashStrike.TryActivate())
                {
                    _isAttacking = false;
                    ClearCurrentSpecial();
                    RefundMeter(id);
                }
                break;
        }
    }

    private void SetCurrentSpecial(SpecialAttackId id)
    {
        _currentSpecialId = id;

        if (animator == null) return;

        switch (id)
        {
            case SpecialAttackId.GroundSlam:
                animator.SetInteger("SpecialId", 1);
                break;
            case SpecialAttackId.MoonKick:
                animator.SetInteger("SpecialId", 2);
                break;
            default:
                animator.SetInteger("SpecialId", 0);
                break;
        }
    }

    private void ClearCurrentSpecial()
    {
        _currentSpecialId = null;

        if (animator != null)
            animator.SetInteger("SpecialId", 0);
    }

    private bool TrySpendMeter(SpecialAttackDef def)
    {
        if (def.meterCost <= 0) return true;

        if (specialMeter == null)
        {
            Debug.LogWarning($"[PlayerCombat] No Meter assigned, cannot pay {def.meterCost} for {def.displayName}.");
            return false;
        }

        return specialMeter.TrySpend(def.meterCost);
    }

    private void DoGroundSlam()
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

    private bool DoMoonKick()
    {
        if (specialKickHitbox == null)
        {
            Debug.LogWarning("[PlayerCombat] No specialKickHitbox assigned, cannot perform Moon Kick.");
            return false;
        }

        _isAttacking = true;
        PlayerStateManager.Instance.SetState(PlayerState.Attacking);

        AdvanceCombo(CombatInputType.Special);

        if (_settings != null)
        {
            specialKickHitbox.Damage = _settings.specialDamage;
            specialKickHitbox.KnockbackForce = 0;
            specialKickHitbox.KnockUpForce = _settings.specialKnockup;
            specialKickHitbox.JugglingForce = _settings.jugglingForce;
            specialKickHitbox.ShouldKnockdown = false;
            specialKickHitbox.IsLauncher = true;
        }

        return true;
    }

    private void EnsureAudioManager()
    {
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }

        if (audioManager == null)
        {
            audioManager = FindFirstObjectByType<AudioManager>();
        }
    }

    public void SpawnSpecialVfx()
    {
        GameObject prefab = null;
        Transform spawnPoint = null;

        switch (_currentSpecialId)
        {
            case SpecialAttackId.GroundSlam:
                prefab = specialVFXPrefab;
                spawnPoint = vfxSpawnPoint;
                break;

            case SpecialAttackId.MoonKick:
                prefab = moonKickVFXPrefab;
                spawnPoint = moonKickVfxSpawnPoint;

                float currentY = transform.eulerAngles.y;
                bool isFacingLeft = Mathf.Abs(Mathf.DeltaAngle(currentY, -90f))
                                   < Mathf.Abs(Mathf.DeltaAngle(currentY, 90f));

                float targetX = isFacingLeft ? -98f : -82f;
                spawnPoint.localRotation = Quaternion.Euler(targetX, 90f, -90f);
                break;
        }

        if (prefab == null || spawnPoint == null)
            return;

        GameObject vfx = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        Destroy(vfx, 20f);
    }

    private Hitbox GetCurrentHitbox()
    {
        if (CurrentAttackType == CombatInputType.Special)
        {
            if (_currentSpecialId == SpecialAttackId.MoonKick && specialKickHitbox != null)
                return specialKickHitbox;

            if (_currentSpecialId == SpecialAttackId.DashStrike && dashStrikeHitbox != null)
                return dashStrikeHitbox;

            if (specialHitbox != null)
                return specialHitbox;
        }
        return hitbox;
    }

    private void PlaySwingSfx(CombatInputType input)
    {
        EnsureAudioManager();
        if (audioManager == null) return;

        int index = Mathf.Clamp(_comboStep - 1, 0, 2);

        switch (input)
        {
            case CombatInputType.Punch:
                audioManager.PlaySfx(SfxType.PunchMiss, index);
                break;
            case CombatInputType.Kick:
                audioManager.PlaySfx(SfxType.KickMiss, index);
                break;
        }
    }

    public void EnableHitbox()
    {

        Debug.Log("[PlayerCombat] EnableHitbox called");

        if (!_isAttacking && !_inSpecialChain)
        {
            Debug.Log("[PlayerCombat] EnableHitbox ignored - attack was already cancelled");
            return;
        }

        _hitLanded = false;

        Hitbox activeHitbox = GetCurrentHitbox();

        if (_settings != null)
            activeHitbox.HitStopDuration = _settings.combatHitStop;

        activeHitbox.Activate();
        OnAttackStarted?.Invoke();
    }

    public void DisableHitbox()
    {

        Debug.Log("[PlayerCombat] DisableHitbox called");

        Hitbox activeHitbox = GetCurrentHitbox();
        activeHitbox.Deactivate();

        if (!_isAttacking && !_inSpecialChain)
        {
            Debug.Log("[PlayerCombat] DisableHitbox ignored - attack was already cancelled");
            return;
        }

        if (!_hitLanded && CurrentAttackType != CombatInputType.Special)
        {
            PlaySwingSfx(CurrentAttackType);
        }

        OnAttackEnded?.Invoke();
    }

    public void FinishAttack()
    {
        Debug.Log("[PlayerCombat] FinishAttack called");

        _isAttacking = false;
        _watchdogArmed = false;

        if (_inSpecialChain)
        {
            if (specialMove != null)
                specialMove.OnChainHitFinished();
            return;
        }

        ClearCurrentSpecial();

        if (_inHitStun) return;

        PlayerStateManager.Instance.ResetToIdle();
    }

    public void SetSpecialChainActive(bool active)
    {
        _inSpecialChain = active;
    }

    public void AbortSpecialChain(bool refundMeter)
    {
        AbortSpecialAttack(SpecialAttackId.ChainAttack, refundMeter);
    }

    public void AbortSpecialAttack(SpecialAttackId id, bool refundMeter)
    {
        Debug.Log($"[PlayerCombat] AbortSpecialAttack called for {id} (refund: {refundMeter})");

        _inSpecialChain = false;
        _isAttacking = false;
        _watchdogArmed = false;

        ClearCurrentSpecial();

        if (refundMeter)
            RefundMeter(id);
    }

    private void RefundMeter(SpecialAttackId id)
    {
        if (specialMeter == null) return;

        SpecialAttackDef def = SpecialAttackCatalog.Get(id);
        if (def != null && def.meterCost > 0)
            specialMeter.AddMeter(def.meterCost);
    }

    public void SetSpecialChainAttack(CombatInputType type, int comboStep)
    {
        CurrentAttackType = type;
        _comboStep = comboStep;
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
        EnsureAudioManager();

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
            //case CombatInputType.Special:
            //    audioManager.PlaySfx(GetSpecialSfx(), 0);
            //    break;
        }
    }

    private SfxType GetSpecialSfx()
    {
        return _currentSpecialId switch
        {
            SpecialAttackId.GroundSlam => SfxType.SpecialGroundSmash,
            SpecialAttackId.MoonKick => SfxType.SpecialMoonKick,
            _ => SfxType.Special
        };
    }

    public void PlayAttackStartSfx()
    {
        Debug.Log("[PlayerCombat] PlayAttackStartSfx called");

        EnsureAudioManager();
        if (audioManager == null) return;

        if (CurrentAttackType == CombatInputType.Special)
        {
            audioManager.PlaySfx(GetSpecialSfx(), 0);
        }
    }

}
