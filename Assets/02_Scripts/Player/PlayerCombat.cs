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
    }

    private void HandleHitLanded(GameObject target)
    {
        _hitLanded = true;
        OnHitLanded?.Invoke(target);
        PlayAttackSfx(CurrentAttackType);
    }

    private void Update()
    {
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

        _hitLanded = false;

        if (CurrentAttackType == CombatInputType.Special)
        {
            EnsureAudioManager();
            if (audioManager != null)
            {
                audioManager.PlaySfx(SfxType.Special, 0);
            }
        }


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

        if (_inSpecialChain)
        {
            if (specialMove != null)
                specialMove.OnChainHitFinished();
            return;
        }

        ClearCurrentSpecial();
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
            case CombatInputType.Special:
                audioManager.PlaySfx(SfxType.Special, index);
                break;
        }
    }

}
