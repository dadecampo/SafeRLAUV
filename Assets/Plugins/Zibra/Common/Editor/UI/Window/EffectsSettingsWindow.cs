#if UNITY_2019_4_OR_NEWER
using com.zibra.liquid.Plugins.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using com.zibra.common.Editor.SDFObjects;

namespace com.zibra.liquid
{
    internal class EffectsSettingsWindow : PackageSettingsWindow<EffectsSettingsWindow>
    {
        internal override IPackageInfo GetPackageInfo() => new ZibraAiPackageInfo();

        private ServerAuthManager.Status LatestStatus =
            ServerAuthManager.Status.NotInitialized;

        protected override void OnWindowEnable(VisualElement root)
        {
            AddTab("Info", new AboutTab());
        }

        internal void Update()
        {
            if (ServerAuthManager.GetInstance().GetStatus(ServerAuthManager.Effect.Liquids) == LatestStatus)
                return;

            LatestStatus = ServerAuthManager.GetInstance().GetStatus(ServerAuthManager.Effect.Liquids);

            if (ServerAuthManager.GetInstance().IsLicenseVerified(ServerAuthManager.Effect.Liquids))
            {
                m_Tabs["Info"].Q<Button>("validateAuthKeyBtn").style.display = DisplayStyle.None;
                m_Tabs["Info"].Q<TextField>("authKeyInputField").style.display = DisplayStyle.None;
                m_Tabs["Info"].Q<Label>("validationProgress").style.display = DisplayStyle.None;
                m_Tabs["Info"].Q<Label>("registeredKeyLabel").style.display = DisplayStyle.Flex;
                m_Tabs["Info"].Q<Button>("removeAuthKeyBtn").style.display = DisplayStyle.Flex;
            }
            else
            {
                m_Tabs["Info"].Q<Label>("validationProgress").text =
                    ServerAuthManager.GetInstance().GetErrorMessage(ServerAuthManager.Effect.Liquids);
                m_Tabs["Info"].Q<Button>("validateAuthKeyBtn").style.display = DisplayStyle.Flex;
                m_Tabs["Info"].Q<TextField>("authKeyInputField").style.display = DisplayStyle.Flex;
                m_Tabs["Info"].Q<Label>("validationProgress").style.display = DisplayStyle.Flex;
                m_Tabs["Info"].Q<Label>("registeredKeyLabel").style.display = DisplayStyle.None;
                m_Tabs["Info"].Q<Button>("removeAuthKeyBtn").style.display = DisplayStyle.None;
            }
        }

        internal static GUIContent WindowTitle => new GUIContent(ZibraAIPackage.DisplayName);
    }
}
#endif