using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Arena : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyGroupPrefabs;
    [SerializeField] private int enemyCount = 3;
    [SerializeField] private float spawnDelay = 1.5f;

    private List<Transform> _spawnPoints = new();
    private bool _activated;
    private int _aliveCount;
    private int _spawnedCount;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        foreach (Transform child in transform)
            _spawnPoints.Add(child);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_activated) return;
        if (!other.CompareTag("Player")) return;

        _activated = true;
        CameraFollow.Instance?.Lock();
        Debug.Log($"[Arena] {gameObject.name} activated. Spawning {enemyCount} enemies");
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy(i);
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void SpawnEnemy(int index)
    {
        GameObject enemyPrefab = GetRandomChar();

        if (enemyPrefab == null) return;

        var spawnPos = _spawnPoints.Count > 0
            ? _spawnPoints[index % _spawnPoints.Count].position
            : transform.position + Vector3.right * (index * 2f);

        var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        ApplyDifficulty(go);
        _aliveCount++;
        _spawnedCount++;

        if (go.TryGetComponent<Health>(out var health))
            health.OnDeath += OnEnemyDied;

        Debug.Log($"[Arena] enemy {_spawnedCount}/{enemyCount} spawned at {spawnPos}");
    }

    private GameObject GetRandomChar()
    {
        int groupIndex = Random.Range(0, enemyGroupPrefabs.Length);

        GameObject selectedGroup = enemyGroupPrefabs[groupIndex];

        int childIndex = Random.Range(0, selectedGroup.transform.childCount);

        return selectedGroup.transform.GetChild(childIndex).gameObject;
    }

    private void OnEnemyDied()
    {
        _aliveCount--;
        Debug.Log($"[Arena] enemies defeated. {_aliveCount} remaining");

        if (_aliveCount <= 0 && _spawnedCount >= enemyCount)
        {
            CameraFollow.Instance?.Unlock();
            Debug.Log($"[Arena] {gameObject.name} done. Camera unlocked");
        }
    }

    private void ApplyDifficulty(GameObject enemy)
    {
        if (DifficultyManager.Instance == null) return;

        float healthMultiplier = DifficultyManager.Instance.GetHealthMultiplier();
        float damageMultiplier = DifficultyManager.Instance.GetDamageMultiplier();

        if (enemy.TryGetComponent<Health>(out var health))
        {
            int newMaxHealth = Mathf.RoundToInt(health.Max * healthMultiplier);
            health.SetMaxHealth(newMaxHealth);
        }

        if (enemy.TryGetComponent<Hitbox>(out var hitbox))
        {
            int newDamage = Mathf.RoundToInt(hitbox.Damage * damageMultiplier);
            hitbox.Damage = newDamage;
        }
    }

    private void OnDrawGizmos()
    {
        if (!TryGetComponent<BoxCollider>(out var col)) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        bool locked = Application.isPlaying && CameraFollow.Instance != null && CameraFollow.Instance.IsLocked;

        Gizmos.color = locked ? new Color(1f, 0f, 0f, 0.1f) : new Color(0f, 1f, 0f, 0.1f);
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = locked ? Color.red : Color.green;
        Gizmos.DrawWireCube(col.center, col.size);

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.cyan;
        foreach (Transform child in transform)
        {
            Gizmos.DrawSphere(child.position, 0.2f);
            Gizmos.DrawLine(transform.position, child.position);
        }
    }
}
