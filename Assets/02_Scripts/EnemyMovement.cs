using UnityEngine;

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

    private Transform player;
    private float tacticTimer;
    private bool isFlanking;
    private Vector3 currentFlankOffset;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
            player = playerObj.transform; 
        else
            Debug.LogWarning("[EnemyMovement] No GameObject with tag 'Player' found.");

        PickNewTactic();
    }

    private void Update()
    {
        if (player == null) return;

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

        isFlanking = Random.value > 0.5f; 

        if (isFlanking)
        {
            float randomAngle = Random.Range(0f, 360f);
            currentFlankOffset = new Vector3(Mathf.Sin(randomAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(randomAngle * Mathf.Deg2Rad)) * flankDistance;
        }
    }

    private void MoveBasedOnTactic()
    {
        Vector3 targetPosition;

        if (isFlanking)
        {
            targetPosition = player.position + currentFlankOffset;
        }
        else
        {
            Vector3 dirToPlayer = (transform.position - player.position).normalized;
            targetPosition = player.position + (dirToPlayer * attackDistance);
        }

        targetPosition.y = transform.position.y;

        if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
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
