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
    private EnemyDrop _drop;

    private void Start()
    {
        _health = GetComponent<Health>();
        _movement = GetComponent<EnemyMovement>();
        _animator = GetComponentInChildren<Animator>();
        _drop = GetComponent<EnemyDrop>();

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
        // Drops belong here, not in Health. Health is shared with the player,
        // which carries no EnemyDrop, so looking the component up there threw
        // before OnDeath was raised and swallowed the whole death sequence.
        // Dropped first so the item still spawns against a live collider.
        if (_drop != null) _drop.DropItem();

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddCoins(coinReward);
        }
        
        if (_movement != null)
        {
            _movement.SetState(EnemyState.Dead);
        }

        foreach (var c in GetComponentsInChildren<Collider>())
        {
            if (c is CharacterController) continue;
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