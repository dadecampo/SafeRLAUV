using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System;
using System.Collections.Generic;
using com.zibra.common.Utilities;
using com.zibra.common.Editor.SDFObjects;

namespace com.zibra.common.Editor.Menus
{
    internal static class Helpers
    {
        static string[] GetImportedRenderPipelines()
        {
            return new string[] {
                "BuiltInRP",
#if UNITY_PIPELINE_HDRP
                "HDRP",
#endif
#if UNITY_PIPELINE_URP
                "URP",
#endif
            };
        }

        static string[] GetWarnings()
        {
            List<string> result = new List<string>();
            var needed_components = new List<string> { "LiquidURPRenderComponent", "SmokeAndFireURPRenderComponent" };
            foreach (var component in needed_components)
            {
                if (RenderPipelineDetector.IsURPMissingRenderComponent(component))
                {
                    result.Add(component + " is missing!");
                }
            }
            return result.ToArray();
        }

        [Serializable]
        internal class EffectLicenseStatus
        {
            public string EffectName;
            public string LicenseStatus;
        }

        static EffectLicenseStatus[] GetLicenseStatuses()
        {
            var result = new EffectLicenseStatus[(int)ServerAuthManager.Effect.Count];

            for (int i = 0; i < (int)ServerAuthManager.Effect.Count; ++i)
            {
                var effect = (ServerAuthManager.Effect)i;
                result[i] = new EffectLicenseStatus();
                result[i].EffectName = effect.ToString();
                result[i].LicenseStatus = ServerAuthManager.GetInstance().GetStatus(effect).ToString();
            }

            return result;
        }

        [Serializable]
        class DiagInfo
        {
            public string Version = Effects.Version;
            public string UnityVersion = Application.unityVersion;
            public string RenderPipeline = "" + RenderPipelineDetector.GetRenderPipelineType();
            public string[] ImportedRenderPipelines = GetImportedRenderPipelines();
            public string OS = SystemInfo.operatingSystem;
            public string TargetPlatform = "" + EditorUserBuildSettings.activeBuildTarget;
            public string GPU = SystemInfo.graphicsDeviceName;
            public string GPUFeatureLevel = SystemInfo.graphicsDeviceVersion;
            public EffectLicenseStatus[] LicenseStatuses = GetLicenseStatuses();
            public string KeyCount = ServerAuthManager.GetInstance().PluginLicenseKeys.Length.ToString();
            public string[] Warnings = GetWarnings();
        };

        [MenuItem(Effects.BaseMenuBarPath + "Copy diagnostic information to clipboard", false, 10000)]
        public static void Copy()
        {
            DiagInfo info = new DiagInfo();
            string diagInfo = "```\nZibra Effects Diagnostic Information Begin\n";
            diagInfo += JsonUtility.ToJson(info, true) + "\n";
            diagInfo += "Zibra Effects Diagnostic Information End\n```\n";
            GUIUtility.systemCopyBuffer = diagInfo;
        }
    }
}