#if UNITY_PIPELINE_HDRP

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using com.zibra.smoke_and_fire.Solver;

namespace com.zibra.smoke_and_fire
{
    internal class SmokeAndFireHDRPRenderComponent : CustomPassVolume
    {
        internal class FluidHDRPRender : CustomPass
        {
            public ZibraSmokeAndFire smokeAndFire;

#if UNITY_PIPELINE_HDRP_9_0_OR_HIGHER
            protected override void Execute(CustomPassContext ctx)
#else
            protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera,
                                            CullingResults cullingResult)
#endif
            {
                if (smokeAndFire && smokeAndFire.IsRenderingEnabled())
                {

                    RTHandle cameraColor, cameraDepth;
#if UNITY_PIPELINE_HDRP_9_0_OR_HIGHER
                    cameraColor = ctx.cameraColorBuffer;
                    cameraDepth = ctx.cameraDepthBuffer;

                    HDCamera hdCamera = ctx.hdCamera;
                    CommandBuffer cmd = ctx.cmd;
#else
                    GetCameraBuffers(out cameraColor, out cameraDepth);
#endif

                    if ((hdCamera.camera.cullingMask & (1 << smokeAndFire.gameObject.layer)) ==
                        0) // fluid gameobject layer is not in the culling mask of the camera
                        return;

                    float scale = (float)(hdCamera.actualWidth) / hdCamera.camera.pixelWidth;

                    smokeAndFire.RenderCallBack(hdCamera.camera, scale);

                    Rect viewport = new Rect(0, 0, hdCamera.actualWidth, hdCamera.actualHeight);

                    var exposure = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.Exposure);
                    cmd.SetGlobalTexture("_CameraExposureTexture", exposure);

                    bool isTextureArray = cameraColor.rt.dimension == TextureDimension.Tex2DArray;
                    if (isTextureArray)
                    {
                        smokeAndFire.cameraResources[hdCamera.camera]
                            .smokeAndFireMaterial.currentMaterial.EnableKeyword("INPUT_2D_ARRAY");
                        smokeAndFire.cameraResources[hdCamera.camera]
                            .smokeShadowProjectionMaterial.currentMaterial.EnableKeyword("INPUT_2D_ARRAY");
                    }
                    else
                    {
                        smokeAndFire.cameraResources[hdCamera.camera]
                            .smokeAndFireMaterial.currentMaterial.DisableKeyword("INPUT_2D_ARRAY");
                        smokeAndFire.cameraResources[hdCamera.camera]
                            .smokeShadowProjectionMaterial.currentMaterial.DisableKeyword("INPUT_2D_ARRAY");
                    }

                    smokeAndFire.RenderParticlesNative(cmd, hdCamera.camera, isTextureArray);
                    smokeAndFire.RenderFluid(cmd, hdCamera.camera, cameraColor, cameraDepth, viewport);
                }
            }
        }

        public FluidHDRPRender fluidPass;
    }
}

#endif // UNITY_PIPELINE_HDRP