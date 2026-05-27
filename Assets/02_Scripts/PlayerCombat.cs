using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private float activeTime = 0.2f;
    //[SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputBuffer inputBuffer;

    [Header("Fist Animation")]
    [SerializeField] private Transform fist1;
    [SerializeField] private Transform fist2;
    [SerializeField] private float punchDistance = 0.4f;  
    [SerializeField] private float punchSpeed = 12f;      
    
    public event Action OnAttackStarted;

    public event Action OnAttackEnded;

    public event Action<GameObject> OnHitLanded;
    
    public Animator animator;

    private bool _isAttacking;
    
    private int _punchIndex;

    private float _fist1InitialZ;
    private float _fist2InitialZ;

    private void Awake()
    {
        hitbox.OnHitLanded += target => OnHitLanded?.Invoke(target);
        
        if (fist1 != null) _fist1InitialZ = fist1.localPosition.z;
        if (fist2 != null) _fist2InitialZ = fist2.localPosition.z;
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    //private void OnEnable()
    //{
    //    attackAction.action.performed += OnAttackInput;
    //    attackAction.action.Enable();
    //}

    //private void OnDisable()
    //{
    //    attackAction.action.performed -= OnAttackInput;
    //    attackAction.action.Disable();
    //    if (_isAttacking && PlayerStateManager.Instance != null) PlayerStateManager.Instance.ResetToIdle();
    //}

    private void Update()
    {
        if (_isAttacking) return;
        if (!PlayerStateManager.Instance.CanPerformAction()) return;

        if (inputBuffer.TryConsume(out CombatInputType input))
        {
            switch (input)
            {
                case CombatInputType.Punch:
                    StartCoroutine(DoAttack());
                    break;
            }
        }
    }

    //private void OnAttackInput(InputAction.CallbackContext context)
    //{
    //    Debug.Log($"[PlayerCombat] Attack Input received from action: {context.action.name}");
    //    if (!_isAttacking && PlayerStateManager.Instance.CanPerformAction())
    //        StartCoroutine(DoAttack());
    //}

    public void ExtendFists(bool extend)
    {
        Vector3 localPos1 = fist1.localPosition;
        Vector3 localPos2 = fist2.localPosition;
        
        localPos1.z = extend ? _fist1InitialZ + punchDistance : _fist1InitialZ;
        localPos2.z = extend ? _fist2InitialZ + punchDistance : _fist2InitialZ;
        
        fist1.localPosition = localPos1;
        fist2.localPosition = localPos2;
    }

    private IEnumerator DoAttack()
    {
        _isAttacking = true;
        PlayerStateManager.Instance.SetState(PlayerState.Attacking);
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
        PlayerStateManager.Instance.ResetToIdle();
    }
    
    private IEnumerator PunchFist(Transform fist)
    {
        float initialZ = (fist == fist1) ? _fist1InitialZ : _fist2InitialZ;
        var origin = fist.localPosition;
        var forward = origin;
        forward.z = initialZ + punchDistance;

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
