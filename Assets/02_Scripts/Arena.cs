using System.Collections.Generic;
using UnityEngine;

public class Arena : MonoBehaviour
{
    [SerializeField] private List<Health> enemies;
    [SerializeField] private string enemyTag = "Enemy";

    private bool _activated;
    private int _aliveCount;

    private void Awake()
    {
        if (enemies == null || enemies.Count == 0)
        {
            enemies = new List<Health>();
            foreach (var go in GameObject.FindGameObjectsWithTag(enemyTag))
            {
                if (go.TryGetComponent<Health>(out var h))
                    enemies.Add(h);
            }
        }

        _aliveCount = enemies.Count;

        foreach (var enemy in enemies)
            enemy.OnDeath += OnEnemyDied;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_activated) return;
        if (!other.CompareTag("Player")) return;

        _activated = true;
        CameraFollow.Instance?.Lock();
        Debug.Log($"[Arena] {gameObject.name} activated. {_aliveCount} enemies remaining");
    }

    private void OnEnemyDied()
    {
        _aliveCount--;
        Debug.Log($"[Arena] enemy defeated. {_aliveCount} remaining");

        if (_aliveCount <= 0)
        {
            CameraFollow.Instance?.Unlock();
            Debug.Log($"[Arena] {gameObject.name} done. Camera unlocked");
        }
    }

    private void OnDrawGizmos()
    {
        if (!TryGetComponent<BoxCollider>(out var col)) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = _activated
            ? new Color(1f, 0f, 0f, 0.1f)
            : new Color(0f, 1f, 0f, 0.1f);
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = _activated ? Color.red : Color.green;
        Gizmos.DrawWireCube(col.center, col.size);
    }
}
