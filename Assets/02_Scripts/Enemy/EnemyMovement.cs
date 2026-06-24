using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Settings")]
    public EnemySettings settings;

    [Header("Visuals")]
    public Transform characterModel;
    [SerializeField] private Animator animator;
    
    public EnemyState CurrentState { get; private set; } = EnemyState.Grounded;
    
    public bool IsStunned => CurrentState == EnemyState.HitStun || 
                             CurrentState == EnemyState.Knockdown || 
                             CurrentState == EnemyState.StandingUp;
    
    public bool CanAct => CurrentState == EnemyState.Grounded || CurrentState == EnemyState.HitStun;
    
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

    private int _juggleCount;
    private float _juggleDecayMultiplier = 1f;
    
    private bool _isBeingThrown;
    public bool IsBeingThrown => _isBeingThrown;
    
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
    
    private Health _health;
    private EnemyCombat _combat;
    
    public float moveSpeed => settings != null ? settings.moveSpeed : 3f;
    private float StopDistance => settings != null ? settings.stopDistance : 1.5f;
    public float attackDistance => settings != null ? settings.attackDistance : 1.5f;
    
    public float minRepositionTime => settings != null ? settings.minRepositionTime : 1.0f;
    public float maxRepositionTime => settings != null ? settings.maxRepositionTime : 3.0f;
    
    public float comboHitStun => settings != null ? settings.comboHitStun : 0.5f;
    public float kickStunDuration => settings != null ? settings.kickStunDuration : 1.5f;
    
    private float Gravity => settings != null ? settings.baseGravity : 20f;
    private float MaxJugglingVelocity => settings != null ? settings.maxJugglingVelocity : 15f;
    private float MaxJuggleHeight => settings != null ? settings.maxJuggleHeight : 4f;
    private float LaunchThreshold => settings != null ? settings.launchThreshold : 3.5f;
    private float JuggleFalloff => settings != null ? settings.juggleFalloffPerHit : 0.15f;
    private float JuggleMinScale => settings != null ? settings.juggleMinScale : 0.30f;
    private int MaxJuggleCount => settings != null ? settings.maxJuggleCount : 10;
    private float KnockdownDuration => settings != null ? settings.knockdownDuration : 1.0f;
    private float StandUpDuration => settings != null ? settings.standUpDuration : 1.0f;
    private float GroundCheckDistance => settings != null ? settings.groundCheckDistance : 0.2f;
    private LayerMask GroundLayer => settings != null ? settings.groundLayer : LayerMask.GetMask("Default");
    private float FallWobbleSpeed => settings != null ? settings.fallWobbleSpeed : 4f;
    private float FallWobbleIntensity => settings != null ? settings.fallWobbleIntensity : 0.02f;
    private float FallHoldPoint => settings != null ? settings.fallAnimationHoldPoint : 0.2f;
    private float DespawnDelay => settings != null ? settings.despawnDelay : 3.0f;

    private void Start()
    {
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
            
        if (settings == null)
        {
            settings = Resources.Load<EnemySettings>("EnemySettings");
            if (settings == null)
                Debug.LogWarning("[EnemyMovement] No EnemySettings assigned or found in Resources folder.");
        }

        PickNewTactic();

        if (TryGetComponent<Health>(out var health))
        {
            health.OnHit += OnHit;
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
        
        Debug.Log($"[EnemyMovement] {gameObject.name} State: {previousState} -> {newState}");
        
        switch (newState)
        {
            case EnemyState.Grounded:
                _isBeingThrown = false; 
                ResetJuggleState();
                UpdateAnimatorFalling(false);
                break;
                
            case EnemyState.HitStun:
                if (IsGroundedRaycast()) UpdateAnimatorFalling(false);
                break;

            case EnemyState.Launched:
            case EnemyState.Airborne:
                UpdateAnimatorFalling(true);
                break;
                
            case EnemyState.Knockdown:
                _isBeingThrown = false;
                _stateTimer = KnockdownDuration; 
                
                if (animator != null)
                {
                    animator.Play("Fall", 0, 1f); 
                    animator.speed = 0f; 
                }
                break;
                
            case EnemyState.StandingUp:
                _stateTimer = StandUpDuration;
                if (animator != null)
                {
                    animator.speed = 1f; 
                    UpdateAnimatorFalling(false); 
                    animator.SetTrigger("StandUp");
                }
                break;

            case EnemyState.Grabbed:
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f);
                }
                break;

            case EnemyState.Dead:
                if (!controller.isGrounded)
                {
                    UpdateAnimatorFalling(true);
                }
                else
                {
                    UpdateAnimatorFalling(false);
                }
                _stateTimer = DespawnDelay;
                break;
        }
    }

    private void UpdateAnimatorFalling(bool isFalling)
    {
        if (animator == null) return;
        
        bool wasFalling = animator.GetBool("IsFalling");
        
        if (wasFalling != isFalling)
        {
            animator.SetBool("IsFalling", isFalling);
            
            if (isFalling)
            {
                animator.Play("Fall", 0, 0f);
            }
        }
    }
    
    private void ResetJuggleState()
    {
        _juggleCount = 0;
        _juggleDecayMultiplier = 1f;
        hitStunTimer = 0f;
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
        
        UpdateAnimatorFalling(true);
        _wobblePhase = 0f;
        
        Debug.Log($"[EnemyMovement] {gameObject.name} StartThrowAnimation called - Fall animation activated");
    }
    
    public void EndThrowAnimation()
    {
        _isBeingThrown = false;
        Debug.Log($"[EnemyMovement] {gameObject.name} EndThrowAnimation called - Physics resumed");
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
        if (CurrentState == EnemyState.StandingUp || CurrentState == EnemyState.Dead) return;
        
        bool isBeingThrown = CurrentState == EnemyState.Grabbed || IsInThrowState || _isBeingThrown;
        
        bool suppressHit = isBeingThrown || hitData.SuppressHitAnimation;
        if (animator != null && !suppressHit && (CurrentState == EnemyState.Grounded || CurrentState == EnemyState.HitStun))
        {
            animator.SetTrigger("Hit");
        }
        
        if (CurrentState == EnemyState.Knockdown)
        {
            StopAllCoroutines();
        }

        bool wasAirborne = CurrentState == EnemyState.Launched || CurrentState == EnemyState.Airborne || isBeingThrown;
        bool isGrounded = IsGroundedRaycast();
        
        float effectiveKnockUp = CalculateJuggleForce(hitData);
        
        if (hitData.ShouldKnockdown || _juggleCount >= MaxJuggleCount)
        {
            HandleKnockdownHit(hitData, effectiveKnockUp);
        }
        else if (hitData.IsLauncher || hitData.JuggleType == JuggleType.Launcher)
        {
            HandleLauncherHit(hitData, effectiveKnockUp, isGrounded);
        }
        else if (wasAirborne || !isGrounded)
        {
            HandleJuggleHit(hitData, effectiveKnockUp);
        }
        else if (effectiveKnockUp > LaunchThreshold)
        {
            HandleLauncherHit(hitData, effectiveKnockUp, isGrounded);
        }
        else
        {
            HandleGroundHit(hitData);
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
        
        verticalVelocity = Mathf.Min(effectiveForce, MaxJugglingVelocity);
        SetState(EnemyState.Launched);
        
        Debug.Log($"[EnemyMovement] {gameObject.name} LAUNCHED! Force: {effectiveForce:F2}, Velocity: {verticalVelocity:F2}");
    }
    
    private void HandleJuggleHit(HitData hitData, float effectiveForce)
    {
        _juggleCount++;
        
        float currentHeight = transform.position.y - _groundYPosition;
        float heightRatio = Mathf.Clamp01(currentHeight / MaxJuggleHeight);
        
        float heightScale = Mathf.Lerp(1f, 0.2f, heightRatio);
        float scaledForce = effectiveForce * heightScale;
        
        float velocityBonus = verticalVelocity > 0 ? verticalVelocity * 0.2f : 0f;
        float targetVelocity = scaledForce + velocityBonus;
        verticalVelocity = Mathf.Min(targetVelocity, MaxJugglingVelocity);
        
        if (verticalVelocity > 0)
            SetState(EnemyState.Launched);
        else
            SetState(EnemyState.Airborne);
        
        Debug.Log($"[EnemyMovement] {gameObject.name} JUGGLED! Count: {_juggleCount}, Height: {currentHeight:F2}/{MaxJuggleHeight:F2}, Velocity: {verticalVelocity:F2}");
    }
    
    private void HandleKnockdownHit(HitData hitData, float effectiveForce)
    {
        if (hitData.JuggleType == JuggleType.Spike)
        {
            verticalVelocity = -effectiveForce; 
        }
        else
        {
            verticalVelocity = effectiveForce;
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

    private void Update()
    {
        if (player == null) return;
        if (_health != null && _health.Current <= 0 && CurrentState != EnemyState.Dead)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        if (hitStunTimer > 0f) hitStunTimer -= Time.deltaTime;
        if (_stateTimer > 0f) _stateTimer -= Time.deltaTime;
        
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
    }

    #region State Updates
    
    private void UpdateGroundedState()
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
        LookAtPlayer();
    }
    
    private void UpdateHitStunState()
    {
        ApplyGravityAndMove(Vector3.zero);
        if (animator != null) animator.SetFloat("Speed", 0f);
        
        if (hitStunTimer <= 0f)
        {
            SetState(EnemyState.Grounded);
        }
    }
    
    private void UpdateLaunchedState()
    {
        if (!_isBeingThrown)
        {
            float currentHeight = transform.position.y - _groundYPosition;
            if (currentHeight >= MaxJuggleHeight && verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }
            
            ApplyGravityAndMove(Vector3.zero);
        }
        UpdateJugglingAnimation();
        
        if (!_isBeingThrown && verticalVelocity <= 0f)
        {
            SetState(EnemyState.Airborne);
        }
    }
    
    private void UpdateAirborneState()
    {
        if (!_isBeingThrown)
        {
            ApplyGravityAndMove(Vector3.zero);
        }
        UpdateJugglingAnimation();
        
        if (!_isBeingThrown && IsGroundedRaycast() && verticalVelocity <= 0f)
        {
            OnLanded();
        }
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
                if (stateInfo.normalizedTime >= FallHoldPoint)
                {
                    _wobblePhase += Time.deltaTime * FallWobbleSpeed;
                    
                    float wobbleSpeed = Mathf.Cos(_wobblePhase) * FallWobbleIntensity;

                    float drift = stateInfo.normalizedTime - FallHoldPoint;
                    
                    if (drift > 0.05f)
                    {
                        animator.Play("Fall", 0, FallHoldPoint);
                        animator.speed = 0f;
                        _wobblePhase = 0f;
                    }
                    else
                    {
                        wobbleSpeed -= drift * 5.0f; 
                        animator.speed = wobbleSpeed;
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
            animator.speed = 0f; 
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
        
        if (_stateTimer <= 0f)
        {
            SetState(EnemyState.Grounded);
            Debug.Log($"[EnemyMovement] {gameObject.name} finished standing up.");
        }
    }
    
    private void OnLanded()
    {
        Debug.Log($"[EnemyMovement] {gameObject.name} LANDED! JuggleCount: {_juggleCount}");
        
        _groundYPosition = transform.position.y;
        
        if (animator != null)
        {
            animator.speed = 1f;
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
        if (distance > 0.1f)
        {
            Vector3 desiredDirection = directionToTarget.normalized;
            Vector3 avoidance = CalculateAvoidance();
            Vector3 finalDirection = (desiredDirection + avoidance).normalized;
            moveVelocity = finalDirection * moveSpeed; 
            normalizedSpeed = 1f;
        }
        
        ApplyGravityAndMove(moveVelocity);
        if (animator != null)
        {
            animator.SetFloat("Speed", normalizedSpeed);
        }
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

    private void ApplyGravityAndMove(Vector3 moveVelocity)
    {
        if (controller == null || !controller.enabled)
        {
            return;
        }

        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            float gravityMultiplier = 1f;
            if (settings != null && settings.useGravityScaling && verticalVelocity < 0f)
            {
                gravityMultiplier = settings.fallGravityMultiplier;
            }
            verticalVelocity -= Gravity * gravityMultiplier * Time.deltaTime;
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
