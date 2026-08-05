using UnityEngine;

public class HitStop : MonoBehaviour
{
    private const float MaxFreezeDuration = 0.25f;
    private const float MaxSlowMotionDuration = 0.6f;
    private const float AbsoluteMaxFreeze = 0.5f;
    private const float AbsoluteMaxActive = AbsoluteMaxFreeze + MaxSlowMotionDuration;
    private const float Epsilon = 0.0001f;

    public static HitStop Instance { get; private set; }
    public static bool IsFrozen => Instance != null && Instance._freezeRemaining > 0f;
    public static bool IsSlowMotion => Instance != null && Instance._isActive && Instance._freezeRemaining <= 0f;

    private bool _isActive;
    private float _freezeRemaining;
    private float _slowRemaining;
    private float _slowScale = 1f;
    private float _restoreTimeScale = 1f;
    private float _appliedTimeScale = -1f;
    private float _activeStartUnscaled;
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
        if (!BeginOrExtend()) return;

        if (_freezeRemaining <= 0f)
            _freezeStartUnscaled = Time.unscaledTime;

        _freezeRemaining = Mathf.Max(_freezeRemaining, Mathf.Min(duration, MaxFreezeDuration));
        ApplyTimeScale();
    }
    
    public void DoSlowMotion(float duration, float timeScale)
    {
        if (duration <= Epsilon) return;

        timeScale = Mathf.Clamp(timeScale, 0.01f, 1f);
        if (timeScale >= 1f - Epsilon) return;

        if (!BeginOrExtend()) return;

        duration = Mathf.Min(duration, MaxSlowMotionDuration);

        if (_slowRemaining > 0f)
        {
            _slowRemaining = Mathf.Max(_slowRemaining, duration);
            _slowScale = Mathf.Min(_slowScale, timeScale);
        }
        else
        {
            _slowRemaining = duration;
            _slowScale = timeScale;
        }

        ApplyTimeScale();
    }

    public static void Cancel()
    {
        if (Instance != null)
            Instance.Release();
    }

    private bool BeginOrExtend()
    {
        if (_isActive) return true;

        if (Time.timeScale <= Epsilon) return false;

        _restoreTimeScale = Time.timeScale;
        _activeStartUnscaled = Time.unscaledTime;
        _isActive = true;
        return true;
    }

    private void ApplyTimeScale()
    {
        float target = _freezeRemaining > 0f ? 0f : _restoreTimeScale * _slowScale;

        if (Mathf.Approximately(target, _appliedTimeScale)) return;

        _appliedTimeScale = target;
        Time.timeScale = target;
    }

    private void Update()
    {
        if (!_isActive) return;

        float delta = Time.unscaledDeltaTime;

        if (_freezeRemaining > 0f)
        {
            _freezeRemaining -= delta;

            if (Time.unscaledTime - _freezeStartUnscaled > AbsoluteMaxFreeze)
                _freezeRemaining = 0f;
        }
        else
        {
            _slowRemaining -= delta;
        }

        if ((_freezeRemaining <= 0f && _slowRemaining <= 0f) ||
            Time.unscaledTime - _activeStartUnscaled > AbsoluteMaxActive)
        {
            Release();
            return;
        }

        ApplyTimeScale();
    }

    private void Release()
    {
        if (!_isActive) return;

        _isActive = false;
        _freezeRemaining = 0f;
        _slowRemaining = 0f;
        _slowScale = 1f;

        if (Mathf.Approximately(Time.timeScale, _appliedTimeScale))
            Time.timeScale = _restoreTimeScale;

        _appliedTimeScale = -1f;
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
