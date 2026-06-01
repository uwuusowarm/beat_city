using UnityEngine;


[RequireComponent(typeof(BoxCollider))]
public class Hurtbox : MonoBehaviour
{
    [SerializeField] private GameObject owner;
    [SerializeField] private Vector3 size = new Vector3(1f, 2f, 1f);
    [SerializeField] private Vector3 offset = Vector3.zero;

    public GameObject Owner => owner;

    private void Awake()
    {
        ApplyToCollider();
        if (owner == null)
            owner = transform.root.gameObject;
    }

    private void OnValidate()
    {
        ApplyToCollider();
    }

    private void ApplyToCollider()
    {
        if (!TryGetComponent<BoxCollider>(out var col)) return;
        col.isTrigger = true;
        col.center = offset;
        col.size = size;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
        Gizmos.DrawCube(offset, size);
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.9f);
        Gizmos.DrawWireCube(offset, size);
    }
}
