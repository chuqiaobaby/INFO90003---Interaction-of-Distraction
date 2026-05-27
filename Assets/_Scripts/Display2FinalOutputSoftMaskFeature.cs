using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class Display2FinalOutputSoftMaskFeature : ScriptableRendererFeature
{
    private sealed class SoftMaskPass : ScriptableRenderPass
    {
        private static readonly int EnabledId = Shader.PropertyToID("_Display2FinalSoftMaskEnabled");
        private static readonly int WidthId = Shader.PropertyToID("_Display2FinalSoftMaskWidth");
        private static readonly int StrengthId = Shader.PropertyToID("_Display2FinalSoftMaskStrength");
        private static readonly int SoftnessId = Shader.PropertyToID("_Display2FinalSoftMaskSoftness");
        private static readonly int CornerBoostId = Shader.PropertyToID("_Display2FinalSoftMaskCornerBoost");
        private static readonly int VignetteStrengthId = Shader.PropertyToID("_Display2FinalSoftMaskVignetteStrength");

        private sealed class PassData
        {
            public TextureHandle Source;
            public Material Material;
        }

        private readonly Material material;

        public SoftMaskPass(Material material)
        {
            this.material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("Display2FinalOutputSoftMask");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (Shader.GetGlobalFloat(EnabledId) < 0.5f ||
                Shader.GetGlobalFloat(WidthId) <= 0.001f ||
                Shader.GetGlobalFloat(StrengthId) <= 0.001f)
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            TextureHandle source = resourceData.activeColorTexture;

            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            TextureHandle temp = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                descriptor,
                "_Display2FinalSoftMaskTemp",
                false,
                FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Display2FinalSoftMask",
                out PassData data,
                profilingSampler))
            {
                data.Source = source;
                data.Material = material;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(temp, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
                    Blitter.BlitTexture(context.cmd, passData.Source, new Vector4(1f, 1f, 0f, 0f), passData.Material, 0));
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Display2FinalSoftMaskCopy",
                out PassData data,
                profilingSampler))
            {
                data.Source = temp;
                data.Material = null;
                builder.UseTexture(temp, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
                    Blitter.BlitTexture(context.cmd, passData.Source, new Vector4(1f, 1f, 0f, 0f), 0, true));
            }
        }
    }

    private Material material;
    private SoftMaskPass pass;

    public override void Create()
    {
        Shader shader = Shader.Find("Hidden/Display2FinalOutputSoftMask");
        if (shader == null)
        {
            Debug.LogError("[Display2FinalOutputSoftMask] Shader 'Hidden/Display2FinalOutputSoftMask' not found.");
            return;
        }

        material = CoreUtils.CreateEngineMaterial(shader);
        pass = new SoftMaskPass(material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || pass == null)
        {
            return;
        }

        if (renderingData.cameraData.cameraType != CameraType.Game)
        {
            return;
        }

        if (renderingData.cameraData.camera.targetDisplay != 1)
        {
            return;
        }

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        if (material != null)
        {
            CoreUtils.Destroy(material);
        }
    }
}
