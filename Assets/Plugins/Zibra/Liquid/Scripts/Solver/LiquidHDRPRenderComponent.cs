#if UNITY_PIPELINE_HDRP

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using com.zibra.liquid.Solver;

namespace com.zibra.liquid
{
    internal class LiquidHDRPRenderComponent : CustomPassVolume
    {
        internal class FluidHDRPRender : CustomPass
        {
            public ZibraLiquid liquid;

            protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
            {
            }

            protected override void Execute(CustomPassContext ctx)
            {
                if (liquid && liquid.IsRenderingEnabled())
                {
                    RTHandle cameraColor = ctx.cameraColorBuffer;
                    RTHandle cameraDepth = ctx.cameraDepthBuffer;

                    HDCamera hdCamera = ctx.hdCamera;
                    CommandBuffer cmd = ctx.cmd;

                    if (liquid.IsCameraFiltered(hdCamera.camera))
                        return;

                    if ((hdCamera.camera.cullingMask & (1 << liquid.gameObject.layer)) ==
                        0) // fluid gameobject layer is not in the culling mask of the camera
                        return;

                    float scale = (float)(hdCamera.actualWidth) / hdCamera.camera.pixelWidth;
                    Vector2 scaleVector = new Vector2(scale, scale);

                    liquid.RenderCallBack(hdCamera.camera, scale);

                    var depth = Shader.PropertyToID("ZibraLiquidCameraDepthCopy");
                    cmd.GetTemporaryRT(depth, hdCamera.actualWidth, hdCamera.actualHeight, 32, FilterMode.Point,
                                       RenderTextureFormat.RFloat);

                    // copy screen to background
                    if (liquid.IsBackgroundCopyNeeded(hdCamera.camera))
                    {
                        cmd.Blit(cameraColor, liquid.CameraResourcesMap[hdCamera.camera].Background, scaleVector,
                                 Vector2.zero, 0, 0);
                    }
                    // blit depth to temp RT
                    CoreUtils.SetRenderTarget(cmd, depth);
                    Vector4 depthCopyScaleBias = new Vector4((float)(hdCamera.camera.pixelWidth) / cameraDepth.rt.width, (float)(hdCamera.camera.pixelHeight) / cameraDepth.rt.height, 0.0f, 0.0f);
                    Blitter.BlitTexture(cmd, cameraDepth, depthCopyScaleBias, 0, false);

                    Rect viewport = new Rect(0, 0, hdCamera.actualWidth, hdCamera.actualHeight);

                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
                    {
                        cmd.SetRenderTarget(liquid.Color0, liquid.Depth);
                        cmd.ClearRenderTarget(true, true, Color.clear);
                    }

                    // bind temp depth RT
                    cmd.SetGlobalTexture("_CameraDepthTexture", depth);
                    liquid.RenderLiquidNative(cmd, hdCamera.camera, viewport);

                    liquid.RenderFluid(cmd, hdCamera.camera, cameraColor, cameraDepth, viewport);
                    cmd.ReleaseTemporaryRT(depth);
                }
            }

            protected override void Cleanup()
            {
            }
        }

        public FluidHDRPRender fluidPass;
    }
}

#endif // UNITY_PIPELINE_HDRP