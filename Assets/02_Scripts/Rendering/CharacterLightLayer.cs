using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class CharacterLightLayer : MonoBehaviour
{
    [Tooltip("Must match the Rendering Layer Mask on the character key light.")]
    [SerializeField] private RenderingLayerMask lightLayer = 1u << 11;

    private void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        uint bit = lightLayer;
        if (bit == 0u) return;

        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            r.renderingLayerMask |= bit;
        }
    }
}
