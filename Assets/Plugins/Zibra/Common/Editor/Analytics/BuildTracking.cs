
using com.zibra.common.Editor;
using com.zibra.common.Utilities;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.UIElements;

namespace com.zibra.common.Analytics
{
    internal class BuildTracking : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPostprocessBuild(BuildReport report)
        {
            Dictionary<string, object> eventProperties = new Dictionary<string, object>
            {
                { "Render_pipeline", RenderPipelineDetector.GetRenderPipelineType().ToString() },
                { "Built_platform", GetPlatformName(report.summary.platform) },
                { "AppleARKit", PackageTracker.IsPackageInstalled("com.unity.xr.arkit") },
                { "GoogleARCore", PackageTracker.IsPackageInstalled("com.unity.xr.arcore") },
                { "MagicLeap", PackageTracker.IsPackageInstalled("com.unity.xr.magicleap") },
                { "Oculus", PackageTracker.IsPackageInstalled("com.unity.xr.oculus") },
                { "OpenXR", PackageTracker.IsPackageInstalled("com.unity.xr.openxr") }
            };

#if ZIBRA_EFFECTS_OTP_VERSION
            if (PluginManager.IsAvailable(PluginManager.Effect.Liquid))
            {
                AnalyticsManager.GetInstance(PluginManager.Effect.Liquid).TrackEvent(new AnalyticsManager.AnalyticsEvent
                {
                    EventType = "Liquid_built",
                    Properties = eventProperties
                });
            }
            if (PluginManager.IsAvailable(PluginManager.Effect.Smoke))
            {
                AnalyticsManager.GetInstance(PluginManager.Effect.Smoke).TrackEvent(new AnalyticsManager.AnalyticsEvent
                {
                    EventType = "SF_built",
                    Properties = eventProperties
                });
            }
#else
            AnalyticsManager.GetInstance("effects").TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "Effects_built",
                Properties = eventProperties
            });
#endif
        }

        string GetPlatformName(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneOSX:
                    return "MacOS";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.iOS:
                    return "IOS";
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.WSAPlayer:
                    return "UWP";
                case BuildTarget.StandaloneLinux64:
                    return "Linux";
                default:
                    return "Unknown";
            }
        }
    }
}
