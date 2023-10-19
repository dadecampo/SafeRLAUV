using UnityEngine;
using com.zibra.liquid.Solver;
using com.zibra.smoke_and_fire.Solver;
using com.zibra.common.Utilities;

namespace com.zibra.common.Samples
{
    /// <summary>
    ///     Component for displaying FPS ans Liquid container stats
    ///     Will automatically find all enabled liquid containers
    /// </summary>
    internal class ZibraPerformanceOverlay : MonoBehaviour
    {
        private string FPSLabel = "";
        private int FrameCount;
        private float ElapsedTime;
        private ZibraLiquid[] Liquids;
        private ZibraSmokeAndFire[] Smokes;
        private RenderPipelineDetector.RenderPipeline CurrentRenderPipeline;
        private string ScriptingBackend;

        private void Start()
        {
            Liquids = FindObjectsOfType<ZibraLiquid>();
            Smokes = FindObjectsOfType<ZibraSmokeAndFire>();
            CurrentRenderPipeline = RenderPipelineDetector.GetRenderPipelineType();
#if ENABLE_MONO
            ScriptingBackend = "Mono";
#endif
#if ENABLE_IL2CPP
            ScriptingBackend = "IL2CPP";
#endif
        }

        private void Update()
        {
            // FPS calculation
            FrameCount++;
            ElapsedTime += Time.unscaledDeltaTime;
            if (ElapsedTime > 0.5f)
            {
                double frameRate = System.Math.Round(FrameCount / ElapsedTime);
                FrameCount = 0;
                ElapsedTime = 0;

                FPSLabel = "FPS: " + frameRate;
            }
        }

        private void OnGUI()
        {
            const int BOX_WIDTH = 220;
            const int BOX_HEIGHT = 25;
            const int START_X = 30;
            const int START_Y = 30 + BOX_HEIGHT * 6;
            int y = -6; // Show FPS above all instances
            int x = 0;

            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT), FPSLabel);
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"OS: {SystemInfo.operatingSystem}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Graphics API: {SystemInfo.graphicsDeviceType}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Unity version: {Application.unityVersion}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Scripting backend: {ScriptingBackend}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Render pipeline: {CurrentRenderPipeline}");

            foreach (var liquidInstance in Liquids)
            {
                if (!liquidInstance.isActiveAndEnabled)
                    continue;

                float ResolutionScale = liquidInstance.EnableDownscale ? liquidInstance.DownscaleFactor : 1.0f;
                float PixelCountScale = ResolutionScale * ResolutionScale;
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Instance: {liquidInstance.name}");
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Grid size: {liquidInstance.GridSize}");
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Render resolution: {ResolutionScale * 100.0f}%");
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Render pixel count: {PixelCountScale * 100.0f}%");
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Max particle count: {liquidInstance.MaxNumParticles}");
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Current particle count: {liquidInstance.CurrentParticleNumber}");
                x++;
                y = 0;
            }

            foreach (var smokeInstance in Smokes)
            {
                if (!smokeInstance.isActiveAndEnabled)
                    continue;

                float ResolutionScale = smokeInstance.EnableDownscale ? smokeInstance.DownscaleFactor : 1.0f;
                float PixelCountScale = ResolutionScale * ResolutionScale;
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Instance: {smokeInstance.name}");
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Grid size: {smokeInstance.GridSize}");
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Render resolution: {ResolutionScale * 100.0f}%");
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                        $"Render pixel count: {PixelCountScale * 100.0f}%");
                x++;
                y = 0;
            }
        }
    }
}
