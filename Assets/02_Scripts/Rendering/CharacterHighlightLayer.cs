using UnityEngine;
using UnityEngine.Rendering;

public enum HighlightFaction { Auto, Player, Enemy }

[DisallowMultipleComponent]
public class CharacterHighlightLayer : MonoBehaviour
{
    private static readonly int RampId = Shader.PropertyToID("_FresnelRamp");

    [Header("Highlight")]
    [Tooltip("Auto reads the Player/Enemy tag on this object.")]
    [SerializeField] private HighlightFaction faction = HighlightFaction.Auto;

    [Tooltip("Highlight starts on. Turn off for characters that should only light up on demand.")]
    [SerializeField] private bool activeOnStart = true;

    [Tooltip("Must match the rendering layers set on FresnelHighlightFeature.")]
    [SerializeField] private RenderingLayerMask playerLayer = 1u << 8;
    [SerializeField] private RenderingLayerMask enemyLayer = 1u << 9;
    [Tooltip("Used while an enemy telegraphs an attack. Ignored for the player.")]
    [SerializeField] private RenderingLayerMask windupLayer = 1u << 10;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _block;
    private bool _highlightActive;
    private bool _windupActive;

    public bool IsHighlightActive => _highlightActive;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _block = new MaterialPropertyBlock();

        SetBit(IdleBit, false);
        SetBit(windupLayer, false);
        _highlightActive = false;
        _windupActive = false;

        if (activeOnStart) SetHighlightActive(true);
    }

    public void SetHighlightActive(bool active)
    {
        if (_highlightActive == active) return;
        _highlightActive = active;

        if (!active)
        {
            SetBit(IdleBit, false);
            SetBit(windupLayer, false);
            SetWindupRamp(1f);
            return;
        }

        SetBit(_windupActive ? (uint)windupLayer : IdleBit, true);
    }

    public void SetWindup(bool active, float ramp = 0f)
    {
        if (Resolve() != HighlightFaction.Enemy) return;
        if (_windupActive == active) return;
        _windupActive = active;

        SetWindupRamp(active ? ramp : 1f);

        if (!_highlightActive) return;

        SetBit(IdleBit, !active);
        SetBit(windupLayer, active);
    }

    public void SetWindupRamp(float t)
    {
        if (_renderers == null) return;

        float value = Mathf.Clamp01(t);
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(_block);
            _block.SetFloat(RampId, value);
            r.SetPropertyBlock(_block);
        }
    }

    public static void Ensure(GameObject target)
    {
        if (!target.TryGetComponent<CharacterHighlightLayer>(out _))
            target.AddComponent<CharacterHighlightLayer>();
    }

    private uint IdleBit => Resolve() == HighlightFaction.Player ? playerLayer : enemyLayer;

    private void SetBit(uint bit, bool on)
    {
        if (bit == 0u || _renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;

            if (on) r.renderingLayerMask |= bit;
            else    r.renderingLayerMask &= ~bit;
        }
    }

    private HighlightFaction Resolve()
    {
        if (faction != HighlightFaction.Auto)
            return faction;

        return CompareTag("Player") ? HighlightFaction.Player : HighlightFaction.Enemy;
    }
}
