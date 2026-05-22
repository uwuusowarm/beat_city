using UnityEngine;

public class PlayerBoundsConstraint : MonoBehaviour
{
    [SerializeField] private float margin = 0.5f;
    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        var camFollow = CameraFollow.Instance;
        if (!camFollow) return;

        if (!_cam) return;

        float distance = Vector3.Dot(transform.position - _cam.transform.position, _cam.transform.forward);
        var (minX, maxX) = camFollow.GetVisibleWorldBoundsX(distance);

        var pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX + margin, maxX - margin);
        transform.position = pos;
    }

    private void OnDrawGizmos()
    {
        var camFollow = CameraFollow.Instance;
        if (camFollow == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        float y = transform.position.y;
        float z = transform.position.z;

        float zStart = z - 5f;
        float zEnd = z + 5f;

        float distStart = Vector3.Dot(new Vector3(0, y, zStart) - cam.transform.position, cam.transform.forward);
        var (minStart, maxStart) = camFollow.GetVisibleWorldBoundsX(distStart);

        float distEnd = Vector3.Dot(new Vector3(0, y, zEnd) - cam.transform.position, cam.transform.forward);
        var (minEnd, maxEnd) = camFollow.GetVisibleWorldBoundsX(distEnd);

        bool locked = camFollow.IsLocked;
        Color lineColor = locked ? Color.red : Color.yellow;

        Gizmos.color = lineColor;
        Gizmos.DrawLine(new Vector3(minStart + margin, y, zStart),
                        new Vector3(minEnd + margin, y, zEnd));

        Gizmos.DrawLine(new Vector3(maxStart - margin, y, zStart),
                        new Vector3(maxEnd - margin, y, zEnd));

        float distCurrent = Vector3.Dot(transform.position - cam.transform.position, cam.transform.forward);
        var (minCurr, maxCurr) = camFollow.GetVisibleWorldBoundsX(distCurrent);

        Gizmos.color = locked ? new Color(1f, 0f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawLine(new Vector3(minCurr + margin, y, z),
                        new Vector3(maxCurr - margin, y, z));
    }
}
