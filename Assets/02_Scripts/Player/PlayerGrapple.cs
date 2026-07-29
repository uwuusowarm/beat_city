﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerGrapple : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private PlayerSettings settings;
    [Header("Grab Box")]
    [Tooltip("Anchor transform of the grab box (the old Hitbox_Grapple GameObject). Size/offset live in the PlayerSettings.")]
    [SerializeField] private Transform grabBoxAnchor;
    [FormerlySerializedAs("grappleAction")]
    [SerializeField] private InputActionReference[] throwActions;

    [Header("Visuals")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private Animator animator;
    [SerializeField] private bool showProjectileGizmos = true;
    [SerializeField] private bool showGrabGizmos = true;

    private float _nextGrappleTime;
    private GameObject _heldTarget;
    private GameObject _currentProjectile;
    private MovementPlayer _movement;
    private PlayerCombat _combat;
    private InputBuffer _inputBuffer;
    private bool _stateSubscribed;
    private float _moveIntentTimer;
    private Vector3 _moveIntentDir;

    private GameObject _debugTarget;
    private bool _debugIntentOk;
    private bool _debugGateOpen;

    private void Awake()
    {
        settings = SettingsResolver.ResolvePlayerSettings(settings);

        _movement = GetComponent<MovementPlayer>();
        _combat = GetComponent<PlayerCombat>();
        _inputBuffer = GetComponent<InputBuffer>();
        if (characterModel == null)
            characterModel = transform.Find("Model");
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (grabBoxAnchor == null)
        {
            Debug.LogError($"[PlayerGrapple] grabBoxAnchor is NOT assigned on {gameObject.name}!");
        }

        if (settings == null)
        {
            Debug.LogWarning("[PlayerGrapple] No PlayerSettings found (provider/inspector/resources).");
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
        if (PlayerStateManager.Instance != null &&
            PlayerStateManager.Instance.CurrentState == PlayerState.Grappling)
        {
            PlayerStateManager.Instance.ResetToIdle();
        }
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
        Debug.Log("Player is no longer holding");

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

        UpdateMoveIntent();

        _debugGateOpen = PlayerStateManager.Instance.CurrentState == PlayerState.Idle
                         && Time.time >= _nextGrappleTime;
        _debugTarget = null;
        _debugIntentOk = false;

        if (_debugGateOpen && TryGetGrappleTarget(out var target))
        {
            _debugTarget = target;
            _debugIntentOk = HasGrabIntent(target);

            if (_debugIntentOk)
            {
                DoGrapple(target);
            }
        }
    }

    private Vector3 GrabBoxOffset => settings != null ? settings.grabBoxOffset : new Vector3(0f, 0f, 0.8f);
    private Vector3 GrabBoxSize => settings != null ? settings.grabBoxSize : new Vector3(1f, 1f, 0.7f);

    private bool TryGetGrappleTarget(out GameObject target)
    {
        target = null;
        if (grabBoxAnchor == null) return false;

        Vector3 worldCenter = grabBoxAnchor.TransformPoint(GrabBoxOffset);
        Collider[] hits = Physics.OverlapBox(worldCenter, GrabBoxSize * 0.5f, grabBoxAnchor.rotation);

        foreach (var col in hits)
        {
            if (col == null) continue;

            if (col.TryGetComponent<Hurtbox>(out var hurtbox))
            {
                if (hurtbox.Owner == gameObject) continue;
                if (hurtbox.Owner.CompareTag(gameObject.tag)) continue;

                if (hurtbox.Owner.TryGetComponent<Health>(out var health) && health.Current > 0)
                {
                    target = hurtbox.Owner;
                    return true;
                }
            }
        }
        return false;
    }

    private void UpdateMoveIntent()
    {
        Vector3 input = _movement != null ? _movement.GetInputDirection() : Vector3.zero;

        if (input.sqrMagnitude < 0.0001f)
        {
            _moveIntentTimer = 0f;
            _moveIntentDir = Vector3.zero;
            return;
        }

        float threshold = settings != null ? settings.grabIntentDot : 0.5f;
        if (_moveIntentDir == Vector3.zero || Vector3.Dot(input, _moveIntentDir) < threshold)
        {
            _moveIntentDir = input;
            _moveIntentTimer = 0f;
        }

        _moveIntentTimer += Time.fixedDeltaTime;
    }

    private bool HasGrabIntent(GameObject target)
    {
        if (settings == null || !settings.requireGrabIntent || _movement == null) return true;
        if (_moveIntentTimer < settings.grabIntentTime) return false;

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return false;

        Vector3 input = _movement.GetInputDirection();
        if (input.sqrMagnitude < 0.0001f) return false;

        return Vector3.Dot(input, toTarget.normalized) >= settings.grabIntentDot;
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

    private void DoGrapple(GameObject target)
    {
        PlayerStateManager.Instance.SetState(PlayerState.Grappling);

        HandleGrappleHit(target);

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
            Debug.Log("Player is holding");
        }

        StartCoroutine(HoldTarget(target));

        Debug.Log($"[PlayerGrapple] Target {target.name} caught and being held.");
    }

    private IEnumerator HoldTarget(GameObject target)
    {
        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        target.TryGetComponent<Health>(out var targetHealth);
        
        try
        {
            while (PlayerStateManager.Instance.CurrentState == PlayerState.Holding && _heldTarget == target)
            {
                if (target == null || (targetHealth != null && targetHealth.Current <= 0))
                {
                    ReleaseHold();
                    yield break;
                }

                Vector3 targetPos = transform.position
                                    + characterModel.forward * settings.holdOffset
                                    + characterModel.right * settings.grappleHoldOffset.x
                                    + Vector3.up * settings.grappleHoldOffset.y
                                    + characterModel.forward * settings.grappleHoldOffset.z;
                target.transform.position = targetPos;

                target.transform.LookAt(transform.position);

                yield return null;
            }
        }
        finally
        {
            bool midThrow = target != null
                            && target.TryGetComponent<EnemyMovement>(out var thrownEm)
                            && thrownEm.IsBeingThrown;

            if (cc != null && !midThrow) cc.enabled = true;
        }
    }

    private void ReleaseHold()
    {
        _heldTarget = null;

        if (animator != null) animator.SetBool("IsGrabbing", false);
        Debug.Log("Player is no longer holding");

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
            Debug.Log("Player is no longer holding");
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

        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.Shake(settings.grappleDamage);
        }

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

                if (settings != null)
                {
                    HitStop.Instance?.Do(settings.grappleImpactHitStop);
                }

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
        
        try
        {
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

                                if (ScreenShake.Instance != null)
                                {
                                    ScreenShake.Instance.Shake(settings.projectileDamage);
                                }

                                Debug.Log($"[PlayerGrapple] Thrown target {target.name} hit {otherEnemy.name} during flight!");
                                if (settings != null)
                                {
                                    HitStop.Instance?.Do(settings.grappleProjectileHitStop);
                                }
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
        finally
        {
            if (cc != null) cc.enabled = true;
        }
    }

    private void DrawProjectileGizmos()
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

    private void OnDrawGizmos()
    {
        DrawProjectileGizmos();

        if (!showGrabGizmos || grabBoxAnchor == null) return;

        Color boxColor;
        if (!Application.isPlaying)     boxColor = new Color(1f, 1f, 1f, 0.5f);
        else if (!_debugGateOpen)       boxColor = new Color(0.4f, 0.4f, 0.4f, 0.5f); 
        else if (_debugTarget == null)  boxColor = new Color(1f, 1f, 1f, 0.5f);      
        else if (!_debugIntentOk)       boxColor = new Color(1f, 0.35f, 0f, 1f);      
        else                            boxColor = new Color(0f, 1f, 0f, 1f);         

        Gizmos.color = boxColor;
        Gizmos.matrix = grabBoxAnchor.localToWorldMatrix;
        Gizmos.DrawWireCube(GrabBoxOffset, GrabBoxSize);
        Gizmos.matrix = Matrix4x4.identity;

        if (!Application.isPlaying) return;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float intentTime = settings != null ? settings.grabIntentTime : 0f;
        float dotThreshold = settings != null ? settings.grabIntentDot : 0.5f;

        if (_moveIntentDir != Vector3.zero)
        {
            float progress = intentTime <= 0f ? 1f : Mathf.Clamp01(_moveIntentTimer / intentTime);
            Gizmos.color = progress >= 1f ? Color.green : Color.yellow;
            Gizmos.DrawLine(origin, origin + _moveIntentDir * (1.5f * progress));
            Gizmos.DrawWireSphere(origin + _moveIntentDir * 1.5f, 0.06f);
        }

        if (_debugTarget == null) return;

        Vector3 toTarget = _debugTarget.transform.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;
        toTarget.Normalize();

        Gizmos.color = _debugIntentOk ? Color.green : new Color(1f, 0.35f, 0f, 1f);
        Gizmos.DrawLine(origin, _debugTarget.transform.position);

        float halfAngle = Mathf.Acos(Mathf.Clamp(dotThreshold, -1f, 1f)) * Mathf.Rad2Deg;
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
        Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0f, halfAngle, 0f) * toTarget) * 1.5f);
        Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0f, -halfAngle, 0f) * toTarget) * 1.5f);
    }
}
