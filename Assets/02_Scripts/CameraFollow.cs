using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -10f);

    private float _minX = float.NegativeInfinity;
    private float _maxX = float.PositiveInfinity;

    public bool IsLocked { get; private set; }
    private float _lockedX;

    private void Awake() => Instance = this;

    private void LateUpdate()
    {
        if (target == null) return;

        float targetX = IsLocked
            ? _lockedX
            : Mathf.Clamp(target.position.x + offset.x, _minX, _maxX);

        var desired = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }

    public void Lock()
    {
        IsLocked = true;
        _lockedX = transform.position.x;
    }

    public void Unlock()
    {
        _minX = transform.position.x;
        IsLocked = false;
    }

    public void SetBounds(float minX, float maxX)
    {
        _minX = minX;
        _maxX = maxX;
    }

    public (float min, float max) GetVisibleWorldBoundsX(float worldY = 0f)
    {
        var cam = Camera.main;
        if (cam == null) return (float.NegativeInfinity, float.PositiveInfinity);

        var left  = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, cam.nearClipPlane));
        var right = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, cam.nearClipPlane));
        return (left.x, right.x);
    }
}
