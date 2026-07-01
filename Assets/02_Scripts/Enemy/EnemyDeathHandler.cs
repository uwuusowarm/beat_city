using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private EnemySettings settings;
    [SerializeField] private int coinReward = 10;

    public EnemySettings Settings { get => settings; set => settings = value; }

    private EnemyMovement _movement;
    private Health _health;
    private Animator _animator;

    private void Start()
    {
        _health = GetComponent<Health>();
        _movement = GetComponent<EnemyMovement>();
        _animator = GetComponentInChildren<Animator>();

        if (settings == null && _movement != null)
        {
            settings = _movement.meleeSettings;
        }

        if (_health != null) _health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_health != null) _health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddCoins(coinReward);
        }
        
        if (_movement != null)
        {
            _movement.SetState(EnemyState.Dead);
        }

        if (_animator != null)
        {
            _animator.SetBool("IsFalling", true);
            _animator.speed = 1f;
        }

        foreach (var c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        float delay = settings != null ? settings.despawnDelay : 3.0f;
        StartCoroutine(Despawn(delay));
    }

    private IEnumerator Despawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        Destroy(gameObject);
    }
}