using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float stopDistance = 1.5f;

    [Header("Visuals")]
    public Transform characterModel;

    private Transform _target;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _target = player.transform;
        else
            Debug.LogWarning("[EnemyMovement] No GameObject with tag 'Player' found.");
    }

    private void Update()
    {
        MoveTowardsPlayer();
    }

    private void MoveTowardsPlayer()
    {
        if (_target == null) return;

        float dist = Vector3.Distance(_target.position, transform.position);
        if (dist <= stopDistance) return;

        var dir = (_target.position - transform.position).normalized;
        dir.y = 0f;

        transform.position = Vector3.MoveTowards(transform.position, _target.position, moveSpeed * Time.deltaTime);

        if (characterModel != null && dir != Vector3.zero)
            characterModel.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
    }
}
