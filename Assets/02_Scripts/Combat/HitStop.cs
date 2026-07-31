using UnityEngine;

public class HitStop : MonoBehaviour
{
    private const float MaxFreezeDuration = 0.25f;
    private const float AbsoluteMaxFreeze = 0.5f;
    private const float Epsilon = 0.0001f;

    public static HitStop Instance { get; private set; }
    public static bool IsFrozen => Instance != null && Instance._isFrozen;

    private bool _isFrozen;
    private float _remaining;
    private float _restoreTimeScale = 1f;
    private float _freezeStartUnscaled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        var go = new GameObject("[HitStop]");
        go.AddComponent<HitStop>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Do(float duration)
    {
        if (duration <= Epsilon) return;
        
        if (!_isFrozen && Time.timeScale <= Epsilon) return;

        duration = Mathf.Min(duration, MaxFreezeDuration);

        if (_isFrozen)
        {
            _remaining = Mathf.Max(_remaining, duration);
            return;
        }

        _restoreTimeScale = Time.timeScale;
        _freezeStartUnscaled = Time.unscaledTime;
        _remaining = duration;
        _isFrozen = true;
        Time.timeScale = 0f;
    }

    public static void Cancel()
    {
        if (Instance != null)
            Instance.Release();
    }

    private void Update()
    {
        if (!_isFrozen) return;

        _remaining -= Time.unscaledDeltaTime;

        if (_remaining <= 0f || Time.unscaledTime - _freezeStartUnscaled > AbsoluteMaxFreeze)
            Release();
    }

    private void Release()
    {
        if (!_isFrozen) return;

        _isFrozen = false;

        if (Time.timeScale <= Epsilon)
            Time.timeScale = _restoreTimeScale;
    }

    private void OnDisable()
    {
        Release();
    }

    private void OnApplicationQuit()
    {
        Release();
    }

    private void OnDestroy()
    {
        Release();

        if (Instance == this)
            Instance = null;
    }
}
