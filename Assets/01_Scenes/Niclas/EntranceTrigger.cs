using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EntranceTrigger : MonoBehaviour
{
    [SerializeField] private EnemyEntrance[] enemies;
    [SerializeField] private bool lockCamera = true;

    private bool _activated;
    private int _aliveCount;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_activated) return;
        if (!other.CompareTag("Player")) return;

        _activated = true;

        if (lockCamera)
            CameraFollow.Instance?.Lock();

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            if (enemy.TryGetComponent<Health>(out var health))
            {
                _aliveCount++;
                health.OnDeath += OnEnemyDied;
            }

            enemy.BeginEntrance();
        }

        Debug.Log($"[EntranceTrigger] {gameObject.name} triggered. {_aliveCount} enemies entering");
    }

    private void OnEnemyDied()
    {
        _aliveCount--;
        Debug.Log($"[EntranceTrigger] enemy ded. {_aliveCount} remain");

        if (_aliveCount <= 0)
        {
            if (lockCamera)
                CameraFollow.Instance?.Unlock();

            Debug.Log($"[EntranceTrigger] {gameObject.name} clear");
        }
    }

    private void OnDrawGizmos()
    {
        if (TryGetComponent<BoxCollider>(out var col))
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f);
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireCube(col.center, col.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        if (enemies == null) return;
        Gizmos.color = Color.white;
        foreach (var enemy in enemies)
            if (enemy != null)
                Gizmos.DrawLine(transform.position, enemy.transform.position);
    }
}