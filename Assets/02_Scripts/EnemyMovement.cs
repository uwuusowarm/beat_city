using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float stopDistance = 1.5f;

    [Header("Visuals")]
    public Transform characterModel;

    [Header("Distances")]
    public float attackDistance = 1.5f;
    public float flankDistance = 3.5f;

    [Header("AI Tactics")]
    public float minRepositionTime = 1.0f;
    public float maxRepositionTime = 3.0f;


    public bool IsStunned => hitStunTimer > 0f;

    private Transform player;
    private float tacticTimer;
    private bool isFlanking;
    private float flankAngle;
    private float flankDirection = 1f;
    private Vector3 currentFlankOffset;

    private CharacterController controller;
    private float verticalVelocity;
    private float hitStunTimer;
    private Vector3 externalForce;
    private float horizontalDrag = 5f;

    private Health _health;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        _health = GetComponent<Health>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
            player = playerObj.transform; 
        else
            Debug.LogWarning("[EnemyMovement] No GameObject with tag 'Player' found.");

        PickNewTactic();

        if (TryGetComponent<Health>(out var health))
        {
            health.OnHit += OnHit;
        }
    }

    public void ApplyImpulse(float knockUpForce, Vector3 knockbackForce)
    {
        if (knockUpForce > 0f) verticalVelocity = knockUpForce;
        if (knockbackForce != Vector3.zero) externalForce = knockbackForce;
    }

    private void OnHit(HitData hitData)
    {
        if (hitData.KnockUpForce > 0f)
        {
            verticalVelocity = hitData.KnockUpForce;
        }

        if (hitData.HitStunDuration > 0f)
        {
            hitStunTimer = hitData.HitStunDuration;
        }

        if (hitData.KnockbackForce > 0f && hitData.KnockbackDirection != Vector3.zero)
        {
            externalForce = hitData.KnockbackDirection * hitData.KnockbackForce;
        }
    }

    private void Update()
    {
        if (player == null) return;
        if (_health != null && _health.Current <= 0) return;

        if (hitStunTimer > 0f)
        {
            hitStunTimer -= Time.deltaTime;
            ApplyGravityAndMove(Vector3.zero);
            return;
        }

        tacticTimer -= Time.deltaTime;
        if (tacticTimer <= 0f)
        {
            PickNewTactic();
        }

        MoveBasedOnTactic();
        LookAtPlayer();
    }

    private void PickNewTactic()
    {
        tacticTimer = Random.Range(minRepositionTime, maxRepositionTime);

        EnemyCombat combat = GetComponent<EnemyCombat>();
        if (combat != null && BeatEmUpDirector.Instance != null)
        {
            isFlanking = !BeatEmUpDirector.Instance.RequestAttackToken(combat);
        }
        else
        {
            isFlanking = Random.value > 0.5f; 
        }
        
        flankDirection = Random.value > 0.5f ? 1f : -1f;
        
        Vector3 dir = transform.position - player.position;
        flankAngle = Mathf.Atan2(dir.z, dir.x);
    }

    private void MoveBasedOnTactic()
    {
        Vector3 targetPosition;

        if (isFlanking)
        {
            flankAngle += flankDirection * (moveSpeed * 0.5f) * Time.deltaTime;
            Vector3 offset = new Vector3(Mathf.Cos(flankAngle), 0f, Mathf.Sin(flankAngle)) * flankDistance;
            targetPosition = player.position + offset;
        }
        else
        {
            Vector3 dirToPlayer = (transform.position - player.position).normalized;
            targetPosition = player.position + (dirToPlayer * attackDistance);
        }

        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0f;
        float distance = directionToTarget.magnitude;

        Vector3 moveVelocity = Vector3.zero;
        if (distance > 0.1f)
        {
            moveVelocity = directionToTarget.normalized * moveSpeed;
        }
        
        ApplyGravityAndMove(moveVelocity);
    }

    private void ApplyGravityAndMove(Vector3 moveVelocity)
    {
        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            verticalVelocity -= 20f * Time.deltaTime;
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
        }
    }

    private void LookAtPlayer()
    {
        if (characterModel == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        
        if (dir != Vector3.zero)
        {
            characterModel.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        }
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
