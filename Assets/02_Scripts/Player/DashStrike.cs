using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashStrike : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private Hitbox dashHitbox;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform characterModel;
    [SerializeField] private InputBuffer inputBuffer;
    [SerializeField] private AfterimageEffect afterimageEffect;
    [SerializeField] private SpeedLinesEffect speedLinesEffect;
    [SerializeField] private Health playerHealth;

    [Header("Animator")]
    [SerializeField] private string dashStateName = "DashStrike";
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private float crossFadeDuration = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private const float ArriveThreshold = 0.15f;
    private const float GroundStick = 0.5f;

    private readonly HashSet<GameObject> _alreadyHit = new();

    private PlayerSettings _settings;

    private int _dashStateHash;
    private int _idleStateHash;
    private bool _dashStateAvailable;
    private bool _idleStateAvailable;
    private bool _enteredDashState;

    private bool _isPerforming;
    private bool _cancelled;
    private bool _endSignalled;
    private bool _strikeConnected;
    private int _hitsLanded;

    private GameObject _currentTarget;
    private bool _travelling;
    private float _travelTimer;
    private bool _animHeld;

    private float _originalAnimSpeed = 1f;
    private bool _animSpeedOverridden;

    public bool IsPerforming => _isPerforming;

    private int HitCount => Mathf.Max(1, _settings != null ? _settings.dashStrikeHitCount : 3);
    private int Damage => _settings != null ? _settings.dashStrikeDamage : 20;
    private int FinalDamage => _settings != null ? _settings.dashStrikeFinalDamage : 30;

    private float SearchRadius => _settings != null ? _settings.dashStrikeSearchRadius : 15f;
    private float ApproachDistance => _settings != null ? _settings.dashStrikeApproachDistance : 1.2f;
    private bool PreferNewTargets => _settings == null || _settings.dashStrikePreferNewTargets;
    private bool RequireTarget => _settings == null || _settings.dashStrikeRequireTarget;

    private float DashSpeed => _settings != null ? _settings.dashStrikeSpeed : 40f;
    private float MaxTravelTime => _settings != null ? _settings.dashStrikeMaxTravelTime : 0.5f;
    private bool UnscaledTravel => _settings == null || _settings.dashStrikeUnscaledTravel;
    private bool HoldAnimUntilArrival => _settings == null || _settings.dashStrikeHoldAnimUntilArrival;
    private bool SnapOnHit => _settings == null || _settings.dashStrikeSnapOnHit;

    private float Knockup => _settings != null ? _settings.dashStrikeKnockup : 6f;
    private float FinalKnockup => _settings != null ? _settings.dashStrikeFinalKnockup : 10f;
    private float Knockback => _settings != null ? _settings.dashStrikeKnockback : 1f;
    private float FinalKnockback => _settings != null ? _settings.dashStrikeFinalKnockback : 4f;
    private bool FinalKnockdown => _settings == null || _settings.dashStrikeFinalKnockdown;

    private Vector3 HitboxSize => _settings != null ? _settings.dashStrikeHitboxSize : new Vector3(3f, 3f, 3f);
    private Vector3 HitboxOffset => _settings != null ? _settings.dashStrikeHitboxOffset : Vector3.zero;

    private float HitFreeze => _settings != null ? _settings.dashStrikeHitFreeze : 0.05f;
    private float SlowMoDuration => _settings != null ? _settings.dashStrikeSlowMoDuration : 0.12f;
    private float SlowMoTimeScale => _settings != null ? _settings.dashStrikeSlowMoTimeScale : 0.35f;
    private float FinalHitFreeze => _settings != null ? _settings.dashStrikeFinalHitFreeze : 0.12f;
    private float FinalSlowMoDuration => _settings != null ? _settings.dashStrikeFinalSlowMoDuration : 0.3f;
    private float FinalSlowMoTimeScale => _settings != null ? _settings.dashStrikeFinalSlowMoTimeScale : 0.2f;

    private bool UseAfterimage => _settings == null || _settings.dashStrikeAfterimage;
    private bool UseSpeedLines => _settings == null || _settings.dashStrikeSpeedLines;

    private float AnimSpeed => _settings != null ? _settings.dashStrikeAnimSpeed : 1f;
    private float Recovery => _settings != null ? _settings.dashStrikeRecovery : 0.15f;
    private float MaxDuration => _settings != null ? _settings.dashStrikeMaxDuration : 3f;

    private void Awake()
    {
        _settings = SettingsResolver.ResolvePlayerSettings();

        if (_settings == null)
        {
            Debug.LogWarning("[DashStrike] No PlayerSettings found (provider/resources).");
        }

        if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
        if (controller == null) controller = GetComponent<CharacterController>();
        if (inputBuffer == null) inputBuffer = GetComponent<InputBuffer>();
        if (afterimageEffect == null) afterimageEffect = GetComponent<AfterimageEffect>();
        if (speedLinesEffect == null) speedLinesEffect = GetComponent<SpeedLinesEffect>();
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (characterModel == null)
        {
            var movement = GetComponent<MovementPlayer>();
            if (movement != null) characterModel = movement.characterModel;
        }

        _dashStateHash = Animator.StringToHash(dashStateName);
        _idleStateHash = Animator.StringToHash(idleStateName);

        if (animator != null)
        {
            _dashStateAvailable = animator.HasState(0, _dashStateHash);
            _idleStateAvailable = animator.HasState(0, _idleStateHash);

            if (!_dashStateAvailable)
            {
                Debug.LogWarning($"[DashStrike] Animator on {gameObject.name} has no '{dashStateName}' state on layer 0 - " +
                                 "add it to the controller with the dash clip.");
            }
        }

        if (dashHitbox != null)
            dashHitbox.OnHitLanded += HandleDashHitLanded;
    }

    private void OnDestroy()
    {
        if (dashHitbox != null)
            dashHitbox.OnHitLanded -= HandleDashHitLanded;
    }

    private void OnDisable()
    {
        if (!_isPerforming) return;

        StopAllCoroutines();

        _travelling = false;
        _currentTarget = null;
        _alreadyHit.Clear();

        RestoreVisuals();

        _isPerforming = false;
        _cancelled = false;

        if (playerCombat != null)
            playerCombat.AbortSpecialAttack(SpecialAttackId.DashStrike, false);
    }

    public bool TryActivate()
    {
        if (_isPerforming) return false;

        if (dashHitbox == null)
        {
            Debug.LogError($"[DashStrike] No dash hitbox assigned on {gameObject.name}.");
            return false;
        }

        if (animator != null && !_dashStateAvailable)
        {
            Debug.LogError($"[DashStrike] Animator has no '{dashStateName}' state on layer 0 - special not started.");
            return false;
        }

        _alreadyHit.Clear();
        _currentTarget = AcquireTarget();

        if (_currentTarget == null && RequireTarget)
        {
            Debug.Log("[DashStrike] No target in range - special not started.");
            return false;
        }

        _isPerforming = true;
        _cancelled = false;
        _endSignalled = false;
        _enteredDashState = false;
        _hitsLanded = 0;

        StartCoroutine(Run());
        return true;
    }

    private IEnumerator Run()
    {
        PlayerStateManager.Instance.SetState(PlayerState.SpecialAttacking);

        if (inputBuffer != null) inputBuffer.Clear();
        if (afterimageEffect != null && UseAfterimage) afterimageEffect.Activate();
        if (speedLinesEffect != null && UseSpeedLines) speedLinesEffect.Activate();

        if (animator != null)
        {
            _originalAnimSpeed = animator.speed;
            _animSpeedOverridden = true;
            animator.speed = AnimSpeed;

            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hit");
            animator.SetBool("IsGrabbing", false);

            if (_dashStateAvailable && animator.isActiveAndEnabled)
                animator.CrossFadeInFixedTime(_dashStateHash, crossFadeDuration, 0);
        }

        BeginTravel(holdAnimation: false);

        float startedUnscaled = Time.unscaledTime;

        while (true)
        {
            if (ShouldCancel())
            {
                _cancelled = true;
                break;
            }

            if (Time.unscaledTime - startedUnscaled >= MaxDuration)
            {
                Debug.LogWarning($"[DashStrike] watchdog fired after {MaxDuration:F2}s - forcing the end " +
                                 $"({_hitsLanded}/{HitCount} strikes landed).");
                break;
            }

            if (_endSignalled) break;
            if (_hitsLanded >= HitCount && ClipFinished()) break;

            yield return null;
        }

        if (!_cancelled && Recovery > 0f)
            yield return new WaitForSecondsRealtime(Recovery);

        Finish();
    }

    private void Update()
    {
        if (!_travelling) return;

        float delta = UnscaledTravel ? Time.unscaledDeltaTime : Time.deltaTime;
        _travelTimer += delta;

        if (!IsTargetValid(_currentTarget))
        {
            _currentTarget = AcquireTarget();

            if (_currentTarget == null)
            {
                StopTravel();
                return;
            }
        }

        FaceTarget(_currentTarget);

        Vector3 toDestination = GetDestination(_currentTarget) - transform.position;
        toDestination.y = 0f;

        float distance = toDestination.magnitude;

        if (distance <= ArriveThreshold || _travelTimer >= MaxTravelTime)
        {
            StopTravel();
            return;
        }

        if (controller == null || !controller.enabled)
        {
            StopTravel();
            return;
        }

        Vector3 step = toDestination / distance * Mathf.Min(DashSpeed * delta, distance);
        step.y = -GroundStick * delta;

        controller.Move(step);
    }

    public void OnStrikeHit()
    {
        if (!_isPerforming) return;

        if (!IsTargetValid(_currentTarget))
            _currentTarget = AcquireTarget();

        if (_currentTarget != null && SnapOnHit)
            SnapToTarget(_currentTarget);

        StopTravel();

        bool isFinal = _hitsLanded >= HitCount - 1;
        ConfigureHitbox(isFinal);

        if (playerCombat != null)
            playerCombat.SetSpecialChainAttack(CombatInputType.Special, _hitsLanded + 1);

        _strikeConnected = false;
        dashHitbox.Activate();
        dashHitbox.Deactivate();

        if (_currentTarget != null)
            _alreadyHit.Add(_currentTarget);

        if (_strikeConnected)
            ApplySlowMotion(isFinal);

        _hitsLanded++;

        Debug.Log($"[DashStrike] strike {_hitsLanded}/{HitCount} on " +
                  $"{(_currentTarget != null ? _currentTarget.name : "nothing")} (connected: {_strikeConnected})");

        if (_hitsLanded < HitCount)
            BeginTravel(holdAnimation: true);
    }

    public void OnSeekNextTarget()
    {
        if (!_isPerforming) return;
        if (_hitsLanded >= HitCount) return;

        BeginTravel(holdAnimation: true);
    }

    public void OnDashStrikeEnd()
    {
        if (!_isPerforming) return;

        if (_hitsLanded < HitCount)
        {
            Debug.LogWarning($"[DashStrike] DashStrikeEnd fired after only {_hitsLanded}/{HitCount} strikes - ignored. " +
                             "Move the event behind the last DashStrikeHit in the clip, or delete it.");
            return;
        }

        _endSignalled = true;
    }

    private void BeginTravel(bool holdAnimation)
    {
        _currentTarget = AcquireTarget();
        _travelTimer = 0f;

        if (_currentTarget == null)
        {
            _travelling = false;
            ReleaseAnimationHold();
            return;
        }

        _travelling = true;

        if (holdAnimation && HoldAnimUntilArrival)
            HoldAnimation();
    }

    private void StopTravel()
    {
        _travelling = false;
        _travelTimer = 0f;

        ReleaseAnimationHold();
    }

    private void HoldAnimation()
    {
        if (animator == null || _animHeld) return;

        _animHeld = true;
        animator.speed = 0f;
    }

    private void ReleaseAnimationHold()
    {
        if (animator == null || !_animHeld) return;

        _animHeld = false;
        animator.speed = AnimSpeed;
    }

    private Vector3 GetDestination(GameObject target)
    {
        Vector3 fromTarget = transform.position - target.transform.position;
        fromTarget.y = 0f;

        if (fromTarget.sqrMagnitude < 0.0001f)
            fromTarget = characterModel != null ? -characterModel.forward : Vector3.right;

        Vector3 destination = target.transform.position + fromTarget.normalized * ApproachDistance;
        destination.y = transform.position.y;

        return destination;
    }

    private void SnapToTarget(GameObject target)
    {
        Vector3 destination = GetDestination(target);

        Vector3 offset = destination - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude > ArriveThreshold * ArriveThreshold)
        {
            bool controllerWasEnabled = controller != null && controller.enabled;

            if (controllerWasEnabled) controller.enabled = false;
            transform.position = destination;
            if (controllerWasEnabled) controller.enabled = true;
        }

        FaceTarget(target);
    }

    private void FaceTarget(GameObject target)
    {
        if (characterModel == null || target == null) return;

        float dirX = target.transform.position.x >= transform.position.x ? 1f : -1f;
        characterModel.rotation = Quaternion.LookRotation(new Vector3(dirX, 0f, 0f));
    }

    private GameObject AcquireTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, SearchRadius);

        GameObject closestUnhit = null;
        GameObject closestAny = null;
        float closestUnhitDistance = float.MaxValue;
        float closestAnyDistance = float.MaxValue;

        foreach (var col in hits)
        {
            if (col == null) continue;
            if (!col.TryGetComponent<Hurtbox>(out var hurtbox)) continue;

            GameObject owner = hurtbox.Owner;
            if (owner == null || owner == gameObject) continue;
            if (owner.CompareTag(gameObject.tag)) continue;
            if (!IsTargetValid(owner)) continue;

            Vector3 flat = owner.transform.position - transform.position;
            flat.y = 0f;
            float distance = flat.sqrMagnitude;

            if (distance < closestAnyDistance)
            {
                closestAnyDistance = distance;
                closestAny = owner;
            }

            if (!_alreadyHit.Contains(owner) && distance < closestUnhitDistance)
            {
                closestUnhitDistance = distance;
                closestUnhit = owner;
            }
        }

        return PreferNewTargets && closestUnhit != null ? closestUnhit : closestAny;
    }

    private bool IsTargetValid(GameObject target)
    {
        if (target == null || !target.activeInHierarchy) return false;

        if (target.TryGetComponent<Health>(out var health) && health.Current <= 0) return false;
        if (target.TryGetComponent<EnemyMovement>(out var movement) && movement.IsInvulnerableWhileDowned) return false;

        return true;
    }

    private void ConfigureHitbox(bool isFinal)
    {
        dashHitbox.ApplyDamage = true;
        dashHitbox.Damage = isFinal ? FinalDamage : Damage;
        dashHitbox.KnockUpForce = isFinal ? FinalKnockup : Knockup;
        dashHitbox.KnockbackForce = isFinal ? FinalKnockback : Knockback;
        dashHitbox.ShouldKnockdown = isFinal && FinalKnockdown;
        dashHitbox.IsLauncher = !dashHitbox.ShouldKnockdown && dashHitbox.KnockUpForce > 0f;

        dashHitbox.JuggleType = dashHitbox.ShouldKnockdown ? JuggleType.None : JuggleType.Launcher;
        dashHitbox.JugglingForce = _settings != null ? _settings.jugglingForce : 3f;
        dashHitbox.HitStopDuration = isFinal ? FinalHitFreeze : HitFreeze;
        dashHitbox.SetDimensions(HitboxSize, HitboxOffset);
    }

    private void ApplySlowMotion(bool isFinal)
    {
        if (HitStop.Instance == null) return;

        float duration = isFinal ? FinalSlowMoDuration : SlowMoDuration;
        if (duration <= 0f) return;

        HitStop.Instance.DoSlowMotion(duration, isFinal ? FinalSlowMoTimeScale : SlowMoTimeScale);
    }

    private void HandleDashHitLanded(GameObject target)
    {
        _strikeConnected = true;
        
        if (target != null)
            _alreadyHit.Add(target);
    }

    private bool ClipFinished()
    {
        if (animator == null || !_dashStateAvailable) return true;
        if (animator.IsInTransition(0)) return false;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (state.shortNameHash == _dashStateHash)
        {
            _enteredDashState = true;
            return state.normalizedTime >= 0.98f;
        }

        return _enteredDashState;
    }

    private bool ShouldCancel()
    {
        if (playerHealth != null && playerHealth.Current <= 0)
            return true;

        if (PlayerStateManager.Instance != null &&
            PlayerStateManager.Instance.CurrentState == PlayerState.Stunned)
            return true;

        return false;
    }

    private void Finish()
    {
        _travelling = false;
        _currentTarget = null;
        _alreadyHit.Clear();

        RestoreVisuals();

        if (inputBuffer != null) inputBuffer.DiscardSpecialInputs();

        _isPerforming = false;

        if (_cancelled)
        {
            _cancelled = false;

            if (playerCombat != null)
                playerCombat.AbortSpecialAttack(SpecialAttackId.DashStrike, false);

            return;
        }

        if (playerCombat != null)
            playerCombat.FinishAttack();
        else
            PlayerStateManager.Instance?.ResetToIdle();
    }

    private void RestoreVisuals()
    {
        _animHeld = false;

        if (animator != null)
        {
            if (_animSpeedOverridden)
                animator.speed = _originalAnimSpeed;

            if (_idleStateAvailable && animator.isActiveAndEnabled && !animator.IsInTransition(0) &&
                animator.GetCurrentAnimatorStateInfo(0).shortNameHash == _dashStateHash)
            {
                animator.CrossFadeInFixedTime(_idleStateHash, crossFadeDuration, 0);
            }
        }

        _animSpeedOverridden = false;

        if (afterimageEffect != null)
            afterimageEffect.Deactivate();

        if (speedLinesEffect != null)
            speedLinesEffect.Deactivate();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, SearchRadius);

        if (!Application.isPlaying || _currentTarget == null) return;

        Vector3 origin = transform.position + Vector3.up * 0.1f;

        Gizmos.color = _travelling ? Color.green : new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawLine(origin, _currentTarget.transform.position);
        Gizmos.DrawWireSphere(GetDestination(_currentTarget), 0.25f);
    }
}
