#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;
using com.zibra.liquid.Bridge;
using com.zibra.common.Utilities;
using com.zibra.common.Editor.SDFObjects;

namespace com.zibra.common.Analytics
{
    [InitializeOnLoad]
    internal static class ZibraEffectsAnalytics
    {
        const int REPORT_PERIOD = 12 * 60 * 60; // 12 hours
        const string ENGINE = "Unity";
        const int TRACKING_VERSION = 2;
        const int FRAME_UPDATE_PERIOD = 15 * 60 * 30; // 15 minutes if FPS = 30
        const int SESSION_TIMEOUT = 15 * 60;          // 15 minutes

        public static void TrackBuiltPlatform(string builtPlatform)
        {
            EditorPrefs.SetBool($"ZibraEffectsTracking_Common_Built{builtPlatform}", true);
        }

        public static void TrackConfiguration(string effect)
        {
            EditorPrefs.SetBool("ZibraEffectsTracking_" + effect + "_Configured", true);

            string sessionKeyName = "ZibraEffectsTracking_" + effect + "_LastActivity";
            string prefsKeyName = "ZibraEffectsTracking_" + effect + "_SessionTime";
            int currentTime = GetCurrentTimeAsUnixTimestamp();
            int lastActivity = SessionState.GetInt(sessionKeyName, currentTime);
            if (currentTime - lastActivity < SESSION_TIMEOUT)
            {
                int oldSessionTime = EditorPrefs.GetInt(prefsKeyName, 0);
                EditorPrefs.SetInt(prefsKeyName, oldSessionTime + currentTime - lastActivity);
            }
            SessionState.SetInt(sessionKeyName, currentTime);
        }

        public static void CheckSendPeriod()
        {
            int dateNow = GetCurrentTimeAsUnixTimestamp();

            if (!EditorPrefs.HasKey("ZibraEffectsTracking_Common_LastSentDate"))
            {
                EditorPrefs.SetInt("ZibraEffectsTracking_Common_LastSentDate", dateNow);
                return;
            }

            int lastSentDate = EditorPrefs.GetInt("ZibraEffectsTracking_Common_LastSentDate");

            if (dateNow - lastSentDate > REPORT_PERIOD)
            {
                ZibraEffectsAnalyticsStruct data = GetAnalyticsData();
                string jsonData = JsonUtility.ToJson(data);
                ZibraEffectsAnalyticsSender.SendAnalyticsData(jsonData);
            }
        }

        static int FrameCount = FRAME_UPDATE_PERIOD;

        private static void Update()
        {
            if (FrameCount == FRAME_UPDATE_PERIOD)
            {
                FrameCount = 0;
                CheckSendPeriod();
            }
            else
            {
                FrameCount++;
            }
        }

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            // Don't track something that is potentially build-machine
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.update += Update;
        }

        // Structure used with JSON parser
        private struct ZibraEffectsAnalyticsStruct
        {
            // Common
            public int TrackingVersion;
            public string LicenseKeys;
            public string HardwareId;
            public string PluginSKU;
            public string Engine;
            public string VersionNumber;
            public string DeveloperOS;
            public string EditorsGraphicsAPI;
            public string EngineVersion;
            public string RenderPipeline;
            public bool BuiltPlatformWindows;
            public bool BuiltPlatformMacOS;
            public bool BuiltPlatformLinux;
            public bool BuiltPlatformUWP;
            public bool BuiltPlatformAndroid;
            public bool BuiltPlatformiOS;

            // SDFs
            public bool SDF_Configured;
            public int SDF_SessionTime;

            // Liquids
            public bool Liquids_Used;
            public bool Liquids_Configured;
            public int Liquids_SessionTime;
            public bool Liquids_MeshRenderUsed;
            public bool Liquids_UnityRenderUsed;
            public bool Liquids_RenderDownscaleUsed;
            public int Liquids_MinGridNodesUsed;
            public int Liquids_MaxGridNodesUsed;
            public int Liquids_MinMaxParticleCount;
            public int Liquids_MaxMaxParticleCount;
            public int Liquids_MinGridResolutionUsed;
            public int Liquids_MaxGridResolutionUsed;
            public bool Liquids_ManipulatorEmitterAnalytic;
            public bool Liquids_ManipulatorEmitterNeural;
            public bool Liquids_ManipulatorEmitterSkinnedMesh;
            public bool Liquids_ManipulatorVoidAnalytic;
            public bool Liquids_ManipulatorVoidNeural;
            public bool Liquids_ManipulatorVoidSkinnedMesh;
            public bool Liquids_ManipulatorForceFieldAnalytic;
            public bool Liquids_ManipulatorForceFieldNeural;
            public bool Liquids_ManipulatorForceFieldSkinnedMesh;
            public bool Liquids_ManipulatorColliderAnalytic;
            public bool Liquids_ManipulatorColliderNeural;
            public bool Liquids_ManipulatorColliderSkinnedMesh;
            public bool Liquids_ManipulatorDetectorAnalytic;
            public bool Liquids_ManipulatorDetectorNeural;
            public bool Liquids_ManipulatorDetectorSkinnedMesh;
            public bool Liquids_ManipulatorSpeciesModifierAnalytic;
            public bool Liquids_ManipulatorSpeciesModifierNeural;
            public bool Liquids_ManipulatorSpeciesModifierSkinnedMesh;
            public bool Liquids_InitialBakedStateUsed;
            public bool Liquids_InitialBakedStateSaved;

            // Smoke & Fire
            public bool SmokeAndFire_Used;
            public bool SmokeAndFire_Configured;
            public int SmokeAndFire_SessionTime;
            public bool SmokeAndFire_RenderDownscaleUsed;
            public bool SmokeAndFire_SimulationModeSmokeUsed;
            public bool SmokeAndFire_SimulationModeColoredSmokeUsed;
            public bool SmokeAndFire_SimulationModeFireUsed;
            public int SmokeAndFire_MinGridResolutionUsed;
            public int SmokeAndFire_MaxGridResolutionUsed;
            public int SmokeAndFire_MinGridNodesUsed;
            public int SmokeAndFire_MaxGridNodesUsed;
            public bool SmokeAndFire_ManipulatorEmitterAnalytic;
            public bool SmokeAndFire_ManipulatorEmitterNeural;
            public bool SmokeAndFire_ManipulatorEmitterSkinnedMesh;
            public bool SmokeAndFire_ManipulatorVoidAnalytic;
            public bool SmokeAndFire_ManipulatorVoidNeural;
            public bool SmokeAndFire_ManipulatorVoidSkinnedMesh;
            public bool SmokeAndFire_ManipulatorColliderAnalytic;
            public bool SmokeAndFire_ManipulatorColliderNeural;
            public bool SmokeAndFire_ManipulatorColliderSkinnedMesh;
            public bool SmokeAndFire_ManipulatorDetectorAnalytic;
            public bool SmokeAndFire_ManipulatorDetectorNeural;
            public bool SmokeAndFire_ManipulatorDetectorSkinnedMesh;
            public bool SmokeAndFire_ManipulatorEffectParticleEmitterrAnalytic;
            public bool SmokeAndFire_ManipulatorEffectParticleEmitterNeural;
            public bool SmokeAndFire_ManipulatorEffectParticleEmitterSkinnedMesh;
            public bool SmokeAndFire_ManipulatorTextureEmitterAnalytic;
            public bool SmokeAndFire_ManipulatorTextureEmitterNeural;
            public bool SmokeAndFire_ManipulatorTextureEmitterSkinnedMesh;
            public bool SmokeAndFire_ManipulatorForceFieldAnalytic;
            public bool SmokeAndFire_ManipulatorForceFieldNeural;
            public bool SmokeAndFire_ManipulatorForceFieldSkinnedMesh;
        }

        private static ZibraEffectsAnalyticsStruct GetAnalyticsData()
        {
            ZibraEffectsAnalyticsStruct data = new ZibraEffectsAnalyticsStruct();

            // Common
            data.LicenseKeys = String.Join(",", ServerAuthManager.GetInstance().PluginLicenseKeys);
            data.HardwareId = LiquidBridge.GetHardwareIDWrapper();
            data.PluginSKU = "Pro";
            data.Engine = ENGINE;
            data.TrackingVersion = TRACKING_VERSION;
            data.VersionNumber = Effects.Version;
            data.DeveloperOS = SystemInfo.operatingSystemFamily.ToString();
            data.EditorsGraphicsAPI = SystemInfo.graphicsDeviceType.ToString();
            data.EngineVersion = Application.unityVersion.ToString();
            data.RenderPipeline = RenderPipelineDetector.GetRenderPipelineType().ToString();
            data.BuiltPlatformWindows = EditorPrefs.GetBool("ZibraEffectsTracking_Common_BuiltWindows", false);
            data.BuiltPlatformLinux = EditorPrefs.GetBool("ZibraEffectsTracking_Common_BuiltLinux", false);
            data.BuiltPlatformMacOS = EditorPrefs.GetBool("ZibraEffectsTracking_Common_BuiltMacOS", false);
            data.BuiltPlatformUWP = EditorPrefs.GetBool("ZibraEffectsTracking_Common_BuiltUWP", false);
            data.BuiltPlatformAndroid = EditorPrefs.GetBool("ZibraEffectsTracking_Common_BuiltAndroid", false);
            data.BuiltPlatformiOS = EditorPrefs.GetBool("ZibraEffectsTracking_Common_BuiltIOS", false);

            // SDF
            data.SDF_Configured = EditorPrefs.GetBool("ZibraEffectsTracking_SDF_Configured", false);
            data.SDF_SessionTime = EditorPrefs.GetInt("ZibraEffectsTracking_SDF_SessionTime", 0);

            // Liquids
            data.Liquids_Used = EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_Used", false);
            data.Liquids_Configured = EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_Configured", false);
            data.Liquids_SessionTime = EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_SessionTime", 0);
            data.Liquids_MeshRenderUsed = EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_MeshRendererUsed", false);
            data.Liquids_UnityRenderUsed = EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_UnityRendererUsed", false);
            data.Liquids_RenderDownscaleUsed =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_RenderDownscaleUsed", false);
            data.Liquids_MinGridNodesUsed =
                EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MinNodeCount", int.MaxValue);
            data.Liquids_MaxGridNodesUsed = EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MaxNodeCount", 0);
            data.Liquids_MinMaxParticleCount =
                EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MinMaxParticleCount", int.MaxValue);
            data.Liquids_MaxMaxParticleCount =
                EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MaxMaxParticleCount", 0);
            data.Liquids_MinGridResolutionUsed =
                EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MinGridResolution", int.MaxValue);
            data.Liquids_MaxGridResolutionUsed =
                EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MaxGridResolution", 0);
            data.Liquids_ManipulatorEmitterAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorEmitterAnalyticSDF", false);
            data.Liquids_ManipulatorEmitterNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorEmitterNeuralSDF", false);
            data.Liquids_ManipulatorEmitterSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorEmitterSkinnedMeshSDF", false);
            data.Liquids_ManipulatorVoidAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorVoidAnalyticSDF", false);
            data.Liquids_ManipulatorVoidNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorVoidNeuralSDF", false);
            data.Liquids_ManipulatorVoidSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorVoidSkinnedMeshSDF", false);
            data.Liquids_ManipulatorForceFieldAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorForceFieldAnalyticSDF", false);
            data.Liquids_ManipulatorForceFieldNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorForceFieldNeuralSDF", false);
            data.Liquids_ManipulatorForceFieldSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorForceFieldSkinnedMeshSDF", false);
            data.Liquids_ManipulatorColliderAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorColliderAnalyticSDF", false);
            data.Liquids_ManipulatorColliderNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorColliderNeuralSDF", false);
            data.Liquids_ManipulatorColliderSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorColliderSkinnedMeshSDF", false);
            data.Liquids_ManipulatorDetectorAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorDetectorAnalyticSDF", false);
            data.Liquids_ManipulatorDetectorNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorDetectorNeuralSDF", false);
            data.Liquids_ManipulatorDetectorSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorDetectorSkinnedMeshSDF", false);
            data.Liquids_ManipulatorSpeciesModifierAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorSpeciesModifierAnalyticSDF", false);
            data.Liquids_ManipulatorSpeciesModifierNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorSpeciesModifierNeuralSDF", false);
            data.Liquids_ManipulatorSpeciesModifierSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_ManipulatorSpeciesModifierSkinnedMeshSDF", false);
            data.Liquids_InitialBakedStateUsed =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_BakedStateUsed", false);
            data.Liquids_InitialBakedStateSaved =
                EditorPrefs.GetBool("ZibraEffectsTracking_Liquids_Liquids_BakedStateSaved", false);

            // Smoke & Fire
            data.SmokeAndFire_Used = EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_Used", false);
            data.SmokeAndFire_Configured = EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_Configured", false);
            data.SmokeAndFire_SessionTime = EditorPrefs.GetInt("ZibraEffectsTracking_SmokeAndFire_SessionTime", 0);
            data.SmokeAndFire_RenderDownscaleUsed =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_RenderDownscaleUsed", false);
            data.SmokeAndFire_SimulationModeSmokeUsed =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_SimulationModeSmokeUsed", false);
            data.SmokeAndFire_SimulationModeColoredSmokeUsed =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_SimulationModeColoredSmokeUsed", false);
            data.SmokeAndFire_SimulationModeFireUsed =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_SimulationModeFireUsed", false);
            data.SmokeAndFire_MinGridResolutionUsed =
                EditorPrefs.GetInt("ZibraEffectsTracking_SmokeAndFire_MinGridResolution", int.MaxValue);
            data.SmokeAndFire_MaxGridResolutionUsed =
                EditorPrefs.GetInt("ZibraEffectsTracking_SmokeAndFire_MaxGridResolution", 0);
            data.SmokeAndFire_MinGridNodesUsed =
                EditorPrefs.GetInt("ZibraEffectsTracking_SmokeAndFire_MinNodeCount", int.MaxValue);
            data.SmokeAndFire_MaxGridNodesUsed =
                EditorPrefs.GetInt("ZibraEffectsTracking_SmokeAndFire_MaxNodeCount", 0);
            data.SmokeAndFire_ManipulatorEmitterAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorEmitterAnalyticSDF", false);
            data.SmokeAndFire_ManipulatorEmitterNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorEmitterNeuralSDF", false);
            data.SmokeAndFire_ManipulatorEmitterSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorEmitterSkinnedMeshSDF", false);
            data.SmokeAndFire_ManipulatorVoidAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorVoidAnalyticSDF", false);
            data.SmokeAndFire_ManipulatorVoidNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorVoidNeuralSDF", false);
            data.SmokeAndFire_ManipulatorVoidSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorVoidSkinnedMeshSDF", false);
            data.SmokeAndFire_ManipulatorColliderAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorColliderAnalyticSDF", false);
            data.SmokeAndFire_ManipulatorColliderNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorColliderNeuralSDF", false);
            data.SmokeAndFire_ManipulatorColliderSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorColliderSkinnedMeshSDF", false);
            data.SmokeAndFire_ManipulatorDetectorAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorDetectorAnalyticSDF", false);
            data.SmokeAndFire_ManipulatorDetectorNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorDetectorNeuralSDF", false);
            data.SmokeAndFire_ManipulatorDetectorSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorDetectorSkinnedMeshSDF", false);
            data.SmokeAndFire_ManipulatorEffectParticleEmitterrAnalytic = EditorPrefs.GetBool(
                "ZibraEffectsTracking_SmokeAndFire_ManipulatorEffectParticleEmitterrAnalyticSDF", false);
            data.SmokeAndFire_ManipulatorEffectParticleEmitterNeural = EditorPrefs.GetBool(
                "ZibraEffectsTracking_SmokeAndFire_ManipulatorEffectParticleEmitterNeuralSDF", false);
            data.SmokeAndFire_ManipulatorEffectParticleEmitterSkinnedMesh = EditorPrefs.GetBool(
                "ZibraEffectsTracking_SmokeAndFire_ManipulatorEffectParticleEmitterSkinnedMeshSDF", false);
            data.SmokeAndFire_ManipulatorTextureEmitterAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorTextureEmitterAnalyticSDF", false);
            data.SmokeAndFire_ManipulatorTextureEmitterNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorTextureEmitterNeuralSDF", false);
            data.SmokeAndFire_ManipulatorTextureEmitterSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorTextureEmitterSkinnedMeshSDF", false);
            data.SmokeAndFire_ManipulatorForceFieldAnalytic =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorForceFieldAnalyticSDF", false);
            data.SmokeAndFire_ManipulatorForceFieldNeural =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorForceFieldNeuralSDF", false);
            data.SmokeAndFire_ManipulatorForceFieldSkinnedMesh =
                EditorPrefs.GetBool("ZibraEffectsTracking_SmokeAndFire_ManipulatorForceFieldSkinnedMeshSDF", false);

            return data;
        }

        public static void CleanAnalyticData()
        {
            // Common
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Common_BuiltWindows");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Common_BuiltMacOS");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Common_BuiltLinux");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Common_BuiltUWP");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Common_BuiltAndroid");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Common_BuiltIOS");

            // SDF
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SDF_Configured");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SDF_SessionTime");
            SessionState.EraseInt("ZibraEffectsTracking_SDF_LastActivity");

            // Liquids
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_Used");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_Configured");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_SessionTime");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_MeshRendererUsed");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_UnityRendererUsed");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_RenderDownscaleUsed");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_MinNodeCount");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_MaxNodeCount");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_MinMaxParticleCount");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_MaxMaxParticleCount");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_MinGridResolution");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_MaxGridResolution");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorEmitterAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorEmitterNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorEmitterSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorVoidAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorVoidNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorVoidSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorForceFieldAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorForceFieldNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorForceFieldSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorColliderAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorColliderrNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorColliderSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorDetectorAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorDetectorNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorDetectorSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorSpeciesModifierAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorSpeciesModifierNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_ManipulatorSpeciesModifierSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_BakedStateUsed");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_Liquids_Liquids_BakedStateSaved");
            SessionState.EraseInt("ZibraEffectsTracking_Liquids_LastActivity");

            // Smoke & Fire
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_Used");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_Configured");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_SessionTime");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_RenderDownscaleUsed");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_SimulationModeSmokeUsed");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_SimulationModeColoredSmokeUsed");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_SimulationModeFireUsed");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_MinGridResolution");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_MaxGridResolution");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_MinNodeCount");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_MaxNodeCount");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorEmitterAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorEmitterNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorEmitterSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorVoidAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorVoidNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorVoidSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorColliderAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorColliderrNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorColliderSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorDetectorAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorDetectorNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorDetectorSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorEffectParticleEmitterrAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorEffectParticleEmitterNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorEffectParticleEmitterSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorTextureEmitterAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorTextureEmitterNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorTextureEmitterSkinnedMeshSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorForceFieldAnalyticSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorForceFieldNeuralSDF");
            EditorPrefs.DeleteKey("ZibraEffectsTracking_SmokeAndFire_ManipulatorForceFieldSkinnedMeshSDF");
            SessionState.EraseInt("ZibraEffectsTracking_SmokeAndFire_LastActivity");

            EditorPrefs.SetInt("ZibraEffectsTracking_Common_LastSentDate", GetCurrentTimeAsUnixTimestamp());
        }

        public static int GetCurrentTimeAsUnixTimestamp()
        {
            return (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }

    internal class ZibraLiquidBuildPostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder
        {
            get {
                return 0;
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            switch (report.summary.platform)
            {
            case BuildTarget.StandaloneOSX:
                ZibraEffectsAnalytics.TrackBuiltPlatform("MacOS");
                break;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                ZibraEffectsAnalytics.TrackBuiltPlatform("Windows");
                break;
            case BuildTarget.iOS:
                ZibraEffectsAnalytics.TrackBuiltPlatform("IOS");
                break;
            case BuildTarget.Android:
                ZibraEffectsAnalytics.TrackBuiltPlatform("Android");
                break;
            case BuildTarget.WSAPlayer:
                ZibraEffectsAnalytics.TrackBuiltPlatform("UWP");
                break;
            case BuildTarget.StandaloneLinux64:
                ZibraEffectsAnalytics.TrackBuiltPlatform("Linux");
                break;
            }
        }
    }

    internal static class ZibraEffectsAnalyticsSender
    {
        const string ANALYTIC_API_URL = "https://analytics.zibra.ai/api/pluginUsageAnalytics";
        const int SUCCESS_CODE = 201;

        private static UnityWebRequestAsyncOperation request;

        public static void SendAnalyticsData(string jsonData)
        {
            if (request != null)
            {
                return;
            }
#if UNITY_2022_2_OR_NEWER
            request = UnityWebRequest.PostWwwForm(ANALYTIC_API_URL, jsonData).SendWebRequest();
#else
            request = UnityWebRequest.Post(ANALYTIC_API_URL, jsonData).SendWebRequest();
#endif
            request.completed += UpdateRequest;
        }

        private static void UpdateRequest(AsyncOperation obj)
        {
            if (request == null || !request.isDone)
            {
                return;
            }

#if UNITY_2020_2_OR_NEWER
            if (request.webRequest.result != UnityWebRequest.Result.Success)
#else
            if (request.webRequest.isHttpError || request.webRequest.isNetworkError)
#endif
            {
                request.webRequest.Dispose();
                request = null;
                return;
            }

            if (request.webRequest.responseCode != SUCCESS_CODE)
            {
                request.webRequest.Dispose();
                request = null;
                return;
            }

            ZibraEffectsAnalytics.CleanAnalyticData();
            request.webRequest.Dispose();
            request = null;
        }
    }
}
#endif
