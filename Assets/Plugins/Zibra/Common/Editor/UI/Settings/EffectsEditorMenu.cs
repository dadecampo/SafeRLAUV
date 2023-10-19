#if UNITY_2019_4_OR_NEWER
using com.zibra.common;
using UnityEditor;
using UnityEngine;

namespace com.zibra.liquid
{
    /// <summary>
    /// Class that contains code for useful actions for the plugin
    /// Those actions exposed to user via MenuItem
    /// You can call them from C# via ExecuteMenuItem
    /// </summary>
    internal static class EffectsEditorMenu
    {
        [MenuItem(Effects.BaseMenuBarPath + "Info", false, 15)]
        internal static void OpenSettings()
        {
            var windowTitle = EffectsSettingsWindow.WindowTitle;
            EffectsSettingsWindow.ShowTowardsInspector(windowTitle.text, windowTitle.image);
        }

        internal static void OpenFile(string GUID)
        {
            string dataPath = Application.dataPath;
            string projectPath = dataPath.Replace("/Assets", "");
            string filePath = AssetDatabase.GUIDToAssetPath(GUID);
            Application.OpenURL("file://" + projectPath + "/" + filePath);
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open User Guide", false, 30)]
        internal static void OpenUserGuide()
        {
            OpenFile("09ace81bf2ac0bd4e8c853cda11f7c84");
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open API Reference", false, 31)]
        internal static void OpenAPIDocumentation()
        {
            string dataPath = Application.dataPath;
            string projectPath = dataPath.Replace("/Assets", "");
            string documentationPath = AssetDatabase.GUIDToAssetPath("d9e57e1e9783349ffa44b5f943410fab");
            Application.OpenURL("file://" + projectPath + "/" + documentationPath + "/index.html");
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open Changelog", false, 33)]
        internal static void OpenChangelog()
        {
            OpenFile("b667af1f31c554a3299ea0e7db5ad45a");
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open Known Issues List", false, 33)]
        internal static void OpenKnownIssues()
        {
            OpenFile("8c104e5e29fc2bc48b6e83e32bb63679");
        }

        [MenuItem(Effects.BaseMenuBarPath + "Contact us/Discord", false, 1000)]
        internal static void OpenDiscord()
        {
            Application.OpenURL("https://discord.gg/QzypP8n7uB");
        }

        [MenuItem(Effects.BaseMenuBarPath + "Contact us/Support E-Mail", false, 1010)]
        internal static void OpenSupportEmail()
        {
            Application.OpenURL("mailto:support@zibra.ai");
        }
    }
}
#endif
