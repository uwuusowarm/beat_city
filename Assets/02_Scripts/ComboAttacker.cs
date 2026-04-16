using UnityEngine;

public class ComboAttacker : MonoBehaviour
{
    [SerializeField] private int comboThreshold = 3;
    [SerializeField] private float knockbackForce = 5f; 
    [SerializeField] private float knockUpForce = 6f;   
    [SerializeField] private float comboResetTime = 2f;

    private int _hitCount;
    private float _lastHitTime;

    private void Awake()
    {
        foreach (var hitbox in GetComponentsInChildren<Hitbox>(includeInactive: true))
            hitbox.OnHitLanded += OnHitLanded;
    }

    private void OnHitLanded(GameObject target)
    {
        if (Time.time - _lastHitTime > comboResetTime)
        {
            _hitCount = 0;
            Debug.Log("[Combo] Reset");
        }

        _lastHitTime = Time.time;
        _hitCount++;
        Debug.Log($"[Combo] Hit {_hitCount}/{comboThreshold}");

        if (_hitCount >= comboThreshold)
        {
            _hitCount = 0;
            Juggle(target);
        }
    }

    private void Juggle(GameObject target)
    {
        if (!target.TryGetComponent<Rigidbody>(out var rb)) return;

        rb.linearVelocity = Vector3.zero;

        var horizontal = (target.transform.position - transform.position).normalized;
        horizontal.y = 0f;

        rb.AddForce(horizontal * knockbackForce + Vector3.up * knockUpForce, ForceMode.Impulse);
        Debug.Log($"[Combo] JUGGLERONI! {target.name} KB:{knockbackForce} KnockUp:{knockUpForce}");
    }
}
