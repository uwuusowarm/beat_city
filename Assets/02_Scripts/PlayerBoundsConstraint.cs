using UnityEngine;

public class PlayerBoundsConstraint : MonoBehaviour
{
    [SerializeField] private float margin = 0.5f;

    private void LateUpdate()
    {
        var cam = CameraFollow.Instance;
        if (cam == null) return;

        var (minX, maxX) = cam.GetVisibleWorldBoundsX();

        var pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX + margin, maxX - margin);
        transform.position = pos;
    }

    private void OnDrawGizmos()
    {
        if (CameraFollow.Instance == null) return;

        var (minX, maxX) = CameraFollow.Instance.GetVisibleWorldBoundsX();
        float height = 10f;
        float y = transform.position.y;

        bool locked = CameraFollow.Instance.IsLocked;
        Color lineColor = locked ? Color.red : Color.yellow;

        Gizmos.color = lineColor;
        Gizmos.DrawLine(new Vector3(minX + margin, y - height * 0.5f, 0f),
                        new Vector3(minX + margin, y + height * 0.5f, 0f));

        Gizmos.DrawLine(new Vector3(maxX - margin, y - height * 0.5f, 0f),
                        new Vector3(maxX - margin, y + height * 0.5f, 0f));

        Gizmos.color = locked ? new Color(1f, 0f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawLine(new Vector3(minX + margin, y, 0f),
                        new Vector3(maxX - margin, y, 0f));
    }
}
