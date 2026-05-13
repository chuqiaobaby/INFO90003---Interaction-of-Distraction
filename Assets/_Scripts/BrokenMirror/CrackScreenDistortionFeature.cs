using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// URP 17 / Unity 6 Renderer Feature.
/// After all transparent objects render (including LiquidGlass blobs), blits the
/// camera colour buffer through the crack-distortion shader so the entire frame is
/// warped by the crack pattern — not just the webcam background behind the blobs.
///
/// Setup: Add this feature to your URP Renderer asset.
/// MirrorFractureController pushes _GlobalMirrorState / _GlobalCrackStrength /
/// _GlobalDistortionStrength / _GlobalCrackTex as global shader properties each frame.
/// </summary>
public sealed class CrackScreenDistortionFeature : ScriptableRendererFeature
{
    // ── Inner pass ────────────────────────────────────────────────────────────

    sealed class DistortionPass : ScriptableRenderPass
    {
        static readonly int s_StrengthId = Shader.PropertyToID("_GlobalCrackStrength");

        // Per-pass data carried into the render-graph lambda
        class PassData
        {
            public TextureHandle src;
            public Material      mat;
        }

        readonly Material _mat;

        public DistortionPass(Material mat)
        {
            _mat = mat;
            renderPassEvent  = RenderPassEvent.AfterRenderingTransparents;
            profilingSampler = new ProfilingSampler("CrackScreenDistortion");
        }

        // ── RenderGraph path (Unity 6 / URP 17) ──────────────────────────────

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (Shader.GetGlobalFloat(s_StrengthId) < 0.01f) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData   = frameData.Get<UniversalCameraData>();

            TextureHandle src = resourceData.activeColorTexture;

            // Allocate a temporary texture matching the camera RT
            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            TextureHandle temp = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_CrackDistortTemp", false, FilterMode.Bilinear);

            // Pass 1: distort camera colour → temp using the crack shader
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "CrackDistort", out var data, profilingSampler))
            {
                data.src = src;
                data.mat = _mat;
                builder.UseTexture(src, AccessFlags.Read);
                builder.SetRenderAttachment(temp, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData d, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, 0));
            }

            // Pass 2: copy temp back to the active camera colour buffer
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "CrackDistortCopy", out var data, profilingSampler))
            {
                data.src = temp;
                data.mat = null;
                builder.UseTexture(temp, AccessFlags.Read);
                builder.SetRenderAttachment(src, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData d, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), 0, true));
            }
        }
    }

    // ── Feature lifecycle ─────────────────────────────────────────────────────

    Material      _mat;
    DistortionPass _pass;

    public override void Create()
    {
        Shader sh = Shader.Find("Hidden/CrackScreenDistortion");
        if (sh == null)
        {
            Debug.LogError("[CrackDistortion] Shader 'Hidden/CrackScreenDistortion' not found " +
                           "— make sure the shader asset is inside the project.");
            return;
        }
        _mat  = CoreUtils.CreateEngineMaterial(sh);
        _pass = new DistortionPass(_mat);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_mat == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        if (_mat != null) CoreUtils.Destroy(_mat);
    }
}
