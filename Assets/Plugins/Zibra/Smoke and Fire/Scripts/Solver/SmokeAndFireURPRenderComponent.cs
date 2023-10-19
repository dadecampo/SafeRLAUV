#if UNITY_PIPELINE_URP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using com.zibra.smoke_and_fire.Solver;

#if UNITY_2022_2_OR_NEWER
// Unity 2022 deprecates some functionality current version uses
// Will need partial rewrite of URP support to fix those
#pragma warning disable 0618
#endif

namespace com.zibra.smoke_and_fire
{
    /// <summary>
    ///     Component responsible for rendering smoke and fire in URP.
    /// </summary>
    public class SmokeAndFireURPRenderComponent : ScriptableRendererFeature
    {
#region Public Interface
        /// <summary>
        ///     URP specific rendering settings.
        /// </summary>
        [System.Serializable]
        public class SmokeAndFireURPRenderSettings
        {
            /// <summary>
            ///     Globally defines whether simulation renders in URP.
            /// </summary>
            public bool IsEnabled = true;
            /// <summary>
            ///     Injection point where we will insert rendering.
            /// </summary>
            /// <remarks>
            ///     In case of URP, this parameter will be used instead of
            ///     <see cref="Solver::ZibraSmokeAndFire::CurrentInjectionPoint">ZibraSmokeAndFire.CurrentInjectionPoint</see>.
            /// </remarks>
            public RenderPassEvent InjectionPoint = RenderPassEvent.AfterRenderingTransparents;
        }

        /// <summary>
        ///     See <see cref="SmokeAndFireURPRenderSettings"/>.
        /// </summary>
        // Must be called exactly "settings" so Unity shows this as render feature settings in editor
        public SmokeAndFireURPRenderSettings settings = new SmokeAndFireURPRenderSettings();

        /// <summary>
        ///     Creates URP ScriptableRendererFeature.
        /// </summary>
        public override void Create()
        {
        }

        /// <summary>
        ///     Adds scriptable render passes.
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!settings.IsEnabled)
            {
                return;
            }

            if (renderingData.cameraData.cameraType != CameraType.Game &&
                renderingData.cameraData.cameraType != CameraType.SceneView)
            {
                return;
            }

            Camera camera = renderingData.cameraData.camera;
            camera.depthTextureMode = DepthTextureMode.Depth;

            int simulationsToRenderCount = 0;
            int simulationsToUpscaleCount = 0;

            foreach (var instance in ZibraSmokeAndFire.AllInstances)
            {
                if (instance != null && instance.Initialized)
                {
                    simulationsToRenderCount++;
                    if (instance.EnableDownscale)
                    {
                        simulationsToUpscaleCount++;
                    }
                }
            }

            if (nativePasses == null || nativePasses.Length != simulationsToRenderCount)
            {
                nativePasses = new SmokeAndFireNativeRenderPass[simulationsToRenderCount];
                for (int i = 0; i < simulationsToRenderCount; ++i)
                {
                    nativePasses[i] = new SmokeAndFireNativeRenderPass(settings.InjectionPoint);
                }
            }

            if (smokeAndFireURPPasses == null || smokeAndFireURPPasses.Length != simulationsToRenderCount)
            {
                smokeAndFireURPPasses = new SmokeAndFireURPRenderPass[simulationsToRenderCount];
                for (int i = 0; i < simulationsToRenderCount; ++i)
                {
                    smokeAndFireURPPasses[i] = new SmokeAndFireURPRenderPass(settings.InjectionPoint);
                }
            }

            if (upscalePasses == null || upscalePasses.Length != simulationsToUpscaleCount)
            {
                upscalePasses = new SmokeAndFireUpscaleURPRenderPass[simulationsToUpscaleCount];
                for (int i = 0; i < simulationsToUpscaleCount; ++i)
                {
                    upscalePasses[i] = new SmokeAndFireUpscaleURPRenderPass(settings.InjectionPoint);
                }
            }

            int currentSmokeAndFirePass = 0;
            int currentUpscalePass = 0;

            foreach (var instance in ZibraSmokeAndFire.AllInstances)
            {
                if (instance != null && instance.IsRenderingEnabled() &&
                    ((camera.cullingMask & (1 << instance.gameObject.layer)) != 0))
                {
                    nativePasses[currentSmokeAndFirePass].smokeAndFire = instance;
                    nativePasses[currentSmokeAndFirePass].renderPassEvent = settings.InjectionPoint;
                    renderer.EnqueuePass(nativePasses[currentSmokeAndFirePass]);

                    smokeAndFireURPPasses[currentSmokeAndFirePass].smokeAndFire = instance;
#if UNITY_PIPELINE_URP_10_0_OR_HIGHER
                    smokeAndFireURPPasses[currentSmokeAndFirePass].ConfigureInput(ScriptableRenderPassInput.Color |
                                                                                  ScriptableRenderPassInput.Depth);
#endif

#if !UNITY_PIPELINE_URP_9_0_OR_HIGHER
                    smokeAndFireURPPasses[currentSmokeAndFirePass].Setup(renderer, ref renderingData);
#endif
                    smokeAndFireURPPasses[currentSmokeAndFirePass].renderPassEvent = settings.InjectionPoint;

                    renderer.EnqueuePass(smokeAndFireURPPasses[currentSmokeAndFirePass]);
                    currentSmokeAndFirePass++;
                    if (instance.EnableDownscale)
                    {
                        upscalePasses[currentUpscalePass].smokeAndFire = instance;

                        upscalePasses[currentUpscalePass].renderPassEvent = settings.InjectionPoint;

                        renderer.EnqueuePass(upscalePasses[currentUpscalePass]);
                        currentUpscalePass++;
                    }
                }
            }
        }
#endregion
#region Implementation details

        private class SmokeAndFireNativeRenderPass : ScriptableRenderPass
        {
            public ZibraSmokeAndFire smokeAndFire;

            public SmokeAndFireNativeRenderPass(RenderPassEvent injectionPoint)
            {
                renderPassEvent = injectionPoint;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (smokeAndFire && smokeAndFire.IsRenderingEnabled())
                {
                    Camera camera = renderingData.cameraData.camera;
                    camera.depthTextureMode = DepthTextureMode.Depth;
                    CommandBuffer cmd = CommandBufferPool.Get("ZibraSmokeAndFire.Render");

                    smokeAndFire.RenderCallBack(renderingData.cameraData.camera, renderingData.cameraData.renderScale);

                    smokeAndFire.RenderFluid(cmd, renderingData.cameraData.camera);

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }
        }

        private class SmokeAndFireURPRenderPass : ScriptableRenderPass
        {
            public ZibraSmokeAndFire smokeAndFire;

            RenderTargetIdentifier cameraColorTexture;

            static int upscaleColorTextureID = Shader.PropertyToID("ZibraSmokeAndFire_SmokeAndFireTempColorTexture");
            RenderTargetIdentifier upscaleColorTexture;

            public SmokeAndFireURPRenderPass(RenderPassEvent injectionPoint)
            {
                renderPassEvent = injectionPoint;
            }

#if UNITY_PIPELINE_URP_9_0_OR_HIGHER
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;
            }
#else
            public void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
            {
                cameraColorTexture = renderer.cameraColorTarget;
            }
#endif

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                if (smokeAndFire.EnableDownscale)
                {
                    RenderTextureDescriptor descriptor = cameraTextureDescriptor;

                    Vector2Int dimensions = new Vector2Int(descriptor.width, descriptor.height);
                    dimensions = smokeAndFire.ApplyDownscaleFactor(dimensions);
                    descriptor.width = dimensions.x;
                    descriptor.height = dimensions.y;

                    descriptor.msaaSamples = 1;

                    descriptor.colorFormat = RenderTextureFormat.ARGBHalf;
                    descriptor.depthBufferBits = 0;

                    cmd.GetTemporaryRT(upscaleColorTextureID, descriptor, FilterMode.Bilinear);

                    upscaleColorTexture = new RenderTargetIdentifier(upscaleColorTextureID);
                    ConfigureTarget(upscaleColorTexture);
                    ConfigureClear(ClearFlag.All, new Color(0, 0, 0, 0));
                }
                else
                {
                    ConfigureTarget(cameraColorTexture);
                    // ConfigureClear seems to be persistent, so need to reset it
                    ConfigureClear(ClearFlag.None, new Color(0, 0, 0, 0));
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                camera.depthTextureMode = DepthTextureMode.Depth;
                CommandBuffer cmd = CommandBufferPool.Get("ZibraSmokeAndFire.EffectParticles.Render");
                if (!smokeAndFire.EnableDownscale)
                {
                    cmd.SetRenderTarget(cameraColorTexture);
                }
                smokeAndFire.RenderParticlesNative(cmd, camera);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                cmd = CommandBufferPool.Get("ZibraSmokeAndFire.Render");
                if (!smokeAndFire.EnableDownscale)
                {
                    cmd.SetRenderTarget(cameraColorTexture);
                }
                smokeAndFire.RenderSmokeAndFireMain(cmd, camera);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

#if UNITY_PIPELINE_URP_9_0_OR_HIGHER
            public override void OnCameraCleanup(CommandBuffer cmd)
#else
            public override void FrameCleanup(CommandBuffer cmd)
#endif
            {
                if (smokeAndFire.EnableDownscale)
                {
                    cmd.ReleaseTemporaryRT(upscaleColorTextureID);
                }
            }
        }

        private class SmokeAndFireUpscaleURPRenderPass : ScriptableRenderPass
        {
            public ZibraSmokeAndFire smokeAndFire;

            static int upscaleColorTextureID = Shader.PropertyToID("ZibraSmokeAndFire_SmokeAndFireTempColorTexture");
            RenderTargetIdentifier upscaleColorTexture;

            public SmokeAndFireUpscaleURPRenderPass(RenderPassEvent injectionPoint)
            {
                renderPassEvent = injectionPoint;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                camera.depthTextureMode = DepthTextureMode.Depth;
                CommandBuffer cmd = CommandBufferPool.Get("ZibraSmokeAndFire.Render");

                upscaleColorTexture = new RenderTargetIdentifier(upscaleColorTextureID);
                smokeAndFire.UpscaleSmokeAndFireDirect(cmd, camera, upscaleColorTexture);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        // 1 pass per rendered simulation
        SmokeAndFireNativeRenderPass[] nativePasses;
        // 1 pass per rendered simulation
        SmokeAndFireURPRenderPass[] smokeAndFireURPPasses;
        // 1 pass per rendered simulation that have downscale enabled
        SmokeAndFireUpscaleURPRenderPass[] upscalePasses;
#endregion
    }
}

#if UNITY_2022_2_OR_NEWER
#pragma warning restore 0618
#endif

#endif // UNITY_PIPELINE_HDRP