using UnityEngine;
using UnityEngine.InputSystem;


public enum CombatInputType
{
    None,
    Punch,
    Kick,
    Special
}

public class InputBuffer : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference punchAction;
    [SerializeField] private InputActionReference kickAction;
    [SerializeField] private InputActionReference specialAction;


    [Header("Buffer")]
    [SerializeField] private float bufferTime = 0.5f;


    private CombatInputType bufferedInput = CombatInputType.None;
    private float bufferTimer;

    private void OnEnable()
    {
        if (punchAction != null)
        {
            punchAction.action.performed += OnPunch;
            punchAction.action.Enable();
        }

        if (kickAction != null)
        {
            kickAction.action.performed += OnKick;
            kickAction.action.Enable();
        }

        if (specialAction != null)
        {
            specialAction.action.performed += OnSpecial;
            specialAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (punchAction != null)
        {
            punchAction.action.performed -= OnPunch;
            punchAction.action.Disable();
        }

        if (kickAction != null)
        {
            kickAction.action.performed -= OnKick;
            kickAction.action.Disable();
        }

        if (specialAction != null)
        {
            specialAction.action.performed -= OnSpecial;
            specialAction.action.Disable();
        }
    }

    private void Update()
    {
        if (bufferedInput == CombatInputType.None) return;

        bufferTimer -= Time.deltaTime;

        if (bufferTimer <= 0f)
        {
            Clear();
        }
    }

    private void OnPunch(InputAction.CallbackContext context)
    {
        if (!context.ReadValueAsButton()) return;
        Buffer(CombatInputType.Punch);
        
    }

    private void OnKick(InputAction.CallbackContext context)
    {
        if (!context.ReadValueAsButton()) return;
        Buffer(CombatInputType.Kick);
    }

    private void OnSpecial(InputAction.CallbackContext context)
    {
        if (!context.ReadValueAsButton()) return;
        Buffer(CombatInputType.Special);
    }

    private void Buffer(CombatInputType input)
    {
        bufferedInput = input;
        bufferTimer = bufferTime;

        Debug.Log($"[CombatInputBuffer] Buffered input: {input}");
    }

    public bool TryConsume(out CombatInputType input)
    {
        input = bufferedInput;

        if (bufferedInput == CombatInputType.None)
            return false;

        Clear();
        return true;
    }

    public void Clear()
    {
        bufferedInput = CombatInputType.None;
        bufferTimer = 0f;
    }

}
