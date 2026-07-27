using UnityEngine;
using UnityEngine.Rendering;

public enum HighlightFaction { Auto, Player, Enemy }

[DisallowMultipleComponent]
public class CharacterHighlightLayer : MonoBehaviour
{
    [Header("Highlight")]
    [Tooltip("Auto reads the Player/Enemy tag on this object.")]
    [SerializeField] private HighlightFaction faction = HighlightFaction.Auto;

    [Tooltip("Must match the rendering layers set on FresnelHighlightFeature.")]
    [SerializeField] private RenderingLayerMask playerLayer = 1u << 8;
    [SerializeField] private RenderingLayerMask enemyLayer = 1u << 9;

    private void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        uint bit = Resolve() == HighlightFaction.Player ? playerLayer : enemyLayer;
        if (bit == 0u) return;

        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].renderingLayerMask |= bit;
        }
    }
    
    public static void Ensure(GameObject target)
    {
        if (!target.TryGetComponent<CharacterHighlightLayer>(out _))
            target.AddComponent<CharacterHighlightLayer>();
    }

    private HighlightFaction Resolve()
    {
        if (faction != HighlightFaction.Auto)
            return faction;

        return CompareTag("Player") ? HighlightFaction.Player : HighlightFaction.Enemy;
    }
}
