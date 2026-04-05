using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class OutputTextureRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PassSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        public string textureName = "_InputTexture";
    }

    public PassSettings settings = new PassSettings();

    // Pasul de randare adaptat pentru Unity 6 Render Graph
    class CustomRenderPass : ScriptableRenderPass
    {
        private readonly string textureName;

        public CustomRenderPass(string textureName)
        {
            this.textureName = textureName;
        }

        // Aceasta este metoda nouă cerută de Unity 6
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "OutputTexturePass";

            using (var builder = renderGraph.AddRasterRenderPass(passName, out PassData passData))
            {
                // Obținem datele camerei și resursele de adâncime
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                passData.textureName = textureName;
                
                // Îi spunem Render Graph-ului că avem nevoie de textura de adâncime a camerei
                builder.UseTexture(resourceData.cameraDepthTexture);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Mapăm textura de adâncime la numele pe care îl așteaptă shaderul
                    context.cmd.SetGlobalTexture(data.textureName, resourceData.cameraDepthTexture);
                });
            }
        }

        private class PassData
        {
            public string textureName;
        }

        // Metoda veche pentru compatibilitate (dacă Render Graph este dezactivat)
        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("OutputTexturePass");
            cmd.SetGlobalTexture(textureName, Shader.GetGlobalTexture("_CameraDepthTexture"));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(settings.textureName);
        m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}