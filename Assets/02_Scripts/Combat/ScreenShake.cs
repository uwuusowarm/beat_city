using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [Header("Scaling")]
    [Tooltip("Damage below this value won't trigger a shake")]
    [SerializeField] private int minDamage = 5;

    [Tooltip("How much each point of damage adds to shake intensity")]
    [SerializeField] private float damageScale = 0.01f;

    [Header("Shake")]
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private float maxIntensity = 0.5f;

    private Coroutine _current;
    private Vector3 _shakeOffset;
    private Vector3 _appliedOffset;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (_appliedOffset != Vector3.zero)
        {
            transform.position -= _appliedOffset;
            _appliedOffset = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (_shakeOffset != Vector3.zero)
        {
            transform.position += _shakeOffset;
            _appliedOffset = _shakeOffset;
        }
    }

    public void Shake(int damage)
    {
        if (damage < minDamage) return;

        float intensity = Mathf.Min(damage * damageScale, maxIntensity);

        if (_current != null)
            StopCoroutine(_current);
        _current = StartCoroutine(DoShake(intensity));
    }

    private IEnumerator DoShake(float intensity)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration);
            float x = Random.Range(-1f, 1f) * intensity * t;
            float y = Random.Range(-1f, 1f) * intensity * t;
            _shakeOffset = new Vector3(x, y, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _shakeOffset = Vector3.zero;
        _current = null;
    }
}
