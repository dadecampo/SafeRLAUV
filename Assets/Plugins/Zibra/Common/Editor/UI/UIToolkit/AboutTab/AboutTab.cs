using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using com.zibra.liquid.Foundation.UIElements;
using com.zibra.common.Editor.SDFObjects;
using System;

#if UNITY_2019_4_OR_NEWER
namespace com.zibra.liquid.Plugins.Editor
{
    internal class AboutTab : BaseTab
    {
        private TextField m_AuthKeyInputField;
        private Label m_ValidationProgressLabel;
        private Label m_RegisteredKeyLabel;

        public AboutTab() : base($"{ZibraAIPackage.UIToolkitPath}/AboutTab/AboutTab")
        {
            VisualElement registrationBlock = this.Q<SettingsBlock>("registrationBlock");
            Button checkAuthKeyBtn = this.Q<Button>("validateAuthKeyBtn");
            Button removeAuthKeyBtn = this.Q<Button>("removeAuthKeyBtn");
            m_AuthKeyInputField = this.Q<TextField>("authKeyInputField");
            m_ValidationProgressLabel = this.Q<Label>("validationProgress");
            m_RegisteredKeyLabel = this.Q<Label>("registeredKeyLabel");

            ServerAuthManager.GetInstance();
            m_AuthKeyInputField.value = String.Join(",", ServerAuthManager.GetInstance().PluginLicenseKeys);
            checkAuthKeyBtn.clicked += OnAuthKeyBtnOnClickedHandler;
            removeAuthKeyBtn.clicked += OnRemoveKeyBtnOnClickedHandler;
            // Hide if key is valid.
            if (ServerAuthManager.GetInstance().IsLicenseVerified(ServerAuthManager.Effect.Liquids))
            {
                checkAuthKeyBtn.style.display = DisplayStyle.None;
                m_AuthKeyInputField.style.display = DisplayStyle.None;
                m_RegisteredKeyLabel.style.display = DisplayStyle.Flex;
                removeAuthKeyBtn.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_RegisteredKeyLabel.style.display = DisplayStyle.None;
                removeAuthKeyBtn.style.display = DisplayStyle.None;
            }

            m_ValidationProgressLabel.style.display = DisplayStyle.None;
        }

        private void OnAuthKeyBtnOnClickedHandler()
        {
            string keys = m_AuthKeyInputField.text;

            if (!ServerAuthManager.CheckKeysFormat(keys))
            {
                EditorUtility.DisplayDialog("Zibra Liquid Key Error", "Incorrect key format.", "Ok");
                return;
            }

            ServerAuthManager.GetInstance().RegisterKey(keys);
            m_RegisteredKeyLabel.style.display = DisplayStyle.None;
        }

        private void OnRemoveKeyBtnOnClickedHandler()
        {
            ServerAuthManager.GetInstance().RemoveKey();
        }
    }
}
#endif