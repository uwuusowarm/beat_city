using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private InputActionReference attackAction;

    [Header("Fist Animation")]
    [SerializeField] private Transform fist1;
    [SerializeField] private Transform fist2;
    [SerializeField] private float punchDistance = 0.4f;  
    [SerializeField] private float punchSpeed = 12f;      
    
    public event Action OnAttackStarted;

    public event Action OnAttackEnded;

    public event Action<GameObject> OnHitLanded;

    private bool _isAttacking;
    
    private int _punchIndex;

    private void Awake()
    {
        hitbox.OnHitLanded += target => OnHitLanded?.Invoke(target);
    }

    private void OnEnable()
    {
        attackAction.action.performed += OnAttackInput;
        attackAction.action.Enable();
    }

    private void OnDisable()
    {
        attackAction.action.performed -= OnAttackInput;
        attackAction.action.Disable();
    }

    private void OnAttackInput(InputAction.CallbackContext _)
    {
        if (!_isAttacking)
            StartCoroutine(DoAttack());
    }

    private IEnumerator DoAttack()
    {
        _isAttacking = true;
        hitbox.Activate();
        OnAttackStarted?.Invoke();
        
        var fist = _punchIndex % 2 == 0 ? fist1 : fist2;
        _punchIndex++;
        if (fist != null)
            StartCoroutine(PunchFist(fist));

        yield return new WaitForSeconds(activeTime);

        hitbox.Deactivate();
        OnAttackEnded?.Invoke();
        _isAttacking = false;
    }
    
    private IEnumerator PunchFist(Transform fist)
    {
        var origin = fist.localPosition;
        var forward = origin + Vector3.forward * punchDistance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * punchSpeed;
            fist.localPosition = Vector3.Lerp(origin, forward, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * punchSpeed;
            fist.localPosition = Vector3.Lerp(forward, origin, t);
            yield return null;
        }

        fist.localPosition = origin;
    }
}
