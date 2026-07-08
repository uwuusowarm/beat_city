using UnityEngine;

public class CameraPivotFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    private void LateUpdate()
    {
        if (!target) return;
        transform.position = target.position;
    }
}