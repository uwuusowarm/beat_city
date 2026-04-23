using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGrapple : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Hitbox grappleHitbox;
    [SerializeField] private InputActionReference grappleAction;
    [SerializeField] private float grappleActiveTime = 0.2f;
    [SerializeField] private int grappleDamage = 15;
    [SerializeField] private float throwDistance = 2f;
    [SerializeField] private float throwHeight = 1.5f;
    [SerializeField] private float throwDuration = 0.4f;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private float holdOffset = 1.2f;

    [Header("Projectile Damage")]
    [SerializeField] private int projectileDamage = 10;
    [SerializeField] private float projectileKnockback = 3f;
    [SerializeField] private float projectileKnockUp = 3f;
    [SerializeField] private float projectileRadius = 1f;

    [Header("Impact Settings")]
    [SerializeField] private float impactKnockback = 0f;
    [SerializeField] private float impactKnockUp = 0f;

    [Header("Visuals")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private bool showProjectileGizmos = true;

    private bool _isGrappling;
    private float _nextGrappleTime;
    private GameObject _heldTarget;
    private GameObject _currentProjectile;
    private MovementPlayer _movement;
    private PlayerCombat _combat;

    private void Awake()
    {
        _movement = GetComponent<MovementPlayer>();
        _combat = GetComponent<PlayerCombat>();
        if (characterModel == null)
            characterModel = transform.Find("Model");
        
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
        grappleAction.action.performed += OnGrappleInput;
        grappleAction.action.Enable();
    }

    private void OnDisable()
    {
        grappleAction.action.performed -= OnGrappleInput;
        grappleAction.action.Disable();
        if (_isGrappling && PlayerStateManager.Instance != null) PlayerStateManager.Instance.ResetToIdle();
    }

    private void OnGrappleInput(InputAction.CallbackContext context)
    {
        if (PlayerStateManager.Instance.CurrentState == PlayerState.Holding && _heldTarget != null)
        {
            StartCoroutine(PerformThrow(_heldTarget));
            return;
        }

        if (!_isGrappling && Time.time >= _nextGrappleTime)
        {
            if (!PlayerStateManager.Instance.CanPerformAction()) return;
            StartCoroutine(DoGrapple());
        }
    }

    private IEnumerator DoGrapple()
    {
        _isGrappling = true;
        PlayerStateManager.Instance.SetState(PlayerState.Grappling);
        
        if (_combat != null) _combat.ExtendFists(true);
        
        grappleHitbox.Activate();
        
        yield return new WaitForSeconds(grappleActiveTime);
        
        grappleHitbox.Deactivate();
        
        _isGrappling = false;
        
        if (PlayerStateManager.Instance.CurrentState == PlayerState.Grappling)
        {
            if (_combat != null) _combat.ExtendFists(false);
            PlayerStateManager.Instance.ResetToIdle();
            _nextGrappleTime = Time.time + cooldown;
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
        
        StartCoroutine(HoldTarget(target));
        
        Debug.Log($"[PlayerGrapple] Target {target.name} caught and being held.");
    }

    private IEnumerator HoldTarget(GameObject target)
    {
        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        while (PlayerStateManager.Instance.CurrentState == PlayerState.Holding && _heldTarget == target)
        {
            Vector3 targetPos = transform.position + characterModel.forward * holdOffset;
            target.transform.position = targetPos;
            
            target.transform.LookAt(transform.position);
            
            yield return null;
        }
    }

    private IEnumerator PerformThrow(GameObject target)
    {
        _heldTarget = null;
        PlayerStateManager.Instance.SetState(PlayerState.Grappling);

        Vector3 throwDir = characterModel.forward;
        if (_movement != null)
        {
            Vector3 inputDir = _movement.GetInputDirection();
            if (inputDir.magnitude > 0.1f)
            {
                throwDir = inputDir;

                characterModel.rotation = Quaternion.LookRotation(throwDir);
            }
        }

        yield return StartCoroutine(AnimateThrow(target, throwDir));

        if (_combat != null) _combat.ExtendFists(false);

        if (target != null)
        {
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(new HitData
                {
                    Damage = grappleDamage,
                    KnockbackDirection = throwDir,
                    KnockbackForce = impactKnockback, 
                    KnockUpForce = impactKnockUp,
                    HitStunDuration = 0.5f,
                    Source = gameObject
                });
            }
        }

        PlayerStateManager.Instance.ResetToIdle();
        _nextGrappleTime = Time.time + cooldown;
    }

    private IEnumerator AnimateThrow(GameObject target, Vector3 throwDir)
    {
        if (target == null) yield break;

        Vector3 startPos = target.transform.position;
        Vector3 targetPos = startPos + throwDir * throwDistance;
        
        CharacterController cc = target.GetComponent<CharacterController>();
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (cc != null) cc.enabled = false;

        _currentProjectile = target;
        HashSet<IDamageable> hitTargetsDuringFlight = new HashSet<IDamageable>();

        float elapsed = 0f;
        while (elapsed < throwDuration)
        {
            if (target == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / throwDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            float heightOffset = 4 * throwHeight * t * (1 - t);
            currentPos.y += heightOffset;

            if (rb != null) rb.position = currentPos;
            target.transform.position = currentPos;

            Collider[] hits = Physics.OverlapSphere(currentPos, projectileRadius);
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
                                Damage = projectileDamage,
                                KnockbackDirection = knockbackDir.normalized,
                                KnockbackForce = projectileKnockback,
                                KnockUpForce = projectileKnockUp, 
                                HitStunDuration = 0.3f,
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
            target.transform.position = targetPos;
            if (cc != null) cc.enabled = true;

            if (target.TryGetComponent<EnemyMovement>(out var em))
            {
                em.ApplyImpulse(impactKnockUp, throwDir * impactKnockback);
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
            Gizmos.DrawWireSphere(_heldTarget.transform.position, projectileRadius);
        }

        if (_currentProjectile != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(_currentProjectile.transform.position, projectileRadius);
        }
    }
}
