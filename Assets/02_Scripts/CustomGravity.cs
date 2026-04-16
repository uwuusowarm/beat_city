using System.Collections;
using UnityEngine;

/// <summary>
/// Wendet Gravitation nur an wenn der Charakter nicht am Boden ist.
/// Wird bei Treffern automatisch pausiert (HitStun = Juggle-Float).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CustomGravity : MonoBehaviour
{
    [SerializeField] private float gravityScale = 2f;
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

        if (TryGetComponent<Health>(out var health))
            health.OnHit += OnHit;
    }

    private void OnHit(HitData hitData)
    {
        if (hitData.HitStunDuration > 0f)
            Pause(hitData.HitStunDuration);
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
        // Y-Velocity nullen damit der Enemy floated statt weiter zu fallen
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
                _rb.linearVelocity = Vector3.zero;
            else if (_rb.linearVelocity.y < 0f)
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
