using UnityEngine;

public class EnemyObstacleAvoidance : MonoBehaviour
{
    [Header("Probe")]
    [SerializeField] private float probeDistance = 1.5f;
    [SerializeField] private float feelerAngle = 45f;
    [SerializeField] private float probeHeight = 0.6f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private Vector3 _lastDesired; //gizmo help

    //in: intended direction, out: adjusted direction (X,Z)
    public Vector3 Adjust(Vector3 desired)
    {
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f) return desired;
        desired.Normalize();
        _lastDesired = desired;
        
        if (!Blocked(desired, out _))
            return desired;
        
        Vector3 left  = Quaternion.AngleAxis(-feelerAngle, Vector3.up) * desired;
        Vector3 right = Quaternion.AngleAxis( feelerAngle, Vector3.up) * desired;

        bool leftBlocked  = Blocked(left,  out float leftRoom);
        bool rightBlocked = Blocked(right, out float rightRoom);


        if (leftBlocked && !rightBlocked) return right;
        if (rightBlocked && !leftBlocked) return left;
        return leftRoom >= rightRoom ? left : right;
    }
    
    private bool Blocked(Vector3 dir, out float room)
    {
        room = probeDistance;
        Vector3 origin = transform.position + Vector3.up * probeHeight;

        if (!Physics.Raycast(origin, dir, out RaycastHit hit,
                             probeDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.collider.CompareTag("Player")) return false;
        if (hit.collider.transform.root == transform.root) return false;

        room = hit.distance;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 forward = Application.isPlaying && _lastDesired.sqrMagnitude > 0.0001f
            ? _lastDesired
            : Vector3.Scale(transform.forward, new Vector3(1f, 0f, 1f)).normalized;

        DrawProbe(forward);
        DrawProbe(Quaternion.AngleAxis(-feelerAngle, Vector3.up) * forward);
        DrawProbe(Quaternion.AngleAxis( feelerAngle, Vector3.up) * forward);
    }

    private void DrawProbe(Vector3 dir)
    {
        Vector3 origin = transform.position + Vector3.up * probeHeight;
        bool blocked = Blocked(dir, out float room);
        Gizmos.color = blocked ? Color.red : Color.green;
        Gizmos.DrawLine(origin, origin + dir * room);
        if (blocked) Gizmos.DrawWireSphere(origin + dir * room, 0.1f);
    }
}