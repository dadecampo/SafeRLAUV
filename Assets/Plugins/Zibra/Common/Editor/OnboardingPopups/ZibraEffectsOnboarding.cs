using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using com.zibra.common.Editor.SDFObjects;

namespace com.zibra.common.Editor
{
    internal class ZibraEffectsOnboarding : EditorWindow
    {
        private const string REGISTRATION_GUID = "39f6c1ca9194bcb4d896f42e94b38b88";
        private const string AUTHORIZATION_GUID = "c70f412c74090c8488e3e2d7ec4a6101";

        private const ServerAuthManager.Effect LIQUIDS = ServerAuthManager.Effect.Liquids;
        private const ServerAuthManager.Effect SMOKE = ServerAuthManager.Effect.Smoke;

        private string CurrentUXMLGUID = REGISTRATION_GUID;
        private bool TriedToVerify = false;

        private TextField AuthKeyInputField;
        private Label AuthMessage;
        private Label AuthMessageLiquids;
        private Label AuthMessageSmoke;
        private Button ActivateButton;
        private Button GetStartedButton;
        private VisualElement ValidationMessages;

        private ServerAuthManager.Status LatestStatusLiquids = ServerAuthManager.Status.NotInitialized;
        private ServerAuthManager.Status LatestStatusSmoke = ServerAuthManager.Status.NotInitialized;

        private ServerAuthManager ServerAuthInstance;

        public static GUIContent WindowTitle => new GUIContent("Zibra Effects Onboarding Screen");

        internal static void ShowWindowDelayed()
        {
            ShowWindow();
            EditorApplication.update -= ShowWindowDelayed;
        }

        [InitializeOnLoadMethod]
        internal static void InitializeOnLoad()
        {
            // Don't automatically open any windows in batch mode
            if (Application.isBatchMode)
            {
                return;
            }

            // If user already has saved license key, don't show him this popup
            if (ServerAuthManager.GetInstance().PluginLicenseKeys.Length > 0)
            {
                // If user removes key during editor session, don't show him popup
                SessionState.SetBool("ZibraEffectsProOnboardingShown", false);
                return;
            }

            if (SessionState.GetBool("ZibraEffectsProOnboardingShown", true))
            {
                SessionState.SetBool("ZibraEffectsProOnboardingShown", false);
                EditorApplication.update += ShowWindowDelayed;
            }
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open Onboarding", false, 1)]
        private static void ShowWindow()
        {
            ZibraEffectsOnboarding window = (ZibraEffectsOnboarding)GetWindow(typeof(ZibraEffectsOnboarding));
            window.titleContent = WindowTitle;
            window.Show();
        }

        private static void CloseWindow()
        {
            ZibraEffectsOnboarding window = (ZibraEffectsOnboarding)GetWindow(typeof(ZibraEffectsOnboarding));
            window.Close();
        }

        private void OnEnable()
        {
            var root = rootVisualElement;
            root.Clear();

            int width = 456;
            int height = 442;

            minSize = maxSize = new Vector2(width, height);

            var uxmlAssetPath = AssetDatabase.GUIDToAssetPath(CurrentUXMLGUID);
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
            visualTree.CloneTree(root);

            if (CurrentUXMLGUID == REGISTRATION_GUID)
            {
                root.Q<Button>("GetKey").clicked += GetKeyClick;
                root.Q<Button>("HaveKey").clicked += HaveKeyClick;
                root.Q<Button>("PrivacyPolicy").clicked += PrivacyPolicyClick;
                root.Q<Button>("TermsAndConditions").clicked += TermsAndConditionsClick;
            }
            if (CurrentUXMLGUID == AUTHORIZATION_GUID)
            {
                ServerAuthInstance = ServerAuthManager.GetInstance();
                root.Q<Button>("BackToRegistration").clicked += BackToRegistrationClick;

                AuthMessage = root.Q<Label>("AuthorizationMessage");
                AuthMessageLiquids = root.Q<Label>("AuthMessageLiquids");
                AuthMessageSmoke = root.Q<Label>("AuthMessageSmoke");
                ActivateButton = root.Q<Button>("Activate");
                GetStartedButton = root.Q<Button>("GetStart");
                AuthKeyInputField = root.Q<TextField>("ActivationField");
                ValidationMessages = root.Q<VisualElement>("ValidationMessages");

                AuthKeyInputField.RegisterCallback<KeyDownEvent>(evt =>
                                                                 {
                                                                     if (evt.keyCode == KeyCode.Return)
                                                                     {
                                                                         ActivateClick();
                                                                         evt.StopPropagation();
                                                                     }
                                                                 });
                ActivateButton.clicked += ActivateClick;
                GetStartedButton.clicked += GetStartedClick;

                IsKeyValidated();
            }
        }

        private void GetKeyClick()
        {
            Application.OpenURL("https://license.zibra.ai/api/stripeTrial?source=plugin");
            CurrentUXMLGUID = AUTHORIZATION_GUID;
            OnEnable();
        }

        private void HaveKeyClick()
        {
            CurrentUXMLGUID = AUTHORIZATION_GUID;
            OnEnable();
        }

        private void PrivacyPolicyClick()
        {
            Application.OpenURL("https://zibra.ai/privacy-policy/");
        }

        private void TermsAndConditionsClick()
        {
            Application.OpenURL("https://zibra.ai/terms-of-service/");
        }

        private void ActivateClick()
        {
            string keys = AuthKeyInputField.text;

            if (!ServerAuthManager.CheckKeysFormat(keys))
            {
                AuthMessageStyle(AuthMessage, Color.red);
                AuthMessage.text = ("Incorrect key format.");
                return;
            }

            TriedToVerify = true;
            ServerAuthInstance.RegisterKey(keys);
            AuthKeyInputField.style.display = DisplayStyle.None;
            ActivateButton.style.display = DisplayStyle.None;
            AuthMessage.text = ServerAuthInstance.GetErrorMessage(LIQUIDS);
        }

        private void GetStartedClick()
        {
            EditorApplication.ExecuteMenuItem(Effects.BaseMenuBarPath + "Open User Guide");
            CloseWindow();
        }

        private void BackToRegistrationClick()
        {
            CurrentUXMLGUID = REGISTRATION_GUID;
            OnEnable();
        }

        private void AuthMessageStyle(Label authMessage, Color color, bool useMargin = true)
        {
            if (useMargin)
                authMessage.style.marginTop = 58;

            authMessage.style.color = new StyleColor(color);
            authMessage.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        private void Update()
        {
            if (TriedToVerify)
            {

                if (ServerAuthInstance.GetStatus(LIQUIDS) == LatestStatusLiquids &&
                    ServerAuthInstance.GetStatus(SMOKE) == LatestStatusSmoke)
                    return;

                LatestStatusLiquids = ServerAuthManager.GetInstance().GetStatus(LIQUIDS);
                LatestStatusSmoke = ServerAuthManager.GetInstance().GetStatus(SMOKE);

                if (!IsKeyValidated())
                {
                    if (ServerAuthInstance.GetStatus(LIQUIDS) != ServerAuthManager.Status.NotRegistered &&
                        ServerAuthInstance.GetStatus(LIQUIDS) != ServerAuthManager.Status.KeyValidationInProgress &&
                        ServerAuthInstance.GetStatus(SMOKE) != ServerAuthManager.Status.NotRegistered &&
                        ServerAuthInstance.GetStatus(SMOKE) != ServerAuthManager.Status.KeyValidationInProgress)
                    {
                        AuthMessage.style.display = DisplayStyle.None;
                        AuthMessageStyle(AuthMessageLiquids, Color.red, false);
                        AuthMessageLiquids.text = ServerAuthInstance.GetErrorMessage(LIQUIDS);
                        AuthMessageStyle(AuthMessageSmoke, Color.red, false);
                        AuthMessageSmoke.text = ServerAuthInstance.GetErrorMessage(SMOKE);
                        ValidationMessages.style.display = DisplayStyle.Flex;
                        AuthKeyInputField.style.display = DisplayStyle.Flex;
                        ActivateButton.style.display = DisplayStyle.Flex;
                    }
                    else if (ServerAuthInstance.GetStatus(LIQUIDS) != ServerAuthManager.Status.NotRegistered &&
                             ServerAuthInstance.GetStatus(LIQUIDS) != ServerAuthManager.Status.KeyValidationInProgress)
                    {
                        AuthMessageStyle(AuthMessage, Color.red);
                        AuthMessage.text = ServerAuthInstance.GetErrorMessage(LIQUIDS);
                        AuthKeyInputField.style.display = DisplayStyle.Flex;
                        ActivateButton.style.display = DisplayStyle.Flex;
                    }
                    else if (ServerAuthInstance.GetStatus(SMOKE) != ServerAuthManager.Status.NotRegistered &&
                             ServerAuthInstance.GetStatus(SMOKE) != ServerAuthManager.Status.KeyValidationInProgress)
                    {
                        AuthMessageStyle(AuthMessage, Color.red);
                        AuthMessage.text = ServerAuthInstance.GetErrorMessage(SMOKE);
                        AuthKeyInputField.style.display = DisplayStyle.Flex;
                        ActivateButton.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        if (ServerAuthInstance.GetErrorMessage(LIQUIDS) != "" &&
                            ServerAuthInstance.GetErrorMessage(SMOKE) != "")
                        {
                            if (ServerAuthInstance.GetErrorMessage(LIQUIDS) ==
                                ServerAuthInstance.GetErrorMessage(SMOKE))
                            {
                                ValidationMessages.style.display = DisplayStyle.None;
                                AuthMessageStyle(AuthMessage, Color.white);
                                AuthMessage.text = ServerAuthInstance.GetErrorMessage(LIQUIDS);
                                AuthMessage.style.display = DisplayStyle.Flex;
                            }
                            else
                            {
                                AuthMessage.style.display = DisplayStyle.None;
                                AuthMessageStyle(AuthMessageLiquids, Color.white, false);
                                AuthMessageLiquids.text = ServerAuthInstance.GetErrorMessage(LIQUIDS);
                                AuthMessageStyle(AuthMessageSmoke, Color.white, false);
                                AuthMessageSmoke.text = ServerAuthInstance.GetErrorMessage(SMOKE);
                                ValidationMessages.style.display = DisplayStyle.Flex;
                            }
                        }
                        else if (ServerAuthInstance.GetErrorMessage(LIQUIDS) != "")
                        {
                            AuthMessageStyle(AuthMessage, Color.white);
                            AuthMessage.text = ServerAuthInstance.GetErrorMessage(LIQUIDS);
                        }
                        else if (ServerAuthInstance.GetErrorMessage(SMOKE) != "")
                        {
                            AuthMessageStyle(AuthMessage, Color.white);
                            AuthMessage.text = ServerAuthInstance.GetErrorMessage(SMOKE);
                        }
                    }
                }
            }
        }
        private bool IsKeyValidated()
        {
            if (ServerAuthInstance.IsLicenseVerified(LIQUIDS) && ServerAuthInstance.IsLicenseVerified(SMOKE))
            {
                ValidationMessages.style.display = DisplayStyle.None;
                AuthMessageStyle(AuthMessage, Color.green);
                AuthMessage.text = "License validated successfully";
                AuthKeyInputField.style.display = DisplayStyle.None;
                ActivateButton.style.display = DisplayStyle.None;
                AuthMessage.style.display = DisplayStyle.Flex;
                GetStartedButton.style.display = DisplayStyle.Flex;
                return true;
            }
            else if (ServerAuthInstance.IsLicenseVerified(LIQUIDS))
            {
                AuthMessage.style.display = DisplayStyle.None;
                AuthKeyInputField.style.display = DisplayStyle.None;
                ActivateButton.style.display = DisplayStyle.None;
                AuthMessageStyle(AuthMessageLiquids, Color.green, false);
                AuthMessageLiquids.text = "Liquids license validated successfully";
                AuthMessageStyle(AuthMessageSmoke, Color.white, false);
                AuthMessageSmoke.text = "Your license doesn't include Smoke & Fire";
                ValidationMessages.style.display = DisplayStyle.Flex;
                GetStartedButton.style.display = DisplayStyle.Flex;
                return true;
            }
            else if (ServerAuthInstance.IsLicenseVerified(SMOKE))
            {
                AuthMessage.style.display = DisplayStyle.None;
                AuthKeyInputField.style.display = DisplayStyle.None;
                ActivateButton.style.display = DisplayStyle.None;
                AuthMessageStyle(AuthMessageLiquids, Color.white, false);
                AuthMessageLiquids.text = "Your license doesn't include Liquids";
                AuthMessageStyle(AuthMessageSmoke, Color.green, false);
                AuthMessageSmoke.text = "Smoke & Fire license validated successfully";
                ValidationMessages.style.display = DisplayStyle.Flex;
                GetStartedButton.style.display = DisplayStyle.Flex;
                return true;
            }
            return false;
        }
    }
}
