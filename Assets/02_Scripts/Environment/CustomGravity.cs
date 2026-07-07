using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravity : MonoBehaviour
{
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private float horizontalDrag = 5f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, -0.9f, 0f);
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayers = ~0;

    public bool IsGrounded { get; private set; }

    private Rigidbody _rb;
    private bool _wasGrounded;
    private bool _paused;
    private Coroutine _pauseRoutine;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;

        if (TryGetComponent<EnemyMovement>(out _))
        {
            enabled = false;
            return;
        }

        if (TryGetComponent<Health>(out var health))
            health.OnHit += OnHit;
    }

    private void OnHit(HitData hitData)
    {
        if (hitData.HitStunDuration > 0f)
            Pause(hitData.HitStunDuration);

        if (hitData.KnockbackForce > 0f)
        {
            _rb.linearVelocity = Vector3.zero;
            Vector3 force = hitData.KnockbackDirection * hitData.KnockbackForce;
            if (hitData.KnockUpForce > 0f) force += Vector3.up * hitData.KnockUpForce;
            _rb.AddForce(force, ForceMode.Impulse);
        }
    }

    public void Pause(float duration)
    {
        if (_pauseRoutine != null)
            StopCoroutine(_pauseRoutine);
        _pauseRoutine = StartCoroutine(PauseRoutine(duration));
    }

    private IEnumerator PauseRoutine(float duration)
    {
        _paused = true;

        var v = _rb.linearVelocity;
        v.y = 0f;
        _rb.linearVelocity = v;

        yield return new WaitForSecondsRealtime(duration);

        _paused = false;
        _pauseRoutine = null;
    }

    private void FixedUpdate()
    {
        if (_paused) return;

        IsGrounded = Physics.CheckSphere(
            transform.position + groundCheckOffset,
            groundCheckRadius,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        if (!IsGrounded)
        {
            _rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        }
        else
        {
            if (!_wasGrounded)
            {
                _rb.linearVelocity = Vector3.zero;
            }
            else
            {
                Vector3 horizontalVel = _rb.linearVelocity;
                horizontalVel.y = 0f;
                
                if (horizontalVel.magnitude > 0.1f)
                {
                    _rb.AddForce(-horizontalVel * horizontalDrag, ForceMode.Acceleration);
                }
                else if (horizontalVel.magnitude > 0f)
                {
                    horizontalVel = Vector3.zero;
                    horizontalVel.y = _rb.linearVelocity.y;
                    _rb.linearVelocity = horizontalVel;
                }
            }

            if (_rb.linearVelocity.y < 0f)
            {
                var v = _rb.linearVelocity;
                v.y = 0f;
                _rb.linearVelocity = v;
            }
        }

        _wasGrounded = IsGrounded;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckRadius);
    }
}
