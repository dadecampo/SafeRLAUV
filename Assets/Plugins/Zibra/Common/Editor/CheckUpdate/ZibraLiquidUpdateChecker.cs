using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System;

namespace com.zibra.common.Editor
{
    internal class ZibraEffectsUpdateChecker : EditorWindow
    {
        public static GUIContent WindowTitle => new GUIContent("Zibra Effects Update Checker");

#if !ZIBRA_EFFECTS_OTP_VERSION
        const string URL = "https://generation.zibra.ai/api/pluginVersion?effect=effects&engine=unity&sku=pro";
#else
        const string URL = "https://generation.zibra.ai/api/pluginVersion?effect=effects&engine=unity&sku=otp";
#endif

        private const string UPDATE_CHECK_PREFS_KEY = "ZibraEffectsAutomaticallyCheckForUpdates";
        private const string UPDATE_CHECKED_SESSION_STATE_KEY = "ZibraEffectsUpdateChecked";

        private static ZibraEffectsUpdateChecker InstanceInternal;
        private static ZibraEffectsUpdateChecker Instance
        {
            get {
                if (InstanceInternal == null)
                {
                    InstanceInternal = CreateInstance<ZibraEffectsUpdateChecker>();
                }
                return InstanceInternal;
            }
        }

        private static UnityWebRequestAsyncOperation Request;
        private static Label LabelMessage;
        private static Label LabelPluginSKU;
        private static Button UpdateButton;
        private static Toggle Checkbox;

        private bool IsAutomaticCheck = false;
        private bool IsLatestVersion = false;

        [InitializeOnLoadMethod]
        internal static void InitializeOnLoad()
        {
            // Don't automatically open any windows in batch mode
            if (Application.isBatchMode)
            {
                return;
            }

            // Check whether user disabled auto update checking
            if (!EditorPrefs.GetBool(UPDATE_CHECK_PREFS_KEY, true))
            {
                return;
            }

            // Check whether auto update checking ran this editor session
            if (!SessionState.GetBool(UPDATE_CHECKED_SESSION_STATE_KEY, false))
            {
                SessionState.SetBool(UPDATE_CHECKED_SESSION_STATE_KEY, true);
                EditorApplication.update += ShowWindowDelayed;
            }
        }

        internal static void ShowWindowDelayed()
        {
            ShowWindow(true);
            EditorApplication.update -= ShowWindowDelayed;
        }

        [MenuItem(Effects.BaseMenuBarPath + "Check for Update", false, 9000)]
        public static void ShowWindowMenu()
        {
            ShowWindow(false);
        }

        public static void ShowWindow(bool isAutomaticCheck)
        {
            Instance.IsAutomaticCheck = isAutomaticCheck;

            if (!isAutomaticCheck)
            {
                Instance.Show();
            }
        }

        private void OnEnable()
        {
            titleContent = WindowTitle;

            var root = rootVisualElement;

            int width = 400;
            int height = 240;

            minSize = maxSize = new Vector2(width, height);

            var uxmlAssetPath = AssetDatabase.GUIDToAssetPath("f1c391edc80cd254a81b3eea9e36b979");
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
            visualTree.CloneTree(root);

            var commonUSSAssetPath = AssetDatabase.GUIDToAssetPath("20c4b12a1544dac44b6c04777afe69db");
            var commonStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(commonUSSAssetPath);
            root.styleSheets.Add(commonStyleSheet);

            UpdateButton = root.Q<Button>("UpdateButton");
            UpdateButton.visible = false;
            LabelMessage = root.Q<Label>("Version");
            Checkbox = root.Q<Toggle>("Check");
            Checkbox.value = EditorPrefs.GetBool(UPDATE_CHECK_PREFS_KEY, true);
            Checkbox.RegisterValueChangedCallback(evt => EditorPrefs.SetBool(UPDATE_CHECK_PREFS_KEY, evt.newValue));

            RequestLatestVersion();
        }

        private void UpdatePluginPageClick()
        {
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }

        private void RequestLatestVersion()
        {
            Request = UnityWebRequest.Get(URL).SendWebRequest();
            LabelMessage.text = "Please wait.";
            Request.completed += UpdateCheckVersionRequest;
        }

        private void UpdateCheckVersionRequest(AsyncOperation obj)
        {
            if (Request == null || !Request.isDone)
            {
                return;
            }
            if (Request.webRequest.result != UnityWebRequest.Result.Success)
            {
                if (!IsAutomaticCheck)
                {
                    string errorMessage = $"Update check failed: {Request.webRequest.error}";
                    Debug.LogError(errorMessage);
                    LabelMessage.text = errorMessage;
                }
                else
                {
                    DestroyImmediate(Instance);
                }
                return;
            }
            if (Request.webRequest.responseCode != 200)
            {
                if (!IsAutomaticCheck)
                {
                    string errorMessage =
                    $"Update check failed: {Request.webRequest.responseCode} - {Request.webRequest.downloadHandler.text}";
                    Debug.LogError(errorMessage);
                    LabelMessage.text = errorMessage;
                }
                else
                {
                    DestroyImmediate(Instance);
                }
                return;
            }

            CheckVersion();
        }

        public void CheckVersion()
        {
            var pluginVersion = JsonUtility.FromJson<ZibraEffectsVersion>(Request.webRequest.downloadHandler.text);
            IsLatestVersion = CheckIsLatestVersion(pluginVersion.version, Effects.Version);
#pragma warning disable 0162
            if (Effects.IsPreReleaseVersion)
            {
                LabelMessage.text = $"You have the Pre-release version: {Effects.Version}";
                if (IsAutomaticCheck)
                {
                    DestroyImmediate(Instance);
                }
            }
            else if (IsLatestVersion)
            {
                LabelMessage.text = $"You have the latest version: {Effects.Version}";
                if (IsAutomaticCheck)
                {
                    DestroyImmediate(Instance);
                }
            }
            else
            {
                LabelMessage.text =
                    $"New version available. Please consider updating. Latest version: \"{pluginVersion.version}\". Current version: \"{Effects.Version}\"";
                UpdateButton.visible = true;
                UpdateButton.clicked += UpdatePluginPageClick;
                if (IsAutomaticCheck)
                {
                    Show();
                }
            }
#pragma warning restore 0162

            LabelMessage.style.marginLeft = 50;
            LabelMessage.style.marginRight = 50;
            LabelMessage.style.whiteSpace = WhiteSpace.Normal;
            LabelMessage.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private bool CheckIsLatestVersion(string pluginVersion, string localVersion)
        {
            char[] separator = { '.' };
            int[] currentVersion = {};
            int[] serverVersion = {};

            try
            {
                string[] currentVersionStrArr = localVersion.Split(separator);
                currentVersion = new int[currentVersionStrArr.Length];
                for (int i = 0; i < currentVersionStrArr.Length; i++)
                {
                    currentVersion[i] = int.Parse(currentVersionStrArr[i]);
                }

                string[] serverVersionStrArr = pluginVersion.Split(separator);
                serverVersion = new int[serverVersionStrArr.Length];
                for (int i = 0; i < serverVersionStrArr.Length; i++)
                {
                    serverVersion[i] = int.Parse(serverVersionStrArr[i]);
                }
            }
            catch (Exception)
            {
                return true;
            }

            if (currentVersion.Length < serverVersion.Length)
            {
                int[] tempArr = currentVersion;
                currentVersion = new int[serverVersion.Length];
                Array.Copy(tempArr, currentVersion, tempArr.Length);
            }
            else if (currentVersion.Length > serverVersion.Length)
            {
                int[] tempArr = serverVersion;
                serverVersion = new int[currentVersion.Length];
                Array.Copy(tempArr, serverVersion, tempArr.Length);
            }

            for (int i = 0; i < Math.Min(currentVersion.Length, serverVersion.Length); i++)
            {
                if (currentVersion[i] < serverVersion[i])
                {
                    return false;
                }
                else if (currentVersion[i] > serverVersion[i])
                {
                    return true;
                }
            }
            return true;
        }

        // C# doesn't know we use it with JSON deserialization
#pragma warning disable 0649
        private struct ZibraEffectsVersion
        {
            public string version;
        }
#pragma warning restore 0649
    }
}
