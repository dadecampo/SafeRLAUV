using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_PIPELINE_URP
using UnityEngine.Rendering.Universal;
using System.Reflection;
using System.Collections.Generic;
#endif

namespace com.zibra.common.Utilities
{
    /// <summary>
    ///     Class responsible for querying data about Render Pipelines.
    /// </summary>
    public static class RenderPipelineDetector
    {
        /// <summary>
        ///     Type of render pipeline.
        /// </summary>
        public enum RenderPipeline
        {
            BuiltInRP,
            URP,
            HDRP
        }

        /// <summary>
        ///     Detects which render pipeline is currently used.
        /// </summary>
        /// <returns>
        ///     <see cref="RenderPipeline"/>.
        /// </returns>
        public static RenderPipeline GetRenderPipelineType()
        {
            if (GraphicsSettings.currentRenderPipeline)
            {
                if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition"))
                {
#if !UNITY_PIPELINE_HDRP
                    Debug.LogError("Current detected render pipeline is HDRP, but UNITY_PIPELINE_HDRP is not defined");
#endif
                    return RenderPipeline.HDRP;
                }
                else
                {
#if !UNITY_PIPELINE_URP
                    Debug.LogError("Current detected render pipeline is URP, but UNITY_PIPELINE_URP is not defined");
#endif
                    return RenderPipeline.URP;
                }
            }
            else
            {
                return RenderPipeline.BuiltInRP;
            }
        }

#if UNITY_PIPELINE_URP
        /// <summary>
        ///     Checks whether ZibraURPRenderComponent is missing in URP options.
        /// </summary>
        /// <returns>
        ///     True if current render pipeline is URP, but ZibraURPRenderComponent is missing,
        ///     and false otherwise.
        /// </returns>
        /// <remarks>
        ///     Please note that you may have different URP options for mobile and desktop,
        ///     but we can only check current options.
        /// </remarks>
        internal static bool IsURPMissingRenderComponent<T>()
            where T : ScriptableRendererFeature
        {
            if (GetRenderPipelineType() == RenderPipeline.URP)
            {
                // Getting non public list of render features via reflection
                var URPAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
                var field = typeof(ScriptableRenderer)
                                .GetField("m_RendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && URPAsset != null)
                {
                    var scriptableRendererFeatures =
                        field.GetValue(URPAsset.scriptableRenderer) as List<ScriptableRendererFeature>;
                    if (scriptableRendererFeatures != null)
                    {
                        foreach (var renderFeature in scriptableRendererFeatures)
                        {
                            if (renderFeature is T)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
#endif

        internal static bool IsURPMissingRenderComponent(string name)
        {
#if UNITY_PIPELINE_URP
            if (GetRenderPipelineType() == RenderPipeline.URP)
            {
                var URPAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
                var field = typeof(ScriptableRenderer)
                                .GetField("m_RendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && URPAsset != null)
                {
                    var scriptableRendererFeatures =
                        field.GetValue(URPAsset.scriptableRenderer) as List<ScriptableRendererFeature>;
                    if (scriptableRendererFeatures != null)
                    {
                        foreach (var renderFeature in scriptableRendererFeatures)
                        {
                            if (renderFeature.GetType().Name == name)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
#endif
            return false;
        }

        /// <summary>
        ///     Checks whether depth buffer is not enabled in URP options.
        /// </summary>
        /// <returns>
        ///     True if current render pipeline is URP, but depth buffer is not enabled,
        ///     and false otherwise.
        /// </returns>
        /// <remarks>
        ///     Please note that you may have differnet URP options for mobile and desktop,
        ///     but we can only check current options.
        /// </remarks>
        public static bool IsURPMissingDepthBuffer()
        {
#if UNITY_PIPELINE_URP
            if (GetRenderPipelineType() == RenderPipeline.URP)
            {
                // Getting non public list of render features via reflection
                var URPAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
                return URPAsset != null && !URPAsset.supportsCameraDepthTexture;
            }
#endif
            return false;
        }
    }
}
