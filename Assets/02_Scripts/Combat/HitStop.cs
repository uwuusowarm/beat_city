using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    private Coroutine _current;

    private void Awake()
    {
        Instance = this;
    }

    public void Do(float duration)
    {
        if (_current != null)
            StopCoroutine(_current);
        _current = StartCoroutine(Freeze(duration));
    }

    private IEnumerator Freeze(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        _current = null;
    }
}
