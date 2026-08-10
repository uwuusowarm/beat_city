using UnityEngine;

[DisallowMultipleComponent]
public class SpeedLinesEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem speedLines;

    [Header("Emitter")]
    [Tooltip("Where the lines spawn, as a world axis offset from this transform. Written onto the emitter every frame, so the effect stays on the character no matter where the child object was dragged in the scene view")]
    [SerializeField] private Vector3 emitterOffset = new Vector3(0f, 0.25f, 0f);

    [Header("Teleport Guard")]
    [Tooltip("Movement faster than this in units per second is treated as a teleport instead of travel. That frame is skipped so rate over distance does not dump a clump of lines at the snap position")]
    [SerializeField] private float teleportSpeed = 150f;

    private PlayerSettings settings;

    private ParticleSystem.MainModule main;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystemRenderer lineRenderer;

    private float baseRateOverDistance;
    private float baseLengthScale;

    private Vector3 lastPosition;
    private bool isActive;
    private bool isConfigured;

    public bool IsActive => isActive;

    private bool IsEnabledInSettings => settings == null || settings.speedLinesEnabled;

    private float MinSpeed => settings != null ? settings.speedLinesMinSpeed : 8f;
    private float Density => settings != null ? settings.speedLinesDensity : 1f;
    private float LengthScale => settings != null ? settings.speedLinesLengthScale : 1f;
    private Color Tint => settings != null ? settings.speedLinesColor : Color.white;

    private void Awake()
    {
        settings = SettingsResolver.ResolvePlayerSettings();

        if (settings == null)
        {
            Debug.LogWarning("[SpeedLinesEffect] No PlayerSettings found (provider/resources).");
        }
    }

    private void Start()
    {
        if (speedLines == null)
            speedLines = GetComponentInChildren<ParticleSystem>(true);

        if (speedLines == null)
        {
            Debug.LogWarning("[SpeedLinesEffect] No ParticleSystem assigned or found in children - speed lines stay off.", this);
            return;
        }

        main = speedLines.main;
        emission = speedLines.emission;
        lineRenderer = speedLines.GetComponent<ParticleSystemRenderer>();

        baseRateOverDistance = emission.rateOverDistanceMultiplier;
        baseLengthScale = lineRenderer != null ? lineRenderer.lengthScale : 1f;

        isConfigured = true;

        emission.enabled = false;
        speedLines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void Activate()
    {
        if (!isConfigured || !IsEnabledInSettings) return;

        ApplySettings();

        speedLines.transform.position = transform.position + emitterOffset;

        lastPosition = transform.position;
        isActive = true;

        emission.enabled = false;
        speedLines.Play(true);
    }

    public void Deactivate()
    {
        if (!isConfigured) return;

        isActive = false;
        emission.enabled = false;
        speedLines.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void ApplySettings()
    {
        emission.rateOverDistanceMultiplier = baseRateOverDistance * Density;
        main.startColor = Tint;

        if (lineRenderer != null)
            lineRenderer.lengthScale = baseLengthScale * LengthScale;
    }

    private void LateUpdate()
    {
        if (!isActive) return;

        if (!IsEnabledInSettings)
        {
            Deactivate();
            return;
        }

        float delta = Time.unscaledDeltaTime;
        if (delta <= 0f) return;

        Vector3 moved = transform.position - lastPosition;
        lastPosition = transform.position;

        speedLines.transform.position = transform.position + emitterOffset;

        float distance = moved.magnitude;
        float speed = distance / delta;

        if (speed < MinSpeed || speed > teleportSpeed)
        {
            emission.enabled = false;
            return;
        }

        emission.enabled = true;
        speedLines.transform.rotation = Quaternion.LookRotation(-moved / distance);
    }

    private void OnDisable()
    {
        Deactivate();
    }
}
