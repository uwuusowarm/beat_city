using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SuperAttack : MonoBehaviour
{
    [SerializeField] private InputActionReference superAction;
    [SerializeField] private Meter meter;
    [SerializeField] private int meterCost = 100;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private float spinDuration = 1f;

    private bool _isSpinning;

    private void OnEnable()
    {
        superAction.action.performed += OnSuperInput;
        superAction.action.Enable();
    }

    private void OnDisable()
    {
        superAction.action.performed -= OnSuperInput;
        superAction.action.Disable();
    }

    private void OnSuperInput(InputAction.CallbackContext _)
    {
        if (_isSpinning) return;
        if (!meter.TrySpend(meterCost)) return;

        StartCoroutine(DoSpin());
    }

    private IEnumerator DoSpin()
    {
        _isSpinning = true;
        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            Debug.Log("<color=yellow> SUPAAAAAAA  SPIIIIIIIIIIIIIIIIIIIIN ꉂ(˵˃ ᗜ ˂˵)");
            transform.Rotate(-spinSpeed * Time.deltaTime, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Vector3 euler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, euler.y, euler.z);

        _isSpinning = false;
    }
}