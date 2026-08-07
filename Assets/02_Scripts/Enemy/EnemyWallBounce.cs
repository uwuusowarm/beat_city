using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class EnemyWallBounce : MonoBehaviour
{
    [SerializeField] private float bounceFactor = 1.0f;
    [SerializeField] private float extraBounceImpulse = 4f;
    [SerializeField] private float bounceKnockUpForce = 8f;
    [SerializeField] private float bounceHitStun = 0.5f;
    [SerializeField] private float margin = 0.5f;
    [SerializeField] private float minBounceVelocity = 1f;
    [SerializeField] private int bounceDamage = 5;
    
    private EnemyMovement _enemyMovement;
    private Health _health;
    private Camera _cam;
    private Vector3 _lastPosition;
    private bool _hasEnteredScreen = false;

    private void Start()
    {
        _enemyMovement = GetComponent<EnemyMovement>();
        _health = GetComponent<Health>();
        _cam = Camera.main;
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        var camFollow = CameraFollow.Instance;
        if (!camFollow || !_cam) return;
        
        if (_enemyMovement != null
            && _enemyMovement.CurrentState == EnemyState.Grabbed
            && !_enemyMovement.IsBeingThrown)
        {
            _lastPosition = transform.position;
            return;
        }

        float distance = Vector3.Dot(transform.position - _cam.transform.position, _cam.transform.forward);
        var (minX, maxX) = camFollow.GetVisibleWorldBoundsX(distance);
        float leftBound = minX + margin;
        float rightBound = maxX - margin;

        Vector3 pos = transform.position;

        if (!_hasEnteredScreen)
        {
            if (pos.x >= leftBound && pos.x <= rightBound)
            {
                _hasEnteredScreen = true;
            }
            else
            {
                _lastPosition = pos;
                return;
            }
        }

        Vector3 force = _enemyMovement.ExternalForce;
        Vector3 frameVelocity = Time.deltaTime > 0f ? (pos - _lastPosition) / Time.deltaTime : Vector3.zero;

        bool bounced = false;
        
        float oldX = pos.x;
        pos.x = Mathf.Clamp(pos.x, leftBound, rightBound);

        if (pos.x < oldX) 
        {
            if (_lastPosition.x <= rightBound) 
            {
                float incomingVelocityX = force.x;
                if (_enemyMovement.IsInThrowState || (incomingVelocityX > minBounceVelocity && incomingVelocityX > _enemyMovement.moveSpeed * 1.1f))
                {
                    force.x = -(Mathf.Max(incomingVelocityX, frameVelocity.x) * bounceFactor + extraBounceImpulse);
                    bounced = true;
                }
                else if (force.x > 0)
                {
                    force.x = 0;
                }
            }
            else 
            {
                if (force.x > 0) force.x = 0;
            }
        }
        else if (pos.x > oldX) 
        {
            if (_lastPosition.x >= leftBound) 
            {
                float incomingVelocityX = force.x;
                if (_enemyMovement.IsInThrowState || (incomingVelocityX < -minBounceVelocity && incomingVelocityX < -_enemyMovement.moveSpeed * 1.1f))
                {
                    force.x = -(Mathf.Min(incomingVelocityX, frameVelocity.x) * bounceFactor - extraBounceImpulse);
                    bounced = true;
                }
                else if (force.x < 0)
                {
                    force.x = 0;
                }
            }
            else 
            {
                if (force.x < 0) force.x = 0;
            }
        }

        if (bounced || pos != transform.position)
        {
            transform.position = pos;
            _enemyMovement.ExternalForce = force;
            
            if (bounced)
            {
                _enemyMovement.ApplyImpulse(bounceKnockUpForce, force, bounceHitStun);

                if (_health != null && bounceDamage > 0)
                {
                    _health.TakeDamage(new HitData
                    {
                        Damage = bounceDamage,
                        Source = gameObject 
                    });
                }
                
                Debug.Log($"[EnemyWallBounce] {gameObject.name} bounced off wall with {bounceKnockUpForce}!");
            }
        }

        _lastPosition = transform.position;
    }
}
