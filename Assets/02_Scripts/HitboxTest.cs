using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HitboxTest : MonoBehaviour
{
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private float activeTime = 0.2f;

    [Header("Fist Animation")]
    [SerializeField] private Transform fist1;
    [SerializeField] private Transform fist2;
    [SerializeField] private float punchDistance = 0.4f;  
    [SerializeField] private float punchSpeed = 12f;      

    private float _timer;
    private int _punchIndex;

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            hitbox.Activate();
            _timer = activeTime;

            var fist = _punchIndex % 2 == 0 ? fist1 : fist2;
            _punchIndex++;
            if (fist != null)
                StartCoroutine(PunchFist(fist));
        }

        if (_timer > 0f)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                hitbox.Deactivate();
        }
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
