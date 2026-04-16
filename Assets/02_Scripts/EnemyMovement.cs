using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform targetPos;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stopDistance = 1.5f;
    
    void Start()
    {
        
    }


    void Update()
    {
        MoveTowardsPlayer();
    }

    private void MoveTowardsPlayer()
    {
        if (targetPos == null)
        {
            Debug.LogWarning("targetPos is missing in Inspector!");
            this.enabled = false;
            return;
        }

        float dist = Vector3.Distance(targetPos.position, transform.position);

        if (dist > stopDistance)
        {
            transform.position = Vector3.MoveTowards(this.transform.position, targetPos.position, moveSpeed * Time.deltaTime);
        }
    }
}
