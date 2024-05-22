using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using com.zibra.common.Editor.SDFObjects;
using System;
using System.Text.RegularExpressions;
using com.zibra.common.Editor.Licensing;
using static com.zibra.common.Editor.PluginManager;
using com.zibra.common.Analytics;

namespace com.zibra.common.Editor
{
    internal class ZibraEffectsOnboarding : EditorWindow
    {
#if !ZIBRA_EFFECTS_OTP_VERSION
        private const string ONBOARDING_GUID = "39f6c1ca9194bcb4d896f42e94b38b88";
        private string CurrentUXMLGUID = ONBOARDING_GUID;
#else
        private const string REGISTRATION_GUID = "a4fb4aa3ec918b0438c0370d64ed10a9";
        private string CurrentUXMLGUID = REGISTRATION_GUID;
#endif
        private const string AUTHORIZATION_GUID = "c70f412c74090c8488e3e2d7ec4a6101";
        private const string SUCCESS_GUID = "ed10b24decb1f804c9d36fc8e2b590db";

        private const string IS_ONBOARDING_SHOWN_SESSION_KEY = "ZibraEffectsProOnboardingShown";
        private Color LIGHT_RED = new Color(1f, 0.3f, 0.3f, 1f);

        private bool TriedToVerify = false;

        private TextField AuthKeyInputField;
        private VisualElement BackElement;
        private Label HeaderMessage;
        private Label BodyMessage;
        private Label StatusMessage;
#if !ZIBRA_EFFECTS_OTP_VERSION
        private Label AuthMessageLiquid;
        private Label AuthMessageSmoke;
        private VisualElement ValidationMessages;
#else
        private TextField OrderNumber;
        private TextField Email;
        private TextField Name;
        private Button Register;
        private VisualElement RegistrationFields;
        private Button HaveKey;
        private Button OrderHistory;
        private RegistrationManager.Status LatestRegistrationStatus;
        private bool TriedToRegister = false;
#endif
        private Button ActivateButton;
        private Button AssetStoreButton;
        private Button UserGuideButton;

#if ZIBRA_EFFECTS_OTP_VERSION
        private Effect EffectToActivate = Effect.Count;
#else
        private Effect EffectToActivate = Effect.Liquid;
#endif
        private float PositionOffset = 0;

        private string ProductName;

        public static GUIContent WindowTitle => new GUIContent("Zibra Effects Onboarding Screen");

        internal static void ShowWindowOnStartup()
        {
            ShowWindow("editor_start");
            EditorApplication.update -= ShowWindowOnStartup;
        }

        [MenuItem(Effects.BaseMenuBarPath + "Activate License", false, 1)]
        public static void ShowWindowFromMenu()
        {
            ShowWindow("onboarding_button");
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
            if (LicensingManager.Instance.GetSavedLicenseKeys().Length > 0)
            {
                // If user removes key during editor session, don't show him popup
                SessionState.SetBool(IS_ONBOARDING_SHOWN_SESSION_KEY, true);
                return;
            }

            if (SessionState.GetBool(IS_ONBOARDING_SHOWN_SESSION_KEY, false))
            {
                SessionState.SetBool(IS_ONBOARDING_SHOWN_SESSION_KEY, true);
                EditorApplication.update += ShowWindowOnStartup;
            }
        }
        public static void ShowWindow(string activationTrigger)
        {
            bool needActivation = false;
#if !ZIBRA_EFFECTS_OTP_VERSION
            if (!GenerationManager.Instance.IsGenerationAvailable())
            {
                ActivationTracking.GetInstance("effects").StoreActivationTrigger(activationTrigger);
                ZibraEffectsOnboarding window = CreateWindow<ZibraEffectsOnboarding>($"Activate Zibra Effects");
                window.Show();
                needActivation = true;
            }
#else
            float offset = 0;
            const float OFFSET_STEP = 50;
            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                if (PluginManager.IsAvailable(effect) && !LicensingManager.Instance.IsLicenseVerified(effect))
                {
                    ActivationTracking.GetInstance(effect).StoreActivationTrigger(activationTrigger);
                    ZibraEffectsOnboarding window = CreateWindow<ZibraEffectsOnboarding>($"Activate Zibra {effect}");
                    window.EffectToActivate = effect;
                    window.PositionOffset = offset;
                    offset += OFFSET_STEP;
                    window.OnEnable();
                    window.Show();
                    needActivation = true;
                }
            }
#endif
            if (!needActivation)
            {
                string errorMessage = "Plugin is already activated.";
                EditorUtility.DisplayDialog("Activation.", errorMessage, "OK");
                Debug.Log(errorMessage);
            }
        }

        private static void CloseWindow()
        {
            ZibraEffectsOnboarding window = (ZibraEffectsOnboarding)GetWindow(typeof(ZibraEffectsOnboarding));
            window.Close();
        }

        private void OnEnable()
        {
#if ZIBRA_EFFECTS_OTP_VERSION
            if (EffectToActivate == Effect.Count)
            {
                return;
            }
#endif

            LicensingManager.Instance.OnLicenseStatusUpdate += UpdateLicensingUI;

#if !ZIBRA_EFFECTS_OTP_VERSION
            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                if (PluginManager.IsAvailable(effect))
                {
                    EffectToActivate = effect;
                    break;
                }
            }

            ProductName = "Zibra Effects";
#else
            switch (EffectToActivate)
            {
                case Effect.Liquid:
                    ProductName = "Zibra Liquid";
                    break;
                case Effect.Smoke:
                    ProductName = "Zibra Smoke & Fire";
                    break;
                default:
                    throw new ArgumentException($"Invalid Effect Type {EffectToActivate}");
            }
#endif

            var root = rootVisualElement;
            root.Clear();

            int width = 456;
            int height = 442;

            minSize = new Vector2(width, height);
            maxSize = minSize;

            Rect pos = position;
            pos.x += PositionOffset;
            pos.y += PositionOffset;
            PositionOffset = 0;
            pos.width = width;
            pos.height = height;
            position = pos;

            var uxmlAssetPath = AssetDatabase.GUIDToAssetPath(CurrentUXMLGUID);
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
            visualTree.CloneTree(root);

            root.Query<TextElement>().ToList().ForEach(e => { e.text = e.text.Replace("!ProductName", ProductName); });

#if !ZIBRA_EFFECTS_OTP_VERSION
            if (CurrentUXMLGUID == ONBOARDING_GUID)
            {
                root.Q<Button>("GetKey").clicked += GetKeyClick;
                root.Q<Button>("HaveKey").clicked += HaveKeyClick;
                root.Q<Button>("PrivacyPolicy").clicked += PrivacyPolicyClick;
                root.Q<Button>("TermsAndConditions").clicked += TermsAndConditionsClick;
            }
#endif
#if ZIBRA_EFFECTS_OTP_VERSION
            if (CurrentUXMLGUID == REGISTRATION_GUID)
            {
                if (LicensingManager.Instance.IsLicenseVerified(EffectToActivate))
                {
                    CurrentUXMLGUID = AUTHORIZATION_GUID;
                    OnEnable();
                    return;
                }

                StatusMessage = root.Q<Label>("StatusMessage");
                OrderNumber = root.Q<TextField>("OrderNumber");
                Email = root.Q<TextField>("Email");
                Name = root.Q<TextField>("Name");
                Register = root.Q<Button>("Register");
                Register.clicked += RegisterClick;
                RegistrationFields = root.Q<VisualElement>("RegistrationFields");
                HaveKey = root.Q<Button>("HaveKey");
                HaveKey.clicked += HaveKeyClick;
                OrderHistory = root.Q<Button>("OrderHistory");
                OrderHistory.clicked += OrderHistoryClick;

                EventCallback<KeyDownEvent> RegisterLambda = (evt =>
                {
                    if (evt.keyCode == KeyCode.Return)
                    {
                        RegisterClick();
                        evt.StopPropagation();
                    }
                });
                OrderNumber.RegisterCallback(RegisterLambda);
                Email.RegisterCallback(RegisterLambda);
                Name.RegisterCallback(RegisterLambda);
            }
#endif
            if (CurrentUXMLGUID == AUTHORIZATION_GUID)
            {
                root.Q<Button>("BackButton").clicked += BackClick;

                BackElement = root.Q<VisualElement>("BackElement");
                HeaderMessage = root.Q<Label>("HeaderMessage");
                BodyMessage = root.Q<Label>("BodyMessage");
                StatusMessage = root.Q<Label>("StatusMessage");
#if !ZIBRA_EFFECTS_OTP_VERSION
                AuthMessageLiquid = root.Q<Label>("AuthMessageLiquid");
                AuthMessageSmoke = root.Q<Label>("AuthMessageSmoke");
                ValidationMessages = root.Q<VisualElement>("ValidationMessages");
#endif
                ActivateButton = root.Q<Button>("Activate");
                AuthKeyInputField = root.Q<TextField>("ActivationField");

                AuthKeyInputField.RegisterCallback<KeyDownEvent>(evt =>
                                                                 {
                                                                     if (evt.keyCode == KeyCode.Return)
                                                                     {
                                                                         ActivateClick();
                                                                         evt.StopPropagation();
                                                                     }
                                                                 });
                ActivateButton.clicked += ActivateClick;

                UpdateActivationUI();
            }

            if (CurrentUXMLGUID == SUCCESS_GUID)
            {
                AssetStoreButton = root.Q<Button>("OpenAssets");
                UserGuideButton = root.Q<Button>("OpenUserGuide");
                AssetStoreButton.clicked += AssetStoreClick;
                UserGuideButton.clicked += UserGuideClick;
            }
        }
        
        private void OnDisable()
        {
            LicensingManager.Instance.OnLicenseStatusUpdate -= UpdateLicensingUI;
        }

        private void GetKeyClick()
        {
#if !ZIBRA_EFFECTS_OTP_VERSION
            Application.OpenURL("https://license.zibra.ai/api/stripeTrial?source=plugin");
            CurrentUXMLGUID = AUTHORIZATION_GUID;
#else
            CurrentUXMLGUID = REGISTRATION_GUID;
#endif
            OnEnable();
        }

        private void HaveKeyClick()
        {
            CurrentUXMLGUID = AUTHORIZATION_GUID;
            OnEnable();
        }

        private void PrivacyPolicyClick()
        {
            Application.OpenURL("https://effects.zibra.ai/privacy-policy");
        }

        private void TermsAndConditionsClick()
        {
            Application.OpenURL("https://effects.zibra.ai/terms-of-servises");
        }

#if ZIBRA_EFFECTS_OTP_VERSION
        private void OrderHistoryClick()
        {
            Application.OpenURL("https://assetstore.unity.com/orders");
        }
        private void RegisterClick()
        {
            if (!ValidateRegistrationInput())
            {
                return;
            }

            TriedToRegister = true;
            RegistrationFields.style.display = DisplayStyle.None;
            HaveKey.style.display = DisplayStyle.None;
            OrderHistory.style.display = DisplayStyle.None;
            LabelStyle(StatusMessage, Color.white);
            StatusMessage.text = "Registering in progress, please wait.";
            RegistrationManager.Instance.Register(EffectToActivate, OrderNumber.text, Email.text, Name.text);
            UpdateRegistrationUI();
        }

        private void ReportError(string error)
        {
            LabelStyle(StatusMessage, LIGHT_RED);
            StatusMessage.text = error;
        }

        private bool ValidateRegistrationInput()
        {
            const string ORDER_NUMBER_REGEX = "^(\\d{13,14})|(IN\\d{12})$";
            const string EMAIL_REGEX = "^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]+$";
            const string NAME_REGEX = "^[a-zA-Z\\- ]+$";

            OrderNumber.value = OrderNumber.text.Trim();
            Email.value = Email.text.Trim();
            Name.value = Name.text.Trim();

            if (!Regex.IsMatch(OrderNumber.text, ORDER_NUMBER_REGEX))
            {
                ReportError(OrderNumber.text == "" ? "Please enter Order number or Invoice number." : "Invalid Order number or Invoice number.");
                return false;
            }
            if (!Regex.IsMatch(Email.text, EMAIL_REGEX))
            {
                ReportError(Email.text == "" ? "Please enter your Email." : "Invalid Email. Please ensure you enter a valid email address.");
                return false;
            }
            if (!Regex.IsMatch(Name.text, NAME_REGEX) || Name.text == "Name")
            {
                ReportError(Name.text == "" || Name.text == "Name" ? "Please enter your Name." : "Please ensure you have entered your name correctly.");
                return false;
            }

            return true;
        }

        private void UpdateRegistrationUI()
        {
            if (LicensingManager.Instance.IsLicenseVerified(EffectToActivate))
            {
                TriedToVerify = true;
                CurrentUXMLGUID = AUTHORIZATION_GUID;
                OnEnable();
                ReportActivationStart();
                return;
            }
            else if (TriedToRegister && RegistrationManager.Instance.CurrentStatus == RegistrationManager.Status.OK)
            {
                TriedToRegister = false;
                TriedToVerify = true;
                CurrentUXMLGUID = AUTHORIZATION_GUID;
                OnEnable();
                ReportActivationStart();
            }
            else if (!TriedToRegister || RegistrationManager.Instance.CurrentStatus == RegistrationManager.Status.InProgress)
            {
                // NOOP
            }
            else
            {
                ReportError(RegistrationManager.Instance.ErrorMessage);
                RegistrationFields.style.display = DisplayStyle.Flex;
                HaveKey.style.display = DisplayStyle.Flex;
                OrderHistory.style.display = DisplayStyle.Flex;
                TriedToRegister = false;
            }
        }
#endif

        private void ActivateClick()
        {
            string key = AuthKeyInputField.text;

            if (key == "")
            {
                LabelStyle(StatusMessage, LIGHT_RED);
                StatusMessage.text = ("Please enter your license key.");
                return;
            }

            if (!LicensingManager.ValidateKeyFormat(key))
            {
                LabelStyle(StatusMessage, LIGHT_RED);
                StatusMessage.text = ("Invalid license key.");
                return;
            }

            TriedToVerify = true;

#if !ZIBRA_EFFECTS_OTP_VERSION
            LicensingManager.Instance.ValidateLicense(key, new Effect[] { Effect.Liquid, Effect.Smoke, Effect.ZibraVDB });
#else
            LicensingManager.Instance.ValidateLicense(key, EffectToActivate);
#endif
            ReportActivationStart();
        }

        private void ReportActivationStart()
        {
            StatusMessage.style.display = DisplayStyle.Flex;
            BackElement.style.display = DisplayStyle.None;
            BodyMessage.style.display = DisplayStyle.None;
            AuthKeyInputField.style.display = DisplayStyle.None;
            ActivateButton.style.display = DisplayStyle.None;
            LabelStyle(StatusMessage, Color.white);
            StatusMessage.text = LicensingManager.Instance.GetErrorMessage(EffectToActivate);
        }
        private void AssetStoreClick()
        {
            EditorApplication.ExecuteMenuItem(Effects.BaseMenuBarPath + "Browse Assets on Unity Asset Store");
        }

        private void UserGuideClick()
        {
            EditorApplication.ExecuteMenuItem(Effects.BaseMenuBarPath + "Open Documentation");
        }

        private void BackClick()
        {
#if !ZIBRA_EFFECTS_OTP_VERSION
            CurrentUXMLGUID = ONBOARDING_GUID;
#else
            CurrentUXMLGUID = REGISTRATION_GUID;
#endif
            OnEnable();
        }

        private void LabelStyle(Label authMessage, Color color)
        {
            authMessage.style.color = new StyleColor(color);
        }

        private void UpdateLicensingUI()
        {
            if (TriedToVerify && CurrentUXMLGUID == AUTHORIZATION_GUID)
            {
                UpdateActivationUI();
            }

#if ZIBRA_EFFECTS_OTP_VERSION
            if (CurrentUXMLGUID == REGISTRATION_GUID)
            {
                UpdateRegistrationUI();
            }
#endif
        }
        private void UpdateActivationUI()
        {
            LicensingManager.Status status = LicensingManager.Instance.GetStatus(EffectToActivate);
            if (LicensingManager.Instance.IsLicenseVerified(EffectToActivate))
            {
                CurrentUXMLGUID = SUCCESS_GUID;
                OnEnable();
                return;
            }
            else if (!TriedToVerify || status == LicensingManager.Status.ValidationInProgress)
            {
                // NOOP
            }
            else
            {
                LabelStyle(StatusMessage, LIGHT_RED);
                StatusMessage.text = LicensingManager.Instance.GetErrorMessage(EffectToActivate);
                BackElement.style.display = DisplayStyle.Flex;
                BodyMessage.style.display = DisplayStyle.Flex;
                StatusMessage.style.display = DisplayStyle.Flex;
                AuthKeyInputField.style.display = DisplayStyle.Flex;
                ActivateButton.style.display = DisplayStyle.Flex;
                TriedToVerify = false;
            }
        }

    }
}
