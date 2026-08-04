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

    public SpecialAttackSO equippedSpecial1;
    public SpecialAttackSO equippedSpecial2;

    [Header("Special Move")]
    [SerializeField] private SpecialMove specialMove;

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
    private bool _inSpecialChain;

    private float _fist1InitialZ;
    private float _fist2InitialZ;

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
    }

    private void HandleHitLanded(GameObject target)
    {
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
                    if (equippedSpecial1 != null) StartCoroutine(DoSpecialAttack(equippedSpecial1));
                    break;
                case CombatInputType.SpecialChain:
                    if (equippedSpecial2 != null) StartCoroutine(DoSpecialAttack(equippedSpecial2));
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
        if (specialVFXPrefab == null) return;

        GameObject vfx = Instantiate(specialVFXPrefab, vfxSpawnPoint.position, Quaternion.identity);
        Destroy(vfx, 20.0f);
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

        PlayerStateManager.Instance.ResetToIdle();
    }

    public void SetSpecialChainActive(bool active)
    {
        _inSpecialChain = active;
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
                audioManager.PlaySfx(SfxType.Punch, index);
                break;
        }
    }

    private IEnumerator DoSpecialAttack(SpecialAttackSO special)
    {
        _isAttacking = true;
        PlayerStateManager.Instance.SetState(PlayerState.Attacking);
        OnAttackStarted?.Invoke();
        float originalAnimSpeed = animator != null ? animator.speed : 1f;
        
        if (special.isComboSequence && animator != null)
        {
            animator.speed = special.animationPlaybackSpeed;
        }
        else if (animator != null) animator.SetTrigger(special.animatorTrigger);

        if (special.moveForward && TryGetComponent<CharacterController>(out var cc))
        {
            StartCoroutine(PerformSpecialDash(cc, special.dashSpeed, special.totalDuration));
        }

        if (special.isProjectile && special.projectilePrefab != null)
        {
            Instantiate(special.projectilePrefab, transform.position + transform.forward, transform.rotation);
            yield return new WaitForSeconds(special.totalDuration);
        }
        else
        {
            if (hitbox != null)
            {
                hitbox.SetDimensions(special.hitboxSize, special.hitboxOffset);

                for (int i = 0; i < special.hitCount; i++)
                {
                    bool isFinisher = (i == special.hitCount - 1); 

                    if (special.isComboSequence && special.animationSequence != null && special.animationSequence.Length > 0 && animator != null)
                    {
                        string animToPlay = special.animationSequence[i % special.animationSequence.Length];
                        animator.Play(animToPlay, 0, 0f); 
                    }

                    hitbox.Damage = special.damagePerHit;
                    hitbox.KnockbackForce = isFinisher ? special.finalKnockback : 1f;
                    hitbox.KnockUpForce = isFinisher ? special.finalKnockup : 0f;
                    hitbox.ShouldKnockdown = isFinisher && special.finalHitShouldKnockdown;

                    hitbox.Activate();
                    yield return new WaitForSeconds(0.05f); 
                    hitbox.Deactivate();

                    if (!isFinisher) yield return new WaitForSeconds(special.timeBetweenHits);
                }
            }
        }

        if (animator != null) animator.speed = originalAnimSpeed;

        yield return new WaitForSeconds(attackCooldown);
        _isAttacking = false;
        PlayerStateManager.Instance.ResetToIdle();
        OnAttackEnded?.Invoke();
    }

    private IEnumerator PerformSpecialDash(CharacterController cc, float speed, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            cc.Move(transform.forward * speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
