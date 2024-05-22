#if UNITY_2019_4_OR_NEWER
using com.zibra.common.Editor;
using com.zibra.common.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.zibra.common
{
    /// <summary>
    /// Class that contains code for useful actions for the plugin
    /// Those actions exposed to user via MenuItem
    /// You can call them from C# via ExecuteMenuItem
    /// </summary>
    internal static class EffectsEditorMenu
    {
        [MenuItem(Effects.BaseMenuBarPath + "View License Details", false, 2)]
        static void OpenSettings()
        {
            var windowTitle = EffectsSettingsWindow.WindowTitle;
            EffectsSettingsWindow.ShowTowardsInspector(windowTitle.text, windowTitle.image);
        }

        static void OpenFile(string GUID)
        {
            string dataPath = Application.dataPath;
            string projectPath = dataPath.Replace("/Assets", "");
            string filePath = AssetDatabase.GUIDToAssetPath(GUID);
            Application.OpenURL("file://" + projectPath + "/" + filePath);
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open Documentation", false, 30)]
        static void OpenUserGuide()
        {
#if ZIBRA_EFFECTS_OTP_VERSION
            if (PluginManager.AvailableCount() >= 2)
            {
                Application.OpenURL("https://zibra.notion.site/Zibra-AI-30f5387d28054131a123d124dbf3941f?pvs=4");
            }
            else if (PluginManager.IsAvailable(PluginManager.Effect.Liquid))
            {
                Application.OpenURL("https://zibra.notion.site/Zibra-Liquid-c0cb383f753a48db8c73b598d71ed68e");
            }
            else
            {
                Application.OpenURL("https://zibra.notion.site/Zibra-Smoke-Fire-ff523dd66e104179b79ca066586fab18");
            }
#else
            Application.OpenURL("https://zibra.notion.site/Zibra-Effects-0dad2d38da054cbeb6a699a3b2cfb2b1");
#endif
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open API Reference", false, 31)]
        static void OpenAPIDocumentation()
        {
            string dataPath = Application.dataPath;
            string projectPath = dataPath.Replace("/Assets", "");
            string documentationPath = AssetDatabase.GUIDToAssetPath("d9e57e1e9783349ffa44b5f943410fab");
            Application.OpenURL("file://" + projectPath + "/" + documentationPath + "/index.html");
        }

        [MenuItem(Effects.BaseMenuBarPath + "Browse Assets on Unity Asset Store", false, 34)]
        static void OpenAssetStore()
        {
            Application.OpenURL("https://effects.zibra.ai/open");
        }        

        [MenuItem(Effects.BaseMenuBarPath + "Contact us/Discord", false, 1000)]
        static void OpenDiscord()
        {
            Application.OpenURL("https://discord.gg/QzypP8n7uB");
        }

        [MenuItem(Effects.BaseMenuBarPath + "Contact us/Support E-Mail", false, 1010)]
        static void OpenSupportEmail()
        {
            Application.OpenURL("mailto:support@zibra.ai");
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open Sample Scene/Combined Liquid and Smoke", false, 29)]
        static void OpenCombinedEffectsSampleScene()
        {
            string GUID = "";
            switch (RenderPipelineDetector.GetRenderPipelineType())
            {
                case RenderPipelineDetector.RenderPipeline.BuiltInRP:
                    GUID = "2eaf9a27e7194a6797736a1b063e581f";
                    break;
                case RenderPipelineDetector.RenderPipeline.URP:
                    GUID = "d9a09277d7d7dc4449a5299b4f122bbb";
                    break;
                case RenderPipelineDetector.RenderPipeline.HDRP:
                    GUID = "f8335ff236204cc468c5c836e4bc180d";
                    break;
            }
            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(GUID));
        }
    }
}
#endif
