using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class ScreenSpaceOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        [Tooltip("When in the frame the stack runs. After Opaques is usually best.")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        [Header("Outline")]
        public bool outlineEnabled = true;
        [ColorUsage(true, true)] public Color outlineColor = Color.black;
        [Range(0f, 8f)] public float thickness = 1.5f;
        [Tooltip("Sensitivity to depth edges (silhouettes/overlaps). Lower = more edges.")]
        [Min(0f)] public float depthThreshold = 0.02f;
        [Tooltip("Sensitivity to normal edges (creases/corners). Lower = more edges.")]
        [Min(0f)] public float normalThreshold = 0.4f;
        [Tooltip("Suppresses false edges at grazing angles (ground receding into distance).")]
        [Range(1f, 20f)] public float grazingSuppression = 4f;
        [Tooltip("0 = flat colored outline, 1 = outline tinted by the surface underneath.")]
        [Range(0f, 1f)] public float outlineTintFromScene = 0f;

        [Header("Posterize")]
        public bool posterizeEnabled = false;
        [Range(0f, 1f)] public float posterizeAmount = 1f;
        [Range(2f, 32f)] public float posterizeSteps = 6f;

        [Header("Halftone (shadows)")]
        public bool halftoneEnabled = false;
        [Range(0f, 1f)] public float halftoneAmount = 1f;
        [Tooltip("Dot grid density (pixels per cell).")]
        [Range(1f, 40f)] public float halftoneScale = 6f;
        [Tooltip("Luminance below which dots appear.")]
        [Range(0f, 1f)] public float halftoneThreshold = 0.5f;
        [Range(0.001f, 0.5f)] public float halftoneSoftness = 0.1f;
        [ColorUsage(true, true)] public Color halftoneColor = Color.black;

        [Header("Stylized Fog")]
        public bool fogEnabled = false;
        [Range(0f, 1f)] public float fogAmount = 1f;
        [ColorUsage(true, true)] public Color fogColor = new Color(0.5f, 0.6f, 0.8f, 1f);
        [Range(0f, 1f)] public float fogStart = 0.1f;
        [Range(0f, 1f)] public float fogEnd = 0.6f;
        [Range(1f, 16f)] public float fogBands = 4f;
    }

    [Tooltip("Assign Assets/06_Shader/ScreenSpaceOutline.shader here.")]
    [SerializeField] private Shader shader;

    public OutlineSettings settings = new OutlineSettings();

    private Material _material;
    private OutlinePass _pass;

    public override void Create()
    {
        if (shader == null)
            shader = Shader.Find("Hidden/Custom/Screen Space Outline");
        if (shader == null)
            return;

        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass = new OutlinePass(_material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null)
            return;

        var cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            return;

        if (!settings.outlineEnabled && !settings.posterizeEnabled &&
            !settings.halftoneEnabled && !settings.fogEnabled)
            return;

        _pass.renderPassEvent = settings.renderPassEvent;
        _pass.Setup(settings);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
    }
    
    private class OutlinePass : ScriptableRenderPass
    {
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int ThicknessId    = Shader.PropertyToID("_OutlineThickness");
        private static readonly int DepthThreshId   = Shader.PropertyToID("_DepthThreshold");
        private static readonly int NormalThreshId  = Shader.PropertyToID("_NormalThreshold");
        private static readonly int GrazingId       = Shader.PropertyToID("_GrazingFade");
        private static readonly int OutlineTintId   = Shader.PropertyToID("_OutlineTint");

        private static readonly int PosterizeAmountId = Shader.PropertyToID("_PosterizeAmount");
        private static readonly int PosterizeStepsId  = Shader.PropertyToID("_PosterizeSteps");

        private static readonly int HalftoneAmountId    = Shader.PropertyToID("_HalftoneAmount");
        private static readonly int HalftoneScaleId     = Shader.PropertyToID("_HalftoneScale");
        private static readonly int HalftoneThresholdId = Shader.PropertyToID("_HalftoneThreshold");
        private static readonly int HalftoneSoftId      = Shader.PropertyToID("_HalftoneSoft");
        private static readonly int HalftoneColorId     = Shader.PropertyToID("_HalftoneColor");

        private static readonly int FogAmountId = Shader.PropertyToID("_FogAmount");
        private static readonly int FogColorId  = Shader.PropertyToID("_FogColor");
        private static readonly int FogStartId  = Shader.PropertyToID("_FogStart");
        private static readonly int FogEndId    = Shader.PropertyToID("_FogEnd");
        private static readonly int FogBandsId  = Shader.PropertyToID("_FogBands");

        private const string k_PassName = "Screen Space Toon Post";

        private readonly Material _material;
        private OutlineSettings _settings;

        public OutlinePass(Material material)
        {
            _material = material;
            profilingSampler = new ProfilingSampler(k_PassName);
            requiresIntermediateTexture = true;
        }

        public void Setup(OutlineSettings settings)
        {
            _settings = settings;
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        private void ApplyMaterial()
        {
            var s = _settings;

            Color oc = s.outlineColor;
            if (!s.outlineEnabled) oc.a = 0f;
            _material.SetColor(OutlineColorId, oc);
            _material.SetFloat(ThicknessId, s.thickness);
            _material.SetFloat(DepthThreshId, s.depthThreshold);
            _material.SetFloat(NormalThreshId, s.normalThreshold);
            _material.SetFloat(GrazingId, s.grazingSuppression);
            _material.SetFloat(OutlineTintId, s.outlineTintFromScene);

            _material.SetFloat(PosterizeAmountId, s.posterizeEnabled ? s.posterizeAmount : 0f);
            _material.SetFloat(PosterizeStepsId, s.posterizeSteps);

            _material.SetFloat(HalftoneAmountId, s.halftoneEnabled ? s.halftoneAmount : 0f);
            _material.SetFloat(HalftoneScaleId, s.halftoneScale);
            _material.SetFloat(HalftoneThresholdId, s.halftoneThreshold);
            _material.SetFloat(HalftoneSoftId, s.halftoneSoftness);
            _material.SetColor(HalftoneColorId, s.halftoneColor);

            _material.SetFloat(FogAmountId, s.fogEnabled ? s.fogAmount : 0f);
            _material.SetColor(FogColorId, s.fogColor);
            _material.SetFloat(FogStartId, s.fogStart);
            _material.SetFloat(FogEndId, s.fogEnd);
            _material.SetFloat(FogBandsId, s.fogBands);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData   = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            ApplyMaterial();

            TextureHandle source = resourceData.activeColorTexture;

            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            TextureHandle destination =
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ToonPostTarget", false);

            var blitParams = new RenderGraphUtils.BlitMaterialParameters(source, destination, _material, 0);
            renderGraph.AddBlitPass(blitParams, k_PassName);

            renderGraph.AddBlitPass(destination, source, new Vector2(1f, 1f), new Vector2(0f, 0f),
                passName: k_PassName + " (Copy Back)");
        }
    }
}
