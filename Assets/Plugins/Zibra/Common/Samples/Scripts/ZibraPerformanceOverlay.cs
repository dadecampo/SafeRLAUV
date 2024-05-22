using UnityEngine;
using com.zibra.common.Utilities;
using com.zibra.common.Solver;

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
        private RenderPipelineDetector.RenderPipeline CurrentRenderPipeline;
        private string ScriptingBackend;

        private void Start()
        {
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
            int startY = 30;
            int y = 0;
            int x = 0;

            GUI.Box(new Rect(START_X + x * BOX_WIDTH, startY + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT), FPSLabel);
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, startY + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"OS: {SystemInfo.operatingSystem}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, startY + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Graphics API: {SystemInfo.graphicsDeviceType}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, startY + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Unity version: {Application.unityVersion}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, startY + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Scripting backend: {ScriptingBackend}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, startY + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Render pipeline: {CurrentRenderPipeline}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, startY + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"{SystemInfo.renderingThreadingMode}");

            // Reset y, without removing offset
            startY = startY + y * BOX_HEIGHT;
            y = 0;

            foreach (var statReporter in StatReporterCollection.GetStatReporters())
            {
                foreach (var stat in statReporter.GetStats())
                {
                    GUI.Box(new Rect(START_X + x * BOX_WIDTH, startY + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT), stat);
                }
                x++;
                y = 0;
            }
        }
    }
}
