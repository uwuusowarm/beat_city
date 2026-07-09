using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerGrapple : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private PlayerSettings settings;
    [SerializeField] private Hitbox grappleHitbox;
    [FormerlySerializedAs("grappleAction")]
    [SerializeField] private InputActionReference[] throwActions;

    [Header("Visuals")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private Animator animator;
    [SerializeField] private bool showProjectileGizmos = true;

    private bool _isGrappling;
    private float _nextGrappleTime;
    private GameObject _heldTarget;
    private GameObject _currentProjectile;
    private MovementPlayer _movement;
    private PlayerCombat _combat;
    private InputBuffer _inputBuffer;
    private bool _stateSubscribed;

    private void Awake()
    {
        if (settings == null)
            settings = Resources.Load<PlayerSettings>("PlayerSettings");

        _movement = GetComponent<MovementPlayer>();
        _combat = GetComponent<PlayerCombat>();
        _inputBuffer = GetComponent<InputBuffer>();
        if (characterModel == null)
            characterModel = transform.Find("Model");
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (grappleHitbox != null)
        {
            grappleHitbox.OnHitLanded += HandleGrappleHit;
            grappleHitbox.ApplyDamage = false;
        }
        else
        {
            Debug.LogError($"[PlayerGrapple] grappleHitbox is NOT assigned on {gameObject.name}!");
        }
    }

    private void OnEnable()
    {
        foreach (var actionRef in throwActions)
        {
            if (actionRef != null && actionRef.action != null)
            {
                actionRef.action.performed += OnThrowInput;
                actionRef.action.Enable();
            }
        }
        EnsureStateSubscription();
    }

    private void OnDisable()
    {
        foreach (var actionRef in throwActions)
        {
            if (actionRef != null && actionRef.action != null)
            {
                actionRef.action.performed -= OnThrowInput;
                actionRef.action.Disable();
            }
        }
        if (_isGrappling && PlayerStateManager.Instance != null) PlayerStateManager.Instance.ResetToIdle();
        if (animator != null) animator.SetBool("IsGrabbing", false);

        if (_stateSubscribed && PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.OnStateChanged -= HandlePlayerStateChanged;
        }
        _stateSubscribed = false;
    }
    
    private void EnsureStateSubscription()
    {
        if (_stateSubscribed || PlayerStateManager.Instance == null) return;
        PlayerStateManager.Instance.OnStateChanged += HandlePlayerStateChanged;
        _stateSubscribed = true;
    }
    
    private void HandlePlayerStateChanged(PlayerState newState)
    {
        if (newState == PlayerState.Holding) return;

        if (animator != null) animator.SetBool("IsGrabbing", false);

        if (_heldTarget != null)
        {
            if (_heldTarget.TryGetComponent<CharacterController>(out var cc))
                cc.enabled = true;

            if (_heldTarget.TryGetComponent<EnemyMovement>(out var em) &&
                em.CurrentState == EnemyState.Grabbed)
            {
                em.SetState(EnemyState.Airborne);
            }

            _heldTarget = null;
        }
    }

    private void FixedUpdate()
    {
        if (PlayerStateManager.Instance == null) return;
        EnsureStateSubscription();

        if (PlayerStateManager.Instance.CurrentState == PlayerState.Idle && Time.time >= _nextGrappleTime && !_isGrappling)
        {
            if (IsEnemyInGrappleRange())
            {
                StartCoroutine(DoGrapple());
            }
        }
    }

    private bool IsEnemyInGrappleRange()
    {
        if (grappleHitbox == null) return false;

        Vector3 worldCenter = transform.TransformPoint(grappleHitbox.Offset);
        Collider[] hits = Physics.OverlapBox(worldCenter, grappleHitbox.Size * 0.5f, transform.rotation);

        foreach (var col in hits)
        {
            if (col == null) continue;
            
            if (col.TryGetComponent<Hurtbox>(out var hurtbox))
            {
                if (hurtbox.Owner == gameObject) continue;

                if (hurtbox.Owner.TryGetComponent<Health>(out var health) && health.Current > 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void OnThrowInput(InputAction.CallbackContext context)
    {
        if (PlayerStateManager.Instance.CurrentState != PlayerState.Holding) return;

        if (_heldTarget != null)
        {
            StartCoroutine(PerformThrow(_heldTarget));
        }
        else
        {
            ReleaseHold();
        }
    }

    private IEnumerator DoGrapple()
    {
        _isGrappling = true;
        PlayerStateManager.Instance.SetState(PlayerState.Grappling);
        
        
        grappleHitbox.Activate();

        if (PlayerStateManager.Instance.CurrentState == PlayerState.Grappling)
        {
            grappleHitbox.Deactivate();
            _isGrappling = false;
            PlayerStateManager.Instance.ResetToIdle();
            _nextGrappleTime = Time.time + settings.grappleCooldown;
            yield break;
        }

        yield return new WaitForSeconds(settings.grappleActiveTime);

        grappleHitbox.Deactivate();

        _isGrappling = false;

        if (PlayerStateManager.Instance.CurrentState == PlayerState.Grappling)
        {
            PlayerStateManager.Instance.ResetToIdle();
            _nextGrappleTime = Time.time + settings.grappleCooldown;
        }
    }

    private void HandleGrappleHit(GameObject hitObject)
    {
        if (PlayerStateManager.Instance.CurrentState != PlayerState.Grappling)
        {
            Debug.LogWarning($"[PlayerGrapple] HandleGrappleHit ignored because player is in state: {PlayerStateManager.Instance.CurrentState}");
            return;
        }

        GameObject target = hitObject;
        if (hitObject.TryGetComponent<Hurtbox>(out var hurtbox))
        {
            target = hurtbox.Owner;
        }

        if (target.TryGetComponent<Health>(out var health) && health.Current <= 0)
        {
            Debug.Log($"[PlayerGrapple] Target {target.name} is already dead, cannot grapple.");
            return;
        }

        _heldTarget = target;
        PlayerStateManager.Instance.SetState(PlayerState.Holding);

        if (target.TryGetComponent<EnemyMovement>(out var em))
        {
            em.SetState(EnemyState.Grabbed);
        }

        if (animator != null)
        {
            animator.SetBool("IsGrabbing", true);
        }

        StartCoroutine(HoldTarget(target));

        Debug.Log($"[PlayerGrapple] Target {target.name} caught and being held.");
    }

    private IEnumerator HoldTarget(GameObject target)
    {
        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        target.TryGetComponent<Health>(out var targetHealth);

        while (PlayerStateManager.Instance.CurrentState == PlayerState.Holding && _heldTarget == target)
        {
            if (target == null || (targetHealth != null && targetHealth.Current <= 0))
            {
                ReleaseHold();
                yield break;
            }

            Vector3 targetPos = transform.position + characterModel.forward * settings.holdOffset;
            target.transform.position = targetPos;

            target.transform.LookAt(transform.position);

            yield return null;
        }
    }

    private void ReleaseHold()
    {
        _heldTarget = null;

        if (animator != null) animator.SetBool("IsGrabbing", false);

        if (PlayerStateManager.Instance.CurrentState == PlayerState.Holding)
        {
            PlayerStateManager.Instance.ResetToIdle();
            _nextGrappleTime = Time.time + settings.grappleCooldown;
        }
    }

    private IEnumerator PerformThrow(GameObject target)
    {
        _heldTarget = null;
        PlayerStateManager.Instance.SetState(PlayerState.Grappling);

        Vector3 throwDir = characterModel.forward;
        bool isBackwardThrow = false;
        if (_movement != null)
        {
            Vector3 inputDir = _movement.GetInputDirection();
            if (inputDir.magnitude > 0.1f)
            {
                if (Vector3.Dot(characterModel.forward, inputDir) < -0.5f)
                {
                    isBackwardThrow = true;
                    throwDir = inputDir;
                    characterModel.rotation = Quaternion.LookRotation(new Vector3(throwDir.x, 0, 0));
                }
                else
                {
                    throwDir = inputDir;
                    characterModel.rotation = Quaternion.LookRotation(new Vector3(throwDir.x, 0, 0));
                }
            }
        }

        if (animator != null)
        {
            animator.SetBool("IsGrabbing", false);
            string animName = isBackwardThrow ? "Throw" : "Headbutt";
            float launchPoint = isBackwardThrow ? settings.throwLaunchPoint : settings.headbuttLaunchPoint;
            animator.SetTrigger(animName);

            if (launchPoint > 0f)
            {
                yield return null;
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                while (!state.IsName(animName))
                {
                    yield return null;
                    state = animator.GetCurrentAnimatorStateInfo(0);
                }
                while (state.IsName(animName) && state.normalizedTime < launchPoint)
                {
                    yield return null;
                    state = animator.GetCurrentAnimatorStateInfo(0);
                }
            }
        }

        yield return StartCoroutine(AnimateThrow(target, throwDir, isBackwardThrow));


        if (target != null)
        {
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(new HitData
                {
                    Damage = settings.grappleDamage,
                    KnockbackDirection = throwDir,
                    KnockbackForce = settings.impactKnockback, 
                    KnockUpForce = settings.impactKnockUp,
                    HitStunDuration = 0.5f,
                    ShouldKnockdown = true,
                    SuppressHitAnimation = true,
                    Source = gameObject
                });

                if (damageable is Health enemyHealth)
                {
                    UIManager.Instance?.UpdateEnemyHealthFocus(enemyHealth);
                }
            }
        }

        if (target != null)
        {
            var cc = target.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
            
            var em = target.GetComponent<EnemyMovement>();
            if (em != null)
            {
                em.EndThrowAnimation();
                
                if (em.CurrentState == EnemyState.Grabbed)
                {
                    em.SetState(EnemyState.Airborne);
                }
            }
        }
        
        if (_inputBuffer != null) _inputBuffer.Clear();
        PlayerStateManager.Instance.ResetToIdle();
        _nextGrappleTime = Time.time + settings.grappleCooldown;
    }

    private IEnumerator AnimateThrow(GameObject target, Vector3 throwDir, bool isBackwardThrow = false)
    {
        if (target == null) yield break;

        Vector3 startPos = target.transform.position;
        float distance = settings.throwDistance + (isBackwardThrow ? settings.backwardThrowOffset : 0f);
        Vector3 targetPos = startPos + throwDir * distance;
        
        CharacterController cc = target.GetComponent<CharacterController>();
        Rigidbody rb = target.GetComponent<Rigidbody>();
        EnemyMovement em = target.GetComponent<EnemyMovement>();
        if (em != null) 
        {
            em.IsInThrowState = true;
            em.StartThrowAnimation();
        }

        if (cc != null) cc.enabled = false;

        _currentProjectile = target;
        HashSet<IDamageable> hitTargetsDuringFlight = new HashSet<IDamageable>();

        float elapsed = 0f;
        while (elapsed < settings.throwDuration)
        {
            if (target == null) yield break;
            if (em != null && !em.IsInThrowState) break;

            elapsed += Time.deltaTime;
            float t = elapsed / settings.throwDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            float heightOffset = 4 * settings.throwHeight * t * (1 - t);
            currentPos.y += heightOffset;

            if (rb != null) rb.position = currentPos;
            target.transform.position = currentPos;

            Collider[] hits = Physics.OverlapSphere(currentPos, settings.projectileRadius);
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (hit.TryGetComponent<Hurtbox>(out var hurtbox))
                {
                    GameObject otherEnemy = hurtbox.Owner;

                    if (otherEnemy == null || otherEnemy == target || otherEnemy == gameObject) continue;

                    if (otherEnemy.TryGetComponent<IDamageable>(out var damageable))
                    {
                        if (hitTargetsDuringFlight.Add(damageable))
                        {
                            if (target == null) break; 
                            Vector3 knockbackDir = (otherEnemy.transform.position - target.transform.position).normalized;
                            knockbackDir.y = 0f;
                            if (knockbackDir == Vector3.zero) knockbackDir = throwDir;

                            damageable.TakeDamage(new HitData
                            {
                                Damage = settings.projectileDamage,
                                KnockbackDirection = knockbackDir.normalized,
                                KnockbackForce = settings.projectileKnockback,
                                KnockUpForce = settings.projectileKnockUp,
                                HitStunDuration = 0.3f,
                                ShouldKnockdown = true,
                                Source = target 
                            });
                            
                            Debug.Log($"[PlayerGrapple] Thrown target {target.name} hit {otherEnemy.name} during flight!");
                            HitStop.Instance?.Do(0.05f);
                        }
                    }
                }
            }
            
            yield return null;
        }

        if (target != null)
        {
            bool interrupted = em != null && !em.IsInThrowState;
            
            if (!interrupted)
            {
                target.transform.position = targetPos;
            }

            if (cc != null) cc.enabled = true;

            if (em != null)
            {
                if (!interrupted)
                {
                    em.SetState(EnemyState.Airborne);
                }
            }
        }

        _currentProjectile = null;
    }

    private void OnDrawGizmos()
    {
        if (!showProjectileGizmos) return;

        if (_heldTarget != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(_heldTarget.transform.position, settings.projectileRadius);
        }

        if (_currentProjectile != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(_currentProjectile.transform.position, settings.projectileRadius);
        }
    }
}
