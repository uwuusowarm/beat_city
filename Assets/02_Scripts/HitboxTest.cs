using UnityEngine;
using UnityEngine.InputSystem;

public class HitboxTest : MonoBehaviour
{
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private float activeTime = 0.2f;

    private float _timer;

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            hitbox.Activate();
            _timer = activeTime;
        }

        if (_timer > 0f)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                hitbox.Deactivate();
        }
    }
}
