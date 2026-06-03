using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float stopDistance = 1.5f;
    [SerializeField] private float gravity = 20f;

    [Header("Visuals")]
    public Transform characterModel;
    [SerializeField] private Animator animator;

    [Header("Distances")]
    public float attackDistance = 1.5f;
    //public float flankDistance = 3.5f;

    [Header("AI Tactics")]
    public float minRepositionTime = 1.0f;
    public float maxRepositionTime = 3.0f;

    [Header("Combat State")]
    public float comboHitStun = 0.5f;
    public float kickStunDuration = 1.5f;
    [SerializeField] private float standUpDelay = 1.0f;
    [SerializeField] private float maxJugglingVelocity = 10f;
    [SerializeField] private float maxIncomingJugglingForce = 3f;
    public bool IsStunned => hitStunTimer > 0f || _isStandingUp || _isLyingDown;

    private Transform player;
    private float tacticTimer;
    private bool isFlanking;
    private float flankAngle;
    private float flankDirection = 1f;
    private Vector3 currentFlankOffset;

    private CharacterController controller;
    private float verticalVelocity;
    private float hitStunTimer;
    private float _lieDownTimer;
    private bool _isStandingUp;
    private bool _isLyingDown;
    private Vector3 externalForce;
    public Vector3 ExternalForce { get => externalForce; set => externalForce = value; }
    private float horizontalDrag = 5f;

    public bool IsInThrowState { get; set; }
    
    private Health _health;
    private EnemyCombat _combat;

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

        PickNewTactic();

        if (TryGetComponent<Health>(out var health))
        {
            health.OnHit += OnHit;
        }
    }

    public void ApplyImpulse(float knockUpForce, Vector3 knockbackForce, float hitStunReset = 0f)    {
        if (animator != null && !_isLyingDown && !_isStandingUp)
        {
            animator.SetTrigger("Hit");
        }

        if (knockUpForce > 0f)
        {
            if (!controller.isGrounded)
            {
                float effectiveForce = Mathf.Min(knockUpForce, maxIncomingJugglingForce);
                float targetVelocity = Mathf.Max(verticalVelocity * 0.2f + effectiveForce, effectiveForce);
                verticalVelocity = Mathf.Min(targetVelocity, maxJugglingVelocity);
            }
            else
            {
                verticalVelocity = knockUpForce;
            }

            if (animator != null)
            {
                animator.SetBool("IsFalling", true);
            }
        }
        if (knockbackForce != Vector3.zero)
        {
            externalForce = knockbackForce;
            if (knockbackForce.magnitude > moveSpeed * 1.5f)
            {
                IsInThrowState = true;
                hitStunTimer = kickStunDuration;
            }
        }
        if (hitStunReset > 0f) hitStunTimer = hitStunReset;
    }

    private void OnHit(HitData hitData)
    {
        if (animator != null && !_isLyingDown && !_isStandingUp)
        {
            animator.SetTrigger("Hit");
        }

        if (hitData.KnockUpForce > 0f)
        {
            if (!controller.isGrounded)
            {
                float effectiveForce = Mathf.Min(hitData.KnockUpForce, maxIncomingJugglingForce);
                float targetVelocity = Mathf.Max(verticalVelocity * 0.2f + effectiveForce, effectiveForce);
                verticalVelocity = Mathf.Min(targetVelocity, maxJugglingVelocity);
            }
            else
            {
                verticalVelocity = hitData.KnockUpForce;
            }
            
            if (animator != null && verticalVelocity > 0.1f)
            {
                animator.SetBool("IsFalling", true);
            }
        }

        if (hitData.HitStunDuration > 0f)
        {
            hitStunTimer = hitData.HitStunDuration;
        }

        if (hitData.KnockbackForce > 0f && hitData.KnockbackDirection != Vector3.zero)
        {
            externalForce = hitData.KnockbackDirection * hitData.KnockbackForce;
            if (hitData.KnockbackForce > moveSpeed * 1.5f)
            {
                IsInThrowState = true;
                hitStunTimer = kickStunDuration;
            }
        }
    }

    public void ForceRecalculateTactic()
    {
        tacticTimer = 0f;
    }

    private void Update()
    {
        if (player == null) return;
        if (_health != null && _health.Current <= 0)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        bool grounded = controller.isGrounded;

        if (hitStunTimer > 0f || !grounded || _isLyingDown || _isStandingUp)
        {
            if (hitStunTimer > 0f) hitStunTimer -= Time.deltaTime;
            
            if ((grounded || controller.velocity.y == 0) && verticalVelocity <= 0f)
            {
                if (IsInThrowState || (animator != null && animator.GetBool("IsFalling")))
                {
                    if (!_isLyingDown && !_isStandingUp)
                    {
                        _isLyingDown = true;
                        _lieDownTimer = standUpDelay;
                        if (animator != null)
                        {
                            animator.SetBool("IsFalling", true); 
                        }
                    }
                }
            }

            if (!grounded && verticalVelocity > 0f)
            {
                _isLyingDown = false;
                _isStandingUp = false;
            }

            if (_isLyingDown)
            {
                _lieDownTimer -= Time.deltaTime;
                if (_lieDownTimer <= 0f)
                {
                    _isLyingDown = false;
                    _isStandingUp = true;
                    if (animator != null)
                    {
                        animator.SetBool("IsFalling", false); 
                        animator.SetTrigger("StandUp");
                    }
                    StartCoroutine(FinishStandUp());
                }
            }

            ApplyGravityAndMove(Vector3.zero);
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

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

    private IEnumerator FinishStandUp()
    {
        yield return new WaitForSeconds(1.0f); 
        _isStandingUp = false;
        IsInThrowState = false;
    }

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
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 totalMovement = moveVelocity + externalForce;
        totalMovement.y = verticalVelocity;
        
        controller.Move(totalMovement * Time.deltaTime);

        if (externalForce.magnitude > 0.01f)
        {
            externalForce -= externalForce * horizontalDrag * Time.deltaTime;
        }
        else
        {
            externalForce = Vector3.zero;
            IsInThrowState = false;
        }
    }

    private void LookAtPlayer()
    {
        if (characterModel == null) return;
        float dirX = (player.position.x >= transform.position.x) ? 1f : -1f;
        Vector3 strictDirection = new Vector3(dirX, 0f, 0f);
        
        characterModel.rotation = Quaternion.LookRotation(strictDirection);
    }

    /*private void MoveTowardsPlayer()
    {
        if (_target == null) return;

        float dist = Vector3.Distance(_target.position, transform.position);
        if (dist <= stopDistance) return;

        var dir = (_target.position - transform.position).normalized;
        dir.y = 0f;

        transform.position = Vector3.MoveTowards(transform.position, _target.position, moveSpeed * Time.deltaTime);

        if (characterModel != null && dir != Vector3.zero)
            characterModel.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
    }*/
}
