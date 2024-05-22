#if UNITY_2019_4_OR_NEWER
using com.zibra.common;
using com.zibra.common.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.zibra.liquid
{
    /// <summary>
    /// Class that contains code for useful actions for the liquid plugin
    /// Those actions exposed to user via MenuItem
    /// You can call them from C# via ExecuteMenuItem
    /// </summary>
    internal static class LiquidEditorMenu
    {
        [MenuItem(Effects.BaseMenuBarPath + "Open Sample Scene/Liquid", false, 30)]
        static void OpenLiquidSampleScene()
        {
            string GUID = "";
            switch (RenderPipelineDetector.GetRenderPipelineType())
            {
                case RenderPipelineDetector.RenderPipeline.BuiltInRP:
                    GUID = "c38cb0fee7a2b8b45868fad0b2b129c5";
                    break;
                case RenderPipelineDetector.RenderPipeline.URP:
                    GUID = "adb3d7b46c46d174891d3c7b5f823f2c";
                    break;
                case RenderPipelineDetector.RenderPipeline.HDRP:
                    GUID = "c493230331ba7c24b837cec206a7cac6";
                    break;
            }
            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(GUID));
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open Sample Scene/Liquid (Mobile)", false, 31)]
        static void OpenLiquidMobileSampleScene()
        {
            string GUID = "";
            switch (RenderPipelineDetector.GetRenderPipelineType())
            {
                case RenderPipelineDetector.RenderPipeline.BuiltInRP:
                    GUID = "e6ff23ceb2cb16a44b4e6a6e101774ba";
                    break;
                case RenderPipelineDetector.RenderPipeline.URP:
                    GUID = "ad2ebae2246d0694b823868547dda06b";
                    break;
                case RenderPipelineDetector.RenderPipeline.HDRP:
                    GUID = "e6ff23ceb2cb16a44b4e6a6e101774ba";
                    string errorMessage = "Mobile platforms don't support HDRP. Opening BiRP sample scene instead.";
                    EditorUtility.DisplayDialog("Zibra Effects Error.", errorMessage, "OK");
                    Debug.LogError(errorMessage);
                    break;
            }
            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(GUID));
        }
    }
}
#endif
