using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Ranged Melee Fallback")]
    [SerializeField] private float rangedMeleeFallbackDistance = 1.8f;
    public float RangedMeleeFallbackDistance => rangedMeleeFallbackDistance;

    [Header("Settings")]
    public EnemySettings meleeSettings;
    public RangedEnemySettings rangedSettings;

    [Header("Visuals")]
    public Transform characterModel;
    [SerializeField] private Animator animator;
    [Header("Grabbed Animation")]
    [SerializeField, Min(0.01f)] private float grabbedFallPlaybackSpeed = 1f;
    
    public EnemyState CurrentState { get; private set; } = EnemyState.Grounded;
    
    public bool IsStunned => CurrentState == EnemyState.HitStun || 
                             CurrentState == EnemyState.Knockdown || 
                             CurrentState == EnemyState.StandingUp;
    

    public bool CanAct => CurrentState == EnemyState.Grounded || CurrentState == EnemyState.HitStun;

    public bool IsDowned => CurrentState == EnemyState.Knockdown ||
                            CurrentState == EnemyState.StandingUp;

    public bool IsInvulnerableWhileDowned => InvulnerableWhileDowned && IsDowned;

    public bool IsInThrowState 
    { 
        get => CurrentState == EnemyState.Launched || CurrentState == EnemyState.Airborne || CurrentState == EnemyState.Grabbed;
        set 
        {
            if (value)
            {
                if (CurrentState == EnemyState.Grounded || CurrentState == EnemyState.HitStun)
                    SetState(EnemyState.Launched);
            }
            else if (CurrentState == EnemyState.Launched || CurrentState == EnemyState.Airborne)
            {
                SetState(EnemyState.Grounded);
            }
        }
    }

    private int rangedSide = 1;
    private float rangedShootTimer;

    private int _juggleCount;
    private float _juggleDecayMultiplier = 1f;
    private float _hoverTimer;
    private bool _hoverPending;

    private float _juggleCeiling;

    private float _airTimer;

    private float _lastLaunchImpactTime = -999f;

    private float _squashTimer;
    private const float SquashDuration = 0.12f;
    private static readonly Vector3 SquashScale = new Vector3(1.3f, 0.7f, 1.3f);

    private bool _isBeingThrown;
    public bool IsBeingThrown => _isBeingThrown;
    
    private bool _wasThrownSkipReset;
    private Coroutine _grabbedFallCoroutine;
    
    private float _groundYPosition;
    
    private Transform player;
    private float tacticTimer;
    private bool isFlanking;
    private float flankAngle;
    private float flankDirection = 1f;
    private Vector3 currentFlankOffset;

    private CharacterController controller;
    private float verticalVelocity;
    private float hitStunTimer;
    private float _stateTimer;
    private float _wobblePhase;
    private Vector3 externalForce;
    public Vector3 ExternalForce { get => externalForce; set => externalForce = value; }
    private float horizontalDrag = 5f;

    private Vector3 rangedTargetPosition;
    private float rangedRepositionTimer;
    private bool isAiming;
    private float rangedAimTimer;
    private float rangedDodgeTimer;
    private float rangedDodgeDirection;
    private float rangedRecoveryTimer;

    private Health _health;
    private EnemyCombat _combat;

    private bool _isAttackWindingUp;
    private bool FreezeDuringTelegraph => meleeSettings != null ? meleeSettings.freezeDuringTelegraph : true;
    private float TelegraphMoveSpeedMultiplier => meleeSettings != null ? meleeSettings.telegraphMoveSpeedMultiplier : 0f;

    private EnemyObstacleAvoidance _obstacleAvoidance;
    
    public float moveSpeed => meleeSettings != null ? meleeSettings.moveSpeed : 3f;
    private float StopDistance => meleeSettings != null ? meleeSettings.stopDistance : 1.5f;
    public float attackDistance => meleeSettings != null ? meleeSettings.attackDistance : 1.5f;
    
    public float minRepositionTime => meleeSettings != null ? meleeSettings.minRepositionTime : 1.0f;
    public float maxRepositionTime => meleeSettings != null ? meleeSettings.maxRepositionTime : 3.0f;
    
    public float comboHitStun => meleeSettings != null ? meleeSettings.comboHitStun : 0.5f;
    public float kickStunDuration => meleeSettings != null ? meleeSettings.kickStunDuration : 1.5f;
    
    private float Gravity => meleeSettings != null ? meleeSettings.baseGravity : 20f;
    private float MaxJugglingVelocity => meleeSettings != null ? meleeSettings.maxJugglingVelocity : 15f;
    private float MaxJuggleHeight => meleeSettings != null ? meleeSettings.maxJuggleHeight : 4f;
    private float LaunchThreshold => meleeSettings != null ? meleeSettings.launchThreshold : 3.5f;
    private float JuggleFalloff => meleeSettings != null ? meleeSettings.juggleFalloffPerHit : 0.15f;
    private float JuggleMinScale => meleeSettings != null ? meleeSettings.juggleMinScale : 0.30f;
    private int MaxJuggleCount => meleeSettings != null ? meleeSettings.maxJuggleCount : 10;
    private float JuggleHoverDuration => meleeSettings != null ? meleeSettings.juggleHoverDuration : 0.5f;
    private float JuggleHoverDriftSpeed => meleeSettings != null ? meleeSettings.juggleHoverDriftSpeed : -0.5f;
    private float JugglePopVelocity => meleeSettings != null ? meleeSettings.jugglePopVelocity : 3.5f;
    private float JugglePopForceScale => meleeSettings != null ? meleeSettings.jugglePopForceScale : 0.4f;
    private float JugglePopMaxVelocityScale => meleeSettings != null ? meleeSettings.jugglePopMaxVelocityScale : 0.5f;
    private float JugglePopHeadroom => meleeSettings != null ? meleeSettings.jugglePopHeadroom : 0.4f;
    private float JugglePopFadeRange => meleeSettings != null ? meleeSettings.jugglePopFadeRange : 0.4f;
    private float JugglePopMinFade => meleeSettings != null ? meleeSettings.jugglePopMinFade : 0.35f;
    private float LaunchHitStopDuration => meleeSettings != null ? meleeSettings.launchHitStopDuration : 0.18f;
    private float LaunchSlowMoDuration => meleeSettings != null ? meleeSettings.launchSlowMoDuration : 0.18f;
    private float LaunchSlowMoTimeScale => meleeSettings != null ? meleeSettings.launchSlowMoTimeScale : 0.35f;
    private float LaunchImpactKnockbackThreshold => meleeSettings != null ? meleeSettings.launchImpactKnockbackThreshold : 5f;
    private float LaunchImpactCooldown => meleeSettings != null ? meleeSettings.launchImpactCooldown : 0.75f;
    private float MaxAirborneDuration => meleeSettings != null ? meleeSettings.maxAirborneDuration : 3f;
    private float KnockdownDuration => meleeSettings != null ? meleeSettings.knockdownDuration : 1.0f;
    private float StandUpDuration => meleeSettings != null ? meleeSettings.standUpDuration : 1.0f;
    private bool InvulnerableWhileDowned => meleeSettings == null || meleeSettings.invulnerableWhileDowned;
    private const float StandUpSafetyBuffer = 1.5f;
    private float GroundCheckDistance => meleeSettings != null ? meleeSettings.groundCheckDistance : 0.2f;
    private LayerMask GroundLayer => meleeSettings != null ? meleeSettings.groundLayer : LayerMask.GetMask("Default");
    private float FallWobbleSpeed => meleeSettings != null ? meleeSettings.fallWobbleSpeed : 4f;
    private float FallWobbleIntensity => meleeSettings != null ? meleeSettings.fallWobbleIntensity : 0.02f;
    private float FallHoldPoint => meleeSettings != null ? meleeSettings.fallAnimationHoldPoint : 0.2f;
    private float DespawnDelay => meleeSettings != null ? meleeSettings.despawnDelay : 3.0f;

    private void Start()
    {
        CharacterHighlightLayer.Ensure(gameObject);

        _combat = GetComponent<EnemyCombat>();
        TryGetComponent(out _obstacleAvoidance);

        controller = GetComponent<CharacterController>();
        _health = GetComponent<Health>();
        _combat = GetComponent<EnemyCombat>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
            player = playerObj.transform; 
        else
            Debug.LogWarning("[EnemyMovement] No GameObject with tag 'Player' found.");

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        _groundYPosition = transform.position.y;

        if (rangedSettings == null)
        {
            meleeSettings = SettingsResolver.ResolveEnemySettings(meleeSettings);
        }
        
        if (meleeSettings != null)
        {
            rangedSettings = null;
        }

        _juggleCeiling = MaxJuggleHeight;

        if (meleeSettings == null && rangedSettings == null)
            Debug.LogWarning("[EnemyMovement] No enemy settings found (provider/inspector/resources).");

        if (rangedSettings == null)
        {
            PickNewTactic();
        }

        if (TryGetComponent<Health>(out var health))
        {
            int settingsMaxHealth = rangedSettings != null ? rangedSettings.maxHealth
                : meleeSettings != null ? meleeSettings.maxHealth
                : health.Max;
            health.SetMaxHealth(settingsMaxHealth);

            health.OnHit += OnHit;
        }



        if (rangedSettings != null)
        {
            PickNewRangedTarget();
            rangedShootTimer = Random.Range(rangedSettings.rangedShootCooldownMin, rangedSettings.rangedShootCooldownMax);
        }
    }
    
    private void OnDestroy()
    {
        if (TryGetComponent<Health>(out var health))
        {
            health.OnHit -= OnHit;
        }
    }
    
    public void SetState(EnemyState newState)
    {
        if (CurrentState == newState) return;
        
        EnemyState previousState = CurrentState;
        CurrentState = newState;
        _stateTimer = 0f;

        if (previousState == EnemyState.Grabbed && newState != EnemyState.Grabbed)
        {
            StopGrabbedFallAnimation(true);
        }

        Debug.Log($"[EnemyMovement] {gameObject.name} State: {previousState} -> {newState}");

        bool couldActBefore = previousState == EnemyState.Grounded || previousState == EnemyState.HitStun;
        bool canActNow = newState == EnemyState.Grounded || newState == EnemyState.HitStun;
        if (couldActBefore && !canActNow)
        {
            _combat?.CancelAttackWindup();
        }
        
        if (animator != null) animator.speed = 1f;

        bool cameFromAirOrDown = previousState == EnemyState.Launched ||
                                 previousState == EnemyState.Airborne ||
                                 previousState == EnemyState.Grabbed ||
                                 previousState == EnemyState.Knockdown ||
                                 previousState == EnemyState.StandingUp;

        switch (newState)
        {
            case EnemyState.Grounded:
                _isBeingThrown = false;
                ResetJuggleState();
                UpdateAnimatorFalling(false);
                if (cameFromAirOrDown) ForceExitFallPose("Idle");
                break;

            case EnemyState.HitStun:
                UpdateAnimatorFalling(false);
                if (cameFromAirOrDown) ForceExitFallPose("Hit");
                break;

            case EnemyState.Launched:
            case EnemyState.Airborne:
                if (previousState != EnemyState.Launched && previousState != EnemyState.Airborne)
                    _airTimer = 0f;
                if (animator != null)
                {
                    animator.ResetTrigger("Hit");
                    animator.ResetTrigger("StandUp");
                }
                UpdateAnimatorFalling(true);
                break;

            case EnemyState.Knockdown:
                _isBeingThrown = false;
                ResetJuggleState();
                _stateTimer = KnockdownDuration;
                if (animator != null) animator.ResetTrigger("StandUp");
                break;

            case EnemyState.StandingUp:
                _stateTimer = StandUpDuration + StandUpSafetyBuffer;
                if (animator != null)
                {
                    UpdateAnimatorFalling(false);
                    animator.SetTrigger("StandUp");
                }
                break;

            case EnemyState.Grabbed:
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f);
                    StartGrabbedFallAnimation();
                }
                break;

            case EnemyState.Dead:
                if (animator != null)
                {
                    bool alreadyDown = previousState == EnemyState.Knockdown ||
                                       previousState == EnemyState.StandingUp;

                    if (alreadyDown)
                    {
                        animator.SetBool("IsFalling", true);
                        animator.Play("Fall", 0, 0.99f);
                        animator.speed = 0f;
                    }
                    else if (!animator.GetBool("IsFalling"))
                    {
                        animator.SetBool("IsFalling", true);
                        animator.Play("Fall", 0, 0f);
                    }
                }
                _stateTimer = DespawnDelay;
                break;
        }
    }

    private void UpdateAnimatorFalling(bool isFalling)
    {
        if (animator == null) return;
        
        bool wasFalling = animator.GetBool("IsFalling");
        
        Debug.Log($"[EnemyMovement] {gameObject.name} UpdateAnimatorFalling({isFalling}) - wasFalling={wasFalling}, _wasThrownSkipReset={_wasThrownSkipReset}, State={CurrentState}");
        
        if (wasFalling != isFalling)
        {
            animator.SetBool("IsFalling", isFalling);
            
            if (isFalling && !_wasThrownSkipReset)
            {
                Debug.Log($"[EnemyMovement] {gameObject.name} RESTARTING FALL ANIMATION from UpdateAnimatorFalling");
                animator.Play("Fall", 0, 0f);
            }
        }
    }
    
    private void ForceExitFallPose(string targetState)
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.ResetTrigger("StandUp");
        animator.SetBool("IsFalling", false);
        animator.Play(targetState, 0, 0f);
    }
    
    private bool IsInOrEnteringFall()
    {
        if (animator == null) return false;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Fall")) return true;
        return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Fall");
    }
    
    private void CatchStrandedFallPose(string targetState)
    {
        if (!IsInOrEnteringFall()) return;

        Debug.LogWarning($"[EnemyMovement] {gameObject.name} stranded in Fall pose while {CurrentState} " +
                         $"- forcing -> {targetState}");
        ForceExitFallPose(targetState);
    }

    private void ResetJuggleState()
    {
        _juggleCount = 0;
        _juggleDecayMultiplier = 1f;
        _juggleCeiling = MaxJuggleHeight;
        _airTimer = 0f;
        _hoverTimer = 0f;
        _hoverPending = false;
        _squashTimer = 0f;
        hitStunTimer = 0f;
        _wasThrownSkipReset = false;
        if (characterModel != null)
            characterModel.localScale = Vector3.one;
    }
    
    private void TrackGroundHeight()
    {
        if (IsGroundedRaycast()) _groundYPosition = transform.position.y;
    }

    private bool IsGroundedRaycast()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, GroundCheckDistance + 0.1f, GroundLayer) 
               || controller.isGrounded;
    }
    
    public void StartThrowAnimation()
    {
        _isBeingThrown = true;
        _wasThrownSkipReset = true;
        StopGrabbedFallAnimation(true);
        
        UpdateAnimatorFalling(true);
        _wobblePhase = 0f;
        
        Debug.Log($"[EnemyMovement] {gameObject.name} StartThrowAnimation called - Fall animation activated");
    }
    
    public void EndThrowAnimation()
    {
        _isBeingThrown = false;
        Debug.Log($"[EnemyMovement] {gameObject.name} EndThrowAnimation called - Physics resumed");
    }

    private void StartGrabbedFallAnimation()
    {
        if (animator == null) return;

        StopGrabbedFallAnimation(false);
        _grabbedFallCoroutine = StartCoroutine(PlayAndPauseGrabbedFall());
    }

    private void StopGrabbedFallAnimation(bool resumeAnimator)
    {
        if (_grabbedFallCoroutine != null)
        {
            StopCoroutine(_grabbedFallCoroutine);
            _grabbedFallCoroutine = null;
        }

        if (animator != null && resumeAnimator)
        {
            animator.speed = 1f;
        }
    }

    private IEnumerator PlayAndPauseGrabbedFall()
    {
        if (animator == null) yield break;

        float pausePoint = Mathf.Clamp01(FallHoldPoint);
        float playbackSpeed = Mathf.Max(0.01f, grabbedFallPlaybackSpeed);

        animator.SetBool("IsFalling", true);
        animator.Play("Fall", 0, 0f);
        animator.speed = playbackSpeed;

        yield return null;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        while (CurrentState == EnemyState.Grabbed && !state.IsName("Fall"))
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        while (CurrentState == EnemyState.Grabbed && state.IsName("Fall") && state.normalizedTime < pausePoint)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        if (CurrentState == EnemyState.Grabbed && animator != null)
        {
            animator.speed = 0f;
        }

        _grabbedFallCoroutine = null;
    }
    

    public void ApplyImpulse(float knockUpForce, Vector3 knockbackForce, float hitStunReset = 0f, bool forceKnockdown = false)
    {
        HitData data = new HitData
        {
            KnockUpForce = knockUpForce,
            KnockbackDirection = knockbackForce.normalized,
            KnockbackForce = knockbackForce.magnitude,
            HitStunDuration = hitStunReset,
            ShouldKnockdown = forceKnockdown,
            Damage = 0,
            JuggleType = forceKnockdown ? JuggleType.Spike : JuggleType.Juggle,
            IsLauncher = knockUpForce > LaunchThreshold && forceKnockdown == false
        };
        OnHit(data);
    }

    private void OnHit(HitData hitData)
    {
        if (CurrentState == EnemyState.Knockdown || CurrentState == EnemyState.StandingUp || CurrentState == EnemyState.Dead) return;
        
        bool isBeingThrown = CurrentState == EnemyState.Grabbed || IsInThrowState || _isBeingThrown;
        
        bool suppressHit = isBeingThrown || hitData.SuppressHitAnimation;
        if (animator != null && !suppressHit && (CurrentState == EnemyState.Grounded || CurrentState == EnemyState.HitStun))
        {
            animator.SetTrigger("Hit");
        }

        bool wasAirborne = CurrentState == EnemyState.Launched || CurrentState == EnemyState.Airborne || isBeingThrown;
        bool isGrounded = IsGroundedRaycast();
        
        float effectiveKnockUp = CalculateJuggleForce(hitData);

        bool sentFlying = false;

        if (hitData.ShouldKnockdown || _juggleCount >= MaxJuggleCount)
        {
            HandleKnockdownHit(hitData, effectiveKnockUp);
            sentFlying = !wasAirborne;
        }
        else if (wasAirborne)
        {
            HandleJuggleHit(hitData, effectiveKnockUp);
        }
        else if (hitData.IsLauncher || hitData.JuggleType == JuggleType.Launcher || effectiveKnockUp > LaunchThreshold)
        {
            HandleLauncherHit(hitData, effectiveKnockUp, isGrounded);
            sentFlying = true;
        }
        else
        {
            HandleGroundHit(hitData);
            sentFlying = LaunchImpactKnockbackThreshold > 0f &&
                         hitData.KnockbackForce >= LaunchImpactKnockbackThreshold;
        }

        if (sentFlying)
        {
            PlayLaunchImpact();
        }

        if (hitData.KnockbackForce > 0f && hitData.KnockbackDirection != Vector3.zero)
        {
            externalForce = hitData.KnockbackDirection * hitData.KnockbackForce;
        }
        
        if (hitData.HitStunDuration > 0f)
        {
            hitStunTimer = Mathf.Max(hitStunTimer, hitData.HitStunDuration);
        }
    }
    
    private void PlayLaunchImpact()
    {
        if (HitStop.Instance == null) return;

        float freeze = LaunchHitStopDuration;
        float slowMo = LaunchSlowMoDuration;
        if (freeze <= 0f && slowMo <= 0f) return;

        if (Time.unscaledTime - _lastLaunchImpactTime < LaunchImpactCooldown) return;
        _lastLaunchImpactTime = Time.unscaledTime;

        if (freeze > 0f) HitStop.Instance.Do(freeze);
        if (slowMo > 0f) HitStop.Instance.DoSlowMotion(slowMo, LaunchSlowMoTimeScale);

        Debug.Log($"[EnemyMovement] {gameObject.name} LAUNCH IMPACT - freeze {freeze:F3}s, slow mo {slowMo:F3}s @ {LaunchSlowMoTimeScale:F2}x");
    }

    private float CalculateJuggleForce(HitData hitData)
    {
        if (hitData.IgnoreJuggleDecay) 
            return hitData.KnockUpForce;
            
        float decay = 1f - (JuggleFalloff * _juggleCount);
        decay = Mathf.Max(decay, JuggleMinScale);
        
        return hitData.KnockUpForce * decay;
    }
    
    private void HandleLauncherHit(HitData hitData, float effectiveForce, bool wasGrounded)
    {
        _juggleCount = 1;
        _juggleDecayMultiplier = 1f;
        _juggleCeiling = MaxJuggleHeight;

        float maxVelocityForHeight = Mathf.Sqrt(2f * Gravity * MaxJuggleHeight);
        verticalVelocity = Mathf.Min(effectiveForce, Mathf.Min(MaxJugglingVelocity, maxVelocityForHeight));
        _hoverPending = true;
        SetState(EnemyState.Launched);

        Debug.Log($"[EnemyMovement] {gameObject.name} LAUNCHED! Force: {effectiveForce:F2}, Velocity: {verticalVelocity:F2}, MaxHeight: {MaxJuggleHeight:F2}");
    }
    
    private void HandleJuggleHit(HitData hitData, float effectiveForce)
    {
        _juggleCount++;

        _juggleCeiling = MaxJuggleHeight + JugglePopHeadroom;

        float currentHeight = transform.position.y - _groundYPosition;
        float remainingHeight = _juggleCeiling - currentHeight;

        float bounceForce = Mathf.Max(effectiveForce * JugglePopForceScale, JugglePopVelocity);
        bounceForce = Mathf.Min(bounceForce, MaxJugglingVelocity * JugglePopMaxVelocityScale);

        float fade = JugglePopFadeRange > 0f
            ? Mathf.Clamp01(remainingHeight / JugglePopFadeRange)
            : 1f;
        bounceForce *= Mathf.Max(fade, JugglePopMinFade);

        verticalVelocity = bounceForce;
        _hoverTimer = 0f;
        _hoverPending = true;
        _squashTimer = SquashDuration;

        SetState(EnemyState.Launched);

        Debug.Log($"[EnemyMovement] {gameObject.name} JUGGLED! Count: {_juggleCount}, Height: {currentHeight:F2}/{MaxJuggleHeight:F2}, Bounce: {verticalVelocity:F2}, Hover: {JuggleHoverDuration:F2}s");
    }
    
    private void HandleKnockdownHit(HitData hitData, float effectiveForce)
    {
        _hoverTimer = 0f;
        _hoverPending = false;

        if (hitData.JuggleType == JuggleType.Spike)
        {
            verticalVelocity = -effectiveForce;
        }
        else
        {
            float maxVelocityForHeight = Mathf.Sqrt(2f * Gravity * MaxJuggleHeight);
            verticalVelocity = Mathf.Min(effectiveForce, maxVelocityForHeight);
        }
        
        SetState(EnemyState.Airborne);
        _juggleCount = MaxJuggleCount;
        
        Debug.Log($"[EnemyMovement] {gameObject.name} KNOCKDOWN incoming!");
    }
    
    private void HandleGroundHit(HitData hitData)
    {
        verticalVelocity = -0.5f;
        SetState(EnemyState.HitStun);
        
        if (hitData.KnockbackForce > moveSpeed * 1.5f)
        {
            hitStunTimer = Mathf.Max(hitStunTimer, kickStunDuration);
        }
    }

    public void ForceRecalculateTactic()
    {
        tacticTimer = 0f;
    }

    public void SetAttackWindupActive(bool active)
    {
        _isAttackWindingUp = active;
    }

    private void Update()
    {
        if (hitStunTimer > 0f) hitStunTimer -= Time.deltaTime;
        if (_stateTimer > 0f) _stateTimer -= Time.deltaTime;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;
            player = playerObj.transform;
        }

        if (_health != null && _health.Current <= 0 && CurrentState != EnemyState.Dead)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            SetState(EnemyState.Dead);
        }

        switch (CurrentState)
        {
            case EnemyState.Grounded:
                UpdateGroundedState();
                break;
                
            case EnemyState.HitStun:
                UpdateHitStunState();
                break;
                
            case EnemyState.Launched:
                UpdateLaunchedState();
                break;
                
            case EnemyState.Airborne:
                UpdateAirborneState();
                break;
                
            case EnemyState.Knockdown:
                UpdateKnockdownState();
                break;
                
            case EnemyState.StandingUp:
                UpdateStandingUpState();
                break;
                
            case EnemyState.Grabbed:
                if (animator != null) animator.SetFloat("Speed", 0f);
                break;

            case EnemyState.Dead:
                ApplyGravityAndMove(Vector3.zero);
                break;
        }

        UpdateSquashAndStretch();
    }

    private void UpdateSquashAndStretch()
    {
        if (characterModel == null) return;

        if (_squashTimer > 0f)
        {
            _squashTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_squashTimer / SquashDuration);
            characterModel.localScale = Vector3.Lerp(Vector3.one, SquashScale, t);
        }
        else if (characterModel.localScale != Vector3.one)
        {
            characterModel.localScale = Vector3.one;
        }
    }

    #region State Updates
    
    private void UpdateGroundedState()
    {
        CatchStrandedFallPose("Idle");
        TrackGroundHeight();

        if (rangedSettings != null)
        {
            MoveRanged();
        }
        else
        {
            tacticTimer -= Time.deltaTime;

            if (!isFlanking && BeatEmUpDirector.Instance != null && !BeatEmUpDirector.Instance.HasToken(_combat))
            {
                ForceRecalculateTactic();
            }

            if (tacticTimer <= 0f)
            {
                PickNewTactic();
            }

            MoveBasedOnTactic();
        }

        LookAtPlayer();
    }
    
    private void UpdateHitStunState()
    {
        CatchStrandedFallPose("Hit");
        TrackGroundHeight();

        ApplyGravityAndMove(Vector3.zero);
        if (animator != null) animator.SetFloat("Speed", 0f);
        
        if (hitStunTimer <= 0f)
        {
            SetState(EnemyState.Grounded);
        }
    }
    
    private void UpdateLaunchedState()
    {
        if (TickAirWatchdog()) return;

        if (!_isBeingThrown)
        {
            float ceiling = _juggleCeiling > 0f ? _juggleCeiling : MaxJuggleHeight;

            float currentHeight = transform.position.y - _groundYPosition;
            if (currentHeight >= ceiling && verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }

            if (_hoverPending && verticalVelocity <= 0f)
            {
                _hoverPending = false;
                _hoverTimer = JuggleHoverDuration;
                Debug.Log($"[EnemyMovement] {gameObject.name} HOVER start at apex - Height: {currentHeight:F2}/{ceiling:F2}, Duration: {JuggleHoverDuration:F2}s");
            }

            TickHoverAndMove();
        }
        UpdateJugglingAnimation();

        if (!_isBeingThrown && _hoverTimer <= 0f && verticalVelocity <= 0f)
        {
            SetState(EnemyState.Airborne);
        }
    }

    private void UpdateAirborneState()
    {
        if (TickAirWatchdog()) return;

        if (!_isBeingThrown)
        {
            TickHoverAndMove();
        }
        UpdateJugglingAnimation();

        if (!_isBeingThrown && _hoverTimer <= 0f && IsGroundedRaycast() && verticalVelocity <= 0f)
        {
            OnLanded();
        }
    }

    private void TickHoverAndMove()
    {
        if (_hoverTimer > 0f) _hoverTimer -= Time.deltaTime;

        if (_hoverTimer > 0f && verticalVelocity <= 0f)
        {
            ApplyHoverMove();
        }
        else
        {
            ApplyGravityAndMove(Vector3.zero);
        }
    }
    
    private bool TickAirWatchdog()
    {
        if (_isBeingThrown)
        {
            _airTimer = 0f;
            return false;
        }

        _airTimer += Time.deltaTime;

        if (_airTimer < MaxAirborneDuration) return false;

        Debug.LogWarning($"[EnemyMovement] {gameObject.name} air-state watchdog fired after {_airTimer:F2}s " +
                         $"(state={CurrentState}, vVel={verticalVelocity:F2}, hover={_hoverTimer:F2}, " +
                         $"ccEnabled={(controller != null && controller.enabled)}) - forcing landing");

        ForceLand();
        return true;
    }

    private void ForceLand()
    {
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
        }

        _hoverTimer = 0f;
        verticalVelocity = 0f;
        OnLanded();
    }

    private void UpdateJugglingAnimation()
    {
        if (animator == null || CurrentState == EnemyState.Dead) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isFallAnimation = stateInfo.IsName("Fall"); 

        if (isFallAnimation)
        {
            if (!IsGroundedRaycast())
            {
                if (_wasThrownSkipReset)
                {
                    animator.speed = 1f;
                    return;
                }

                if (_hoverTimer > 0f && verticalVelocity <= 0f)
                {
                    if (stateInfo.normalizedTime >= FallHoldPoint)
                    {
                        animator.Play("Fall", 0, FallHoldPoint);
                        animator.speed = 0f;
                    }
                    return;
                }

                if (stateInfo.normalizedTime >= FallHoldPoint)
                {
                    _wobblePhase += Time.deltaTime * FallWobbleSpeed;

                    float wobbleSpeed = Mathf.Cos(_wobblePhase) * FallWobbleIntensity;

                    float drift = stateInfo.normalizedTime - FallHoldPoint;

                    if (drift > 0.05f)
                    {
                        Debug.Log($"[EnemyMovement] {gameObject.name} RESETTING FALL to HoldPoint in UpdateJugglingAnimation - drift={drift:F3}");
                        animator.Play("Fall", 0, FallHoldPoint);
                        animator.speed = 0f;
                        _wobblePhase = 0f;
                    }
                    else
                    {
                        wobbleSpeed -= drift * 5.0f;
                        animator.speed = Mathf.Max(wobbleSpeed, 0f);
                    }
                }
                else
                {
                    animator.speed = 1f;
                    _wobblePhase = 0f;
                }
            }
            else
            {
                animator.speed = 1f;
            }
        }
        else
        {
            animator.speed = 1f;
            if (animator.GetFloat("Speed") != 0) animator.SetFloat("Speed", 0f);
        }
    }
    
    private void UpdateKnockdownState()
    {
        ApplyGravityAndMove(Vector3.zero);
    
        if (animator != null) 
        {
            animator.SetFloat("Speed", 0f);
        
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
            if (stateInfo.IsName("Fall"))
            {
                if (stateInfo.normalizedTime >= 0.95f)
                {
                    animator.Play("Fall", 0, 0.99f);
                    animator.speed = 0f;
                }
            }
        }
    
        if (_stateTimer <= 0f)
        {
            SetState(EnemyState.StandingUp);
        }
    }
    
    private void UpdateStandingUpState()
    {
        ApplyGravityAndMove(Vector3.zero);
        if (animator != null) animator.SetFloat("Speed", 0f);

        bool standUpAnimationDone = animator != null &&
            animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") &&
            !animator.IsInTransition(0);

        if (standUpAnimationDone || _stateTimer <= 0f)
        {
            SetState(EnemyState.Grounded);
            Debug.Log($"[EnemyMovement] {gameObject.name} finished standing up.");
        }
    }
    
    private void OnLanded()
    {
        Debug.Log($"[EnemyMovement] {gameObject.name} LANDED! JuggleCount: {_juggleCount}, _wasThrownSkipReset={_wasThrownSkipReset}");
        
        _groundYPosition = transform.position.y;
        
        if (animator != null)
        {
            if (_wasThrownSkipReset)
            {
                animator.Play("Fall", 0, 0.99f);
                animator.speed = 0f;
                Debug.Log($"[EnemyMovement] {gameObject.name} Throw landing - snapping to end of Fall animation");
            }
            else
            {
                animator.speed = 1f;
            }
        }

        SetState(EnemyState.Knockdown);
        verticalVelocity = -0.5f;
    }
    
    #endregion

    private void PickNewTactic()
    {
        tacticTimer = Random.Range(minRepositionTime, maxRepositionTime);

        if (_combat != null && BeatEmUpDirector.Instance != null)
        {
            if (_combat.IsReadyToAttack)
            {
                isFlanking = !BeatEmUpDirector.Instance.RequestAttackToken(_combat);
            }
            else
            {
                isFlanking = true;
                BeatEmUpDirector.Instance.ReleaseToken(_combat);
            }
        }
        else
        {
            isFlanking = Random.value > 0.5f;
        }
    }

    private void MoveBasedOnTactic()
    {

        Vector3 targetPosition;

        if (isFlanking)
        {
            if (BeatEmUpDirector.Instance != null && _combat != null)
            {
                targetPosition = BeatEmUpDirector.Instance.GetFlankSlotPosition(_combat);
            }
            else
            {
                targetPosition = player.position + Vector3.right * 4f; 
            }
        }
        else
        {
            float dirX = (transform.position.x >= player.position.x) ? 1f : -1f;
            
            targetPosition = player.position + new Vector3(dirX * attackDistance, 0f, 0f);
        }

        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0f;
        float distance = directionToTarget.magnitude;

        Vector3 moveVelocity = Vector3.zero;
        float normalizedSpeed = 0f;
        float windupSpeedMultiplier = (_isAttackWindingUp && FreezeDuringTelegraph) ? TelegraphMoveSpeedMultiplier : 1f;

        if (distance > 0.1f && windupSpeedMultiplier > 0f)
        {
            Vector3 desiredDirection = directionToTarget.normalized;
            Vector3 avoidance = CalculateAvoidance();

            Vector3 steered = (desiredDirection + avoidance).normalized;

            Vector3 finalDirection = _obstacleAvoidance != null ? _obstacleAvoidance.Adjust(steered) : steered;
            moveVelocity = finalDirection * moveSpeed * windupSpeedMultiplier;
            normalizedSpeed = windupSpeedMultiplier;
        }

        ApplyGravityAndMove(moveVelocity);
        if (animator != null)
        {
            animator.SetFloat("Speed", normalizedSpeed);
        }
    }

    private void MoveRanged()
    {
        float distanceToPlayerXZ = Vector3.Distance(
        new Vector3(transform.position.x, 0f, transform.position.z),
        new Vector3(player.position.x, 0f, player.position.z));

        if (distanceToPlayerXZ <= rangedMeleeFallbackDistance)
        {
            ApplyGravityAndMove(Vector3.zero);
            if (animator != null) animator.SetFloat("Speed", 0f);
            isAiming = false;
            return;
        }

        if (rangedRecoveryTimer > 0f)
        {
            rangedRecoveryTimer -= Time.deltaTime;

            ApplyGravityAndMove(Vector3.zero);

            if (animator != null)
                animator.SetFloat("Speed", 0f);

            return;
        }

        if (rangedDodgeTimer > 0f)
        {
            rangedDodgeTimer -= Time.deltaTime;

            Vector3 dodgeMove = new Vector3(0f, 0f, rangedDodgeDirection) * moveSpeed;

            ApplyGravityAndMove(dodgeMove);

            if (animator != null)
                animator.SetFloat("Speed", 1f);

            if (rangedDodgeTimer <= 0f)
            {
                PickNewRangedTarget();
            }

            return;
        }

        rangedRepositionTimer -= Time.deltaTime;

        if (rangedRepositionTimer <= 0f)
        {
            PickNewRangedTarget();
        }

        Vector3 pos = transform.position;

        Vector3 direction = rangedTargetPosition - pos;
        direction.y = 0f;

        Vector3 moveVelocity = Vector3.zero;

        if (direction.magnitude > 0.2f)
        {
            moveVelocity = direction.normalized * moveSpeed;
        }

        ApplyGravityAndMove(moveVelocity);

        if (animator != null)
        {
            animator.SetFloat("Speed", moveVelocity.sqrMagnitude > 0f ? 1f : 0f);
        }

        float zDistance = Mathf.Abs(player.position.z - transform.position.z);
        bool isAlignedOnZ = zDistance <= 0.3f;

        if (isAlignedOnZ)
        {
            if (!isAiming)
            {
                isAiming = true;
                rangedAimTimer = Random.Range(1f, 2f);
            }

            rangedAimTimer -= Time.deltaTime;

            if (rangedAimTimer <= 0f)
            {
                if (_combat != null)
                {
                    _combat.TryRangedAttack(rangedSettings);
                }

                isAiming = false;
                rangedAimTimer = 0f;

                rangedRecoveryTimer = 1f;

                rangedDodgeTimer = Random.Range(0.8f, 1.0f);
                rangedDodgeDirection = Random.value < 0.5f ? -1f : 1f;
            }
        }
        else
        {
            isAiming = false;
        }
    }



    private void PickNewRangedTarget()
    {
        rangedSide = transform.position.x < player.position.x ? -1 : 1;

        float targetDistance =
            (rangedSettings.rangedMinDistance + rangedSettings.rangedMaxDistance) * 0.5f;

        rangedTargetPosition = new Vector3(
            player.position.x + rangedSide * targetDistance,
            transform.position.y,
            player.position.z
        );

        rangedRepositionTimer = Random.Range(0.8f, 1.5f);
    }


    private Vector3 CalculateAvoidance()
    {
        Vector3 avoidance = Vector3.zero;
        
        Collider[] nearby = Physics.OverlapSphere(transform.position, 1.5f);
        
        foreach (var col in nearby)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                float zDiff = transform.position.z - col.transform.position.z;
                
                if (Mathf.Abs(zDiff) < 0.1f)
                {
                    zDiff = (gameObject.GetInstanceID() > col.gameObject.GetInstanceID()) ? 1f : -1f;
                }
                
                avoidance.z += Mathf.Sign(zDiff) * 1.5f; 
            }
        }
        
        return avoidance;
    }

    private void ApplyHoverMove()
    {
        verticalVelocity = JuggleHoverDriftSpeed;

        if (controller == null || !controller.enabled) return;

        Vector3 totalMovement = externalForce;
        totalMovement.y = verticalVelocity;

        controller.Move(totalMovement * Time.deltaTime);

        if (externalForce.magnitude > 0.01f)
            externalForce -= externalForce * (horizontalDrag * Time.deltaTime);
        else
            externalForce = Vector3.zero;
    }

    private void ApplyGravityAndMove(Vector3 moveVelocity)
    {
        bool grounded = controller != null && controller.enabled && controller.isGrounded;

        if (grounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            float gravityMultiplier = 1f;
            if (meleeSettings != null && meleeSettings.useGravityScaling && verticalVelocity < 0f)
            {
                gravityMultiplier = meleeSettings.fallGravityMultiplier;
            }
            verticalVelocity -= Gravity * gravityMultiplier * Time.deltaTime;
        }

        if (controller == null || !controller.enabled)
        {
            return;
        }

        Vector3 totalMovement = moveVelocity + externalForce;
        totalMovement.y = verticalVelocity;
        
        controller.Move(totalMovement * Time.deltaTime);

        if (externalForce.magnitude > 0.01f)
        {
            externalForce -= externalForce * (horizontalDrag * Time.deltaTime);
        }
        else
        {
            externalForce = Vector3.zero;
        }
    }

    private void LookAtPlayer()
    {
        if (characterModel == null) return;
        float dirX = (player.position.x >= transform.position.x) ? 1f : -1f;
        Vector3 strictDirection = new Vector3(dirX, 0f, 0f);
        
        characterModel.rotation = Quaternion.LookRotation(strictDirection);
    }
}
