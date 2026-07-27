using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class FresnelHighlightFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class FactionSettings
    {
        public bool enabled = true;
        [Tooltip("Rendering Layer of this faction. Must match CharacterHighlightLayer on the " +
                 "characters. Names live in Project Settings > Tags and Layers > Rendering Layers. " +
                 "Leave empty and this faction is skipped.")]
        public RenderingLayerMask renderingLayer = 0u;
        [ColorUsage(true, true)] public Color color = Color.cyan;
        [Tooltip("Higher = the rim hugs the silhouette more tightly.")]
        [Range(0.5f, 8f)] public float power = 2f;
        [Tooltip("How far the rim reaches inward from the silhouette.")]
        [Range(0f, 1f)] public float threshold = 0.5f;
        [Range(0.001f, 1f)] public float smoothness = 0.25f;
        [Range(0f, 4f)] public float intensity = 1f;
    }

    [Tooltip("Assign Assets/06_Shader/FresnelHighlight.shader here.")]
    [SerializeField] private Shader shader;

    [Tooltip("Default runs after the screen space toon post stack, so the glow stays smooth. " +
             "Set to After Rendering Opaques and move this feature above ScreenSpaceOutlineFeature " +
             "instead to have the glow posterized along with the rest of the toon look.")]
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

    [Tooltip("Shader passes the characters are pulled in by. Keep this to a single tag: a shader " +
             "matching two tags is drawn twice, which doubles the additive glow (Custom/Toon Lit " +
             "has both UniversalForward and SRPDefaultUnlit).")]
    public string[] shaderTags = { "UniversalForward" };

    public FactionSettings player = new FactionSettings
    {
        renderingLayer = 1u << 8,
        color = new Color(0.2f, 0.8f, 1f)
    };

    public FactionSettings enemy = new FactionSettings
    {
        renderingLayer = 1u << 9,
        color = new Color(1f, 0.2f, 0.2f)
    };

    private Material _playerMaterial;
    private Material _enemyMaterial;
    private FresnelPass _playerPass;
    private FresnelPass _enemyPass;
    private readonly List<ShaderTagId> _shaderTagIds = new List<ShaderTagId>();

    public override void Create()
    {
        if (shader == null)
            shader = Shader.Find("Hidden/Custom/Fresnel Highlight");
        if (shader == null)
            return;
        
        _playerMaterial = CoreUtils.CreateEngineMaterial(shader);
        _enemyMaterial  = CoreUtils.CreateEngineMaterial(shader);

        _playerPass = new FresnelPass(_playerMaterial, "Fresnel Highlight (Player)");
        _enemyPass  = new FresnelPass(_enemyMaterial, "Fresnel Highlight (Enemy)");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_playerPass == null || _enemyPass == null)
            return;

        var cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            return;

        RebuildShaderTags();
        if (_shaderTagIds.Count == 0)
            return;

        if (player.enabled)
        {
            _playerPass.renderPassEvent = renderPassEvent;
            _playerPass.Setup(player, _shaderTagIds);
            renderer.EnqueuePass(_playerPass);
        }

        if (enemy.enabled)
        {
            _enemyPass.renderPassEvent = renderPassEvent;
            _enemyPass.Setup(enemy, _shaderTagIds);
            renderer.EnqueuePass(_enemyPass);
        }
    }
    
    private void RebuildShaderTags()
    {
        _shaderTagIds.Clear();
        if (shaderTags == null)
            return;

        for (int i = 0; i < shaderTags.Length; i++)
        {
            if (!string.IsNullOrEmpty(shaderTags[i]))
                _shaderTagIds.Add(new ShaderTagId(shaderTags[i]));
        }
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_playerMaterial);
        CoreUtils.Destroy(_enemyMaterial);
    }

    private class FresnelPass : ScriptableRenderPass
    {
        private static readonly int ColorId      = Shader.PropertyToID("_FresnelColor");
        private static readonly int PowerId      = Shader.PropertyToID("_FresnelPower");
        private static readonly int ThresholdId  = Shader.PropertyToID("_FresnelThreshold");
        private static readonly int SmoothnessId = Shader.PropertyToID("_FresnelSmoothness");
        private static readonly int IntensityId  = Shader.PropertyToID("_FresnelIntensity");

        private class PassData
        {
            public RendererListHandle rendererList;
        }

        private readonly Material _material;
        private readonly string _passName;
        private FilteringSettings _filteringSettings;
        private FactionSettings _settings;
        private List<ShaderTagId> _shaderTagIds;

        public FresnelPass(Material material, string passName)
        {
            _material = material;
            _passName = passName;
            profilingSampler = new ProfilingSampler(passName);
            _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
        }

        public void Setup(FactionSettings settings, List<ShaderTagId> shaderTagIds)
        {
            _settings = settings;
            _shaderTagIds = shaderTagIds;
            _filteringSettings.renderingLayerMask = settings.renderingLayer;
        }

        private void ApplyMaterial()
        {
            var s = _settings;
            _material.SetColor(ColorId, s.color);
            _material.SetFloat(PowerId, s.power);
            _material.SetFloat(ThresholdId, s.threshold);
            _material.SetFloat(SmoothnessId, s.smoothness);
            _material.SetFloat(IntensityId, s.intensity);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_filteringSettings.renderingLayerMask == 0u)
                return;

            var resourceData  = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData    = frameData.Get<UniversalCameraData>();
            var lightData     = frameData.Get<UniversalLightData>();

            ApplyMaterial();

            var drawingSettings = CreateDrawingSettings(_shaderTagIds, renderingData, cameraData,
                                                       lightData, cameraData.defaultOpaqueSortFlags);
            drawingSettings.overrideMaterial = _material;
            drawingSettings.overrideMaterialPassIndex = 0;
            drawingSettings.perObjectData = PerObjectData.None;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                       _passName, out var passData, profilingSampler))
            {
                var listParams = new RendererListParams(renderingData.cullResults,
                                                        drawingSettings, _filteringSettings);
                passData.rendererList = renderGraph.CreateRendererList(listParams);

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    ctx.cmd.DrawRendererList(data.rendererList));
            }
        }
    }
}
